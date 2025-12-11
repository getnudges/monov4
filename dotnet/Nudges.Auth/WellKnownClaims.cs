using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Nudges.Auth;

public static class WellKnownClaims {
    public const string Role = ClaimTypes.Role;
    public const string Username = JwtRegisteredClaimNames.PreferredUsername;
    public const string Locale = JwtRegisteredClaimNames.Locale;
    public const string PhoneNumber = JwtRegisteredClaimNames.PhoneNumber;
    public const string Sub = ClaimTypes.NameIdentifier; // Because claim types are stupid
}

public static class ClaimExtensions {
    public static bool HasRole(this ClaimsPrincipal principal, string role) {
        if (principal.HasClaim(WellKnownClaims.Role, role)) {
            return true;
        }
        // Because `ClaimsPrincipal` is stupid...
        if (principal.HasClaim("role", ClaimValues.Roles.Admin)) {
            return true;
        }
        if (principal.IsInRole(role)) {
            return true;
        }
        return false;
    }

    public static bool ContainsRole(this IEnumerable<Claim> claims, string role) {
        if (claims.FirstOrDefault(c => c.Type == WellKnownClaims.Role)?.Value == role) {
            return true;
        }
        if (claims.FirstOrDefault(c => c.Type == "role")?.Value == role) {
            return true;
        }
        return false;
    }

    public static bool TryGetRole(this IEnumerable<Claim> claims, out string role) {
        if (claims.FirstOrDefault(c => c.Type == WellKnownClaims.Role)?.Value is string dumbRoleClaim) {
            role = dumbRoleClaim;
            return true;
        }
        if (claims.FirstOrDefault(c => c.Type == "role")?.Value is string roleClaim) {
            role = roleClaim;
            return true;
        }
        role = default!;
        return false;
    }
}
