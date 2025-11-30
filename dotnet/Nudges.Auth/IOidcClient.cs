using ErrorOr;

namespace Nudges.Auth;

/// <summary>
/// Provides a simple wrapper over the Keycloak REST API.
/// </summary>
public interface IOidcClient {
    /// <summary>
    /// Retrieves a token from Keycloak using the password grant type.
    /// </summary>
    /// <param name="username">User's username</param>
    /// <param name="password">User's password</param>
    public Task<ErrorOr<TokenResponse>> GetUserTokenAsync(string username, string password, CancellationToken cancellationToken);
    /// <summary>
    /// Retrieves a token from Keycloak using the authorization code grant type.
    /// </summary>
    /// <param name="code">Code from OIDC auth flow</param>
    /// <param name="codeVerifier">Code Verifier from OIDC auth flow</param>
    /// <param name="redirectUri">Redirect URI from OIDC auth flow</param>
    public Task<ErrorOr<TokenResponse>> GrantTokenAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken);
    /// <summary>
    /// Creates a new user in Keycloak.
    /// </summary>
    /// <param name="userRepresentation"> Keycloak <see cref="UserRepresentation"/> object. (see <see href="https://www.keycloak.org/docs-api/latest/rest-api/index.html#UserRepresentation"/>)</param>
    public Task<ErrorOr<Success>> CreateUser(UserRepresentation userRepresentation, CancellationToken cancellationToken);

    public Task<ErrorOr<List<UserRepresentation>>> GetUserByUsername(string username, CancellationToken cancellationToken);
}
