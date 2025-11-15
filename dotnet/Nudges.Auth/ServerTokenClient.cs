using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monads;
using Precision.WarpCache;

namespace Nudges.Auth;

public interface IServerTokenClient {
    public Task<Result<TokenResponse, OidcException>> GetTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class ServerTokenClient(HttpClient client, IOptions<OidcConfig> config, ICacheStore<string, string> cacheStore, ILogger<ServerTokenClient> logger) : IServerTokenClient {
    private readonly HttpClient _client = client;
    private readonly OidcConfig _config = config.Value;
    private readonly ICacheStore<string, string> _cacheStore = cacheStore;


    private async Task<Result<T, OidcException>> SendRequestAsync<T>(HttpRequestMessage request, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken) {
        try {
            var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadFromJsonAsync(AuthApiErrorContext.Default.AuthApiError, cancellationToken);
                if (body is null) {
                    return new OidcException("Could not parse error response");
                }
                var exception = OidcException.FromApiError(body);
                logger.LogRequestError(exception);
                return exception;
            }

            var result = await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken);
            if (result is null) {
                var exception = new OidcException($"Could not parse {typeof(T)} response.");
                logger.LogRequestError(exception);
                return exception;
            }
            return result;
        } catch (Exception ex) {
            logger.LogRequestError(ex);
            return OidcException.FromException($"Request failed: {ex.Message}", ex);
        }
    }

    private async Task<Result<TokenResponse, OidcException>> GetAdminToken(CancellationToken cancellationToken) {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/{_config.Realm}/protocol/openid-connect/token") {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "grant_type", "client_credentials" },
            })
        };
        return await SendRequestAsync(request, TokenResponseContext.Default.TokenResponse, cancellationToken);
    }

    public async Task<Result<TokenResponse, OidcException>> GetTokenAsync(CancellationToken cancellationToken = default) {
        var cached = await _cacheStore.GetAsync($"{_config.Realm}:token:{_config.ClientId}", cancellationToken);
        if (cached.Found) {
            // TODO: Should ExpiryTime be an int?
            return new TokenResponse(
                cached.Value,
                Convert.ToInt32(cached.ExpiryTime > int.MaxValue ? int.MaxValue : Convert.ToInt32(cached.ExpiryTime, CultureInfo.InvariantCulture)));
        }
        try {
            return await GetAdminToken(cancellationToken).Map(async adminToken => {
                await _cacheStore.SetAsync(
                    $"{_config.Realm}:token:{_config.ClientId}", adminToken.AccessToken, TimeSpan.FromSeconds(adminToken.ExpiresIn));
                return adminToken;
            });
        } catch (Exception ex) {
            return new OidcException("Could not retrive Token", ex);
        }

    }
}

public record TokenResponse(string AccessToken, int ExpiresIn);
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(TokenResponse))]
public partial class TokenResponseContext : JsonSerializerContext;

public record AuthApiError(string Error, string ErrorDescription);
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(AuthApiError))]
public partial class AuthApiErrorContext : JsonSerializerContext;

internal static partial class ServerTokenClientLogs {
    [LoggerMessage(Level = LogLevel.Warning, Message = "Request Failed")]
    public static partial void LogRequestError(this ILogger<ServerTokenClient> logger, Exception? exception = null);
}
