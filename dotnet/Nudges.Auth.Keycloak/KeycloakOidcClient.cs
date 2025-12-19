using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nudges.Auth.Keycloak;

public sealed class KeycloakOidcClient(HttpClient client, IOptions<OidcConfig> config, ILogger<KeycloakOidcClient> logger) : IOidcClient, IDisposable {
    private readonly HttpClient _client = client;
    private readonly OidcConfig _config = config.Value;

    private async Task<ErrorOr<T>> SendRequestAsync<T>(HttpRequestMessage request, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken) {
        try {
            var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadFromJsonAsync(AuthApiErrorContext.Default.AuthApiError, cancellationToken);
                if (body is null) {
                    return Errors.ResponseParseFailed();
                }
                return Errors.RequestFailed(body);
            }

            var result = await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken);
            if (result is null) {
                return Errors.ResponseParseFailed(typeof(T).FullName);
            }
            return result;
        } catch (Exception ex) {
            logger.LogRequestError(ex);
            return Errors.Exception(ex);
        }
    }

    public async Task<ErrorOr<TokenResponse>> GetUserTokenAsync(string username, string password, CancellationToken cancellationToken) {
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

    public Task<ErrorOr<List<UserRepresentation>>> GetUserByUsername(string username, CancellationToken cancellationToken) =>
        GetAdminToken(cancellationToken).ThenAsync<TokenResponse, List<UserRepresentation>>(async adminToken => {
            var request = new HttpRequestMessage(HttpMethod.Get, Urls.Users(_config.Realm)) {
                Headers = { { "Authorization", $"Bearer {adminToken.AccessToken}" } },
            };
            try {
                var response = await _client.SendAsync(request);
                var users = await response.Content.ReadFromJsonAsync(UserRepresentationContext.Default.ListUserRepresentation);
                return users ?? [];
            } catch (Exception ex) {
                return Errors.Exception(ex);
            }
        });

    public async Task<ErrorOr<TokenResponse>> GrantTokenAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken) {
        var request = new HttpRequestMessage(HttpMethod.Post, Urls.Token(_config.Realm)) {
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

    private async Task<ErrorOr<TokenResponse>> GetAdminToken(CancellationToken cancellationToken) {
        var request = new HttpRequestMessage(HttpMethod.Post, Urls.Token("master")) {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", "admin-cli" },
                { "username", _config.AdminCredentials.Username },
                { "password", _config.AdminCredentials.Password },
                { "grant_type", "password" },
            })
        };
        return await SendRequestAsync(request, TokenResponseContext.Default.TokenResponse, cancellationToken);
    }

    public async Task<ErrorOr<Success>> CreateUser(UserRepresentation userRepresentation, CancellationToken cancellationToken) =>
        await GetAdminToken(cancellationToken).ThenAsync<TokenResponse, Success>(async adminToken => {
            var request = new HttpRequestMessage(HttpMethod.Post, Urls.Users(_config.Realm)) {
                Headers = { { "Authorization", $"Bearer {adminToken.AccessToken}" } },
                Content = JsonContent.Create(userRepresentation, UserRepresentationContext.Default.UserRepresentation)
            };
            try {
                var response = await _client.SendAsync(request);
                if (!response.IsSuccessStatusCode) {
                    if (response.StatusCode == System.Net.HttpStatusCode.Conflict) {
                        var err = await response.Content.ReadFromJsonAsync(SimpleApiErrorContext.Default.SimpleApiError);
                        return Errors.SimpleError(err?.ErrorMessage ?? "Unknown Error");
                    }
                    var body = await response.Content.ReadFromJsonAsync(AuthApiErrorContext.Default.AuthApiError);
                    if (body is null) {
                        return Errors.ResponseParseFailed();
                    }
                    return Errors.RequestFailed(body);
                }
                return Result.Success;
            } catch (Exception ex) {
                return Errors.Exception(ex);
            }
        });
    public void Dispose() => _client.Dispose();

    private static class Urls {
        public static string Users(string realm) => $"/admin/realms/{realm}/users";
        public static string Token(string realm) => $"/realms/{realm}/protocol/openid-connect/token";
    }

    private static class Errors {
        public static Error ResponseParseFailed(string? type = null) =>
            Error.Validation("Response.Parsing", $"Could not parse error response {(type is null ? "" : $"of type {type}")}.");
        public static Error RequestFailed(AuthApiError error) =>
            Error.Failure("Http.Request.Failure", $"OIDC request failed: {error.ErrorDescription}");
        public static Error SimpleError(string errorMessage) =>
            Error.Failure("Http.Request.Failure", $"OIDC request failed: {errorMessage}");

        public static Error Exception(Exception ex) =>
            Error.Failure(ex.GetType().Name, ex.Message, new Dictionary<string, object>([
                new KeyValuePair<string, object>("StackTrace", ex.StackTrace ?? string.Empty)
            ]));
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UserRepresentation))]
[JsonSerializable(typeof(List<UserRepresentation>))]
public partial class UserRepresentationContext : JsonSerializerContext;
