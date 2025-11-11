using Keycloak.AuthServices.Sdk.Admin.Models;
using Monads;
using Nudges.Auth;

namespace Nudges.Auth.Keycloak;

/// <summary>
/// Provides a simple wrapper over the Keycloak REST API.
/// </summary>
public interface IKeycloakOidcClient {
    /// <summary>
    /// Retrieves a token from Keycloak using the password grant type.
    /// </summary>
    /// <param name="username">User's username</param>
    /// <param name="password">User's password</param>
    public Task<Result<TokenResponse, OidcException>> GetUserTokenAsync(string username, string password);
    /// <summary>
    /// Retrieves a token from Keycloak using the authorization code grant type.
    /// </summary>
    /// <param name="code">Code from OIDC auth flow</param>
    /// <param name="codeVerifier">Code Verifier from OIDC auth flow</param>
    /// <param name="redirectUri">Redirect URI from OIDC auth flow</param>
    public Task<Result<TokenResponse, OidcException>> GrantTokenAsync(string code, string codeVerifier, string redirectUri);
    /// <summary>
    /// Creates a new user in Keycloak.
    /// </summary>
    /// <param name="userRepresentation"> Keycloak <see cref="UserRepresentation"/> object. (see <see href="https://www.keycloak.org/docs-api/latest/rest-api/index.html#UserRepresentation"/>)</param>
    public Task<Maybe<OidcException>> CreateUser(UserRepresentation userRepresentation);
}
