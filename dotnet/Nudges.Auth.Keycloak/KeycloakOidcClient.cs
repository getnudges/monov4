using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Options;
using Monads;
using UnAd.Auth;

namespace Nudges.Auth.Keycloak;

public sealed class KeycloakOidcClient(HttpClient client, IOptions<OidcConfig> config) : IKeycloakOidcClient {
    private readonly HttpClient _client = client;
    private readonly OidcConfig _config = config.Value;

    private async Task<Result<T, OidcException>> SendRequestAsync<T>(HttpRequestMessage request, JsonTypeInfo<T> jsonTypeInfo) {
        try {
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadFromJsonAsync(AuthApiErrorContext.Default.AuthApiError);
                return body is null
                    ? new OidcException("Could not parse error response")
                    : OidcException.FromApiError(body);
            }

            var result = await response.Content.ReadFromJsonAsync(jsonTypeInfo);
            if (result is null) {
                return new OidcException($"Could not parse {typeof(T)} response.");
            }
            return result;
        } catch (Exception ex) {
            return OidcException.FromException("Request failed.", ex);
        }
    }

    public async Task<Result<TokenResponse, OidcException>> GetTokenAsync(string username, string password) {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/{_config.Realm}/protocol/openid-connect/token") {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "grant_type", "password" },
                { "username", username },
                { "password", password },
            })
        };
        return await SendRequestAsync(request, TokenResponseContext.Default.TokenResponse);
    }

    public async Task<Result<TokenResponse, OidcException>> GrantTokenAsync(string code, string codeVerifier, string redirectUri) {
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

        return await SendRequestAsync(request, TokenResponseContext.Default.TokenResponse);
    }

    private async Task<Result<TokenResponse, OidcException>> GetAdminToken() {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/master/protocol/openid-connect/token") {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", _config.AdminCredentials.AdminClientId },
                { "grant_type", "password" },
                { "username", _config.AdminCredentials.Username },
                { "password", _config.AdminCredentials.Password },
            })
        };
        return await SendRequestAsync(request, TokenResponseContext.Default.TokenResponse);
    }

    public async Task<Maybe<OidcException>> CreateUser(UserRepresentation userRepresentation) =>
        await GetAdminToken().MapError(async adminToken => {
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
}

public class OidcException : Exception {
    public AuthApiError? ApiError { get; init; }

    public OidcException(string message) : base(message) { }

    private OidcException(string message, AuthApiError? apiError = null, Exception? inner = null)
        : base(message, inner) => ApiError = apiError;

    public static OidcException FromApiError(AuthApiError error) =>
        new(error.ErrorDescription ?? error.Error ?? "Unknown OIDC API error", error);

    public static OidcException FromException(string message, Exception ex) =>
        new(message, inner: ex);
}

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(UserRepresentation))]
public partial class UserRepresentationContext : JsonSerializerContext;
