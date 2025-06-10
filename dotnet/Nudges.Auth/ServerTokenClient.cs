using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Monads;
using Precision.WarpCache;

namespace Nudges.Auth;

public interface IServerTokenClient {
    public Task<Result<TokenResponse, AuthApiError>> GetTokenAsync(string resource);
}

public sealed class ServerTokenClient(HttpClient client, IOptions<OidcConfig> config, ICacheStore<string, string> cacheStore) : IServerTokenClient {
    private readonly HttpClient _client = client;
    private readonly OidcConfig _config = config.Value;
    private readonly ICacheStore<string, string> _cacheStore = cacheStore;

    public async Task<Result<TokenResponse, AuthApiError>> GetTokenAsync(string resource) {
        var cached = await _cacheStore.GetAsync($"{_config.Realm}:token:{resource}");
        if (cached.Found) {
            // TODO: Should ExpiryTime be an int?
            return new TokenResponse(
                cached.Value,
                Convert.ToInt32(cached.ExpiryTime > int.MaxValue ? int.MaxValue : Convert.ToInt32(cached.ExpiryTime, CultureInfo.InvariantCulture)));
        }
        try {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/{_config.Realm}/protocol/openid-connect/token");
            request.Headers.Add("Accept", "application/json");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", _config.ClientId },
            { "client_secret", _config.ClientSecret },
            { "grant_type", "client_credentials" },
            { "resource", resource }
        });
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadFromJsonAsync(AuthApiErrorContext.Default.AuthApiError);
                if (body is null) {
                    return new AuthApiError("Parse error", "Could not parse error response.");
                }
                return new AuthApiError(body.Error, body.ErrorDescription);
            }

            var token = await response.Content.ReadFromJsonAsync(TokenResponseContext.Default.TokenResponse);
            if (token is null) {
                return new AuthApiError("Parse error", "Could not parse token response.");
            }
            await _cacheStore.SetAsync($"{_config.Realm}:token:{_config.ClientId}", token.AccessToken, TimeSpan.FromSeconds(token.ExpiresIn));
            return token;
        } catch (Exception ex) {
            return new AuthApiError("Request error", ex.Message);
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
