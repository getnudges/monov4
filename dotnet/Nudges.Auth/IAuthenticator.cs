using System.Security.Claims;

namespace Nudges.Auth;

public interface IAuthenticator {
    public (string?, int) GenerateToken(string userName, IEnumerable<Claim> additionalClaims);
    public ClaimsPrincipal? ValidateToken(string token);
}
