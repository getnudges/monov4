using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UnAd.Auth;

public class SimpleAuthenticator(SecurityKey signingKey) : IAuthenticator {
    private static readonly TimeSpan Expiration = TimeSpan.FromDays(1);
    private readonly SecurityKey _signingKey = signingKey;

    public SimpleAuthenticator(string signingKeyString) : this(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyString))) { }

    public (string?, int) GenerateToken(string userName, IEnumerable<Claim> additionalClaims) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentNullException.ThrowIfNull(additionalClaims);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateEncodedJwt(new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(additionalClaims),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature),
            Expires = DateTime.UtcNow.Add(Expiration),
        });
        return (token, (int)Expiration.TotalSeconds);
    }

    public ClaimsPrincipal? ValidateToken(string token) {
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));
        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        try {
            handler.ValidateToken(token, validationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwtToken) {
                return null;
            }
            return new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims));
        } catch (Exception) {
            return default;
        }

    }
}
