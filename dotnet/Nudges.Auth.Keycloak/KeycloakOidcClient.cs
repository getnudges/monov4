using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monads;

namespace Nudges.Auth.Keycloak;

public sealed class KeycloakOidcClient(HttpClient client, IOptions<OidcConfig> config, ILogger<KeycloakOidcClient> logger) : IOidcClient, IDisposable {
    private readonly HttpClient _client = client;
    private readonly OidcConfig _config = config.Value;

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

    public async Task<Result<TokenResponse, OidcException>> GetUserTokenAsync(string username, string password, CancellationToken cancellationToken) {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/{_config.Realm}/protocol/openid-connect/token") {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "grant_type", "password" },
                { "username", username },
                { "password", password },
            })
        };
        return await SendRequestAsync(request, TokenResponseContext.Default.TokenResponse, cancellationToken);
    }

    public async Task<Result<List<UserRepresentation>, OidcException>> GetUserByUsername(string username, CancellationToken cancellationToken) =>
        await GetAdminToken(cancellationToken).Map(async adminToken => {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/realms/{_config.Realm}/users") {
                Headers = { { "Authorization", $"Bearer {adminToken.AccessToken}" } },
            };
            try {
                var response = await _client.SendAsync(request);
                //var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var users = await response.Content.ReadFromJsonAsync(UserRepresentationContext.Default.ListUserRepresentation);
                return users ?? [];
            } catch (Exception e) {
                return [];
            }
        });

    public async Task<Result<TokenResponse, OidcException>> GrantTokenAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken) {
        logger.LogError($"Using ClientSecret: {_config.ClientSecret}");
        var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/{_config.Realm}/protocol/openid-connect/token") {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "code_verifier", codeVerifier },
            })
        };

        return await SendRequestAsync(request, TokenResponseContext.Default.TokenResponse, cancellationToken);
    }

    private async Task<Result<TokenResponse, OidcException>> GetAdminToken(CancellationToken cancellationToken) {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/master/protocol/openid-connect/token") {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", _config.AdminCredentials.AdminClientId },
                { "username", _config.AdminCredentials.Username },
                { "password", _config.AdminCredentials.Password },
                { "grant_type", "password" },
            })
        };
        return await SendRequestAsync(request, TokenResponseContext.Default.TokenResponse, cancellationToken);
    }

    public async Task<Maybe<OidcException>> CreateUser(UserRepresentation userRepresentation, CancellationToken cancellationToken) =>
        await GetAdminToken(cancellationToken).MapError(async adminToken => {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/realms/{_config.Realm}/users") {
                Headers = { { "Authorization", $"Bearer {adminToken.AccessToken}" } },
                Content = JsonContent.Create(userRepresentation, UserRepresentationContext.Default.UserRepresentation)
            };
            try {
                var response = await _client.SendAsync(request);
                if (!response.IsSuccessStatusCode) {
                    var body = await response.Content.ReadFromJsonAsync(AuthApiErrorContext.Default.AuthApiError);
                    if (body is null) {
                        return new OidcException("Could not parse error response.");
                    }
                    return OidcException.FromApiError(body);
                }
                return Maybe<OidcException>.None;
            } catch (Exception e) {
                return OidcException.FromException("Could not retrieve token.", e);
            }
        });
    public void Dispose() => _client.Dispose();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UserRepresentation))]
[JsonSerializable(typeof(List<UserRepresentation>))]
public partial class UserRepresentationContext : JsonSerializerContext;
