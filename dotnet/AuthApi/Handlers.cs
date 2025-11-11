using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Monads;
using Nudges.Auth.Keycloak;
using Precision.WarpCache.Grpc.Client;
using Nudges.Auth;
using Nudges.Auth.Web;
using Nudges.Configuration.Extensions;
using Nudges.Kafka;

namespace AuthApi;

internal static class Handlers {

    public static async Task<IResult> GenerateOtp(string p, IOtpVerifier otpVerifier, ICacheClient<string> cache, KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer, HttpContext context) {
        var (code, base32Key) = otpVerifier.GetOtp();

        await cache.SetOtpSecret(p, base32Key);
        var locale = context.Request.GetRequestCulture();
        await notificationProducer.ProduceSendOtp(p, locale, code, context.RequestAborted);
        if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
            return Results.Json(new OtpResponse(code, DateTimeOffset.UtcNow.Add(WarpCacheExtensions.OtpExpirationTime)), OtpResponseSerializerContext.Default.OtpResponse);
        }
        return Results.Ok();
    }

    public static async Task<IResult> ValidateOtp(OtpCredentials credentials, IOtpVerifier otpVerifier, IKeycloakOidcClient tokenClient, ICacheClient<string> cache, HttpContext httpContext) {
        if (!httpContext.Request.Headers.TryGetValue("X-Role-Claim", out var role)) {
            return Results.BadRequest(new ErrorResponse("Missing required role claim."));
        }

        var roleClaim = role.ToString();
        if (roleClaim is not ClaimValues.Roles.Client and not ClaimValues.Roles.Subscriber) {
            return Results.BadRequest(new ErrorResponse($"Invalid role: {roleClaim}"));
        }

        var key = await cache.GetOtpSecret(credentials.PhoneNumber);
        if (key is null) {
            return Results.BadRequest(new ErrorResponse("OTP expired or not generated"));
        }

        var isValid = otpVerifier.ValidateOtp(key, credentials.Code);
        if (!isValid) {
            return Results.BadRequest(new ErrorResponse("Invalid OTP"));
        }
        await cache.RemoveOtpSecret(credentials.PhoneNumber);

        var tempPassword = Guid.NewGuid().ToString("N");
        var result = await tokenClient.CreateUser(new UserRepresentation {
            Username = credentials.PhoneNumber,
            Credentials = [
                new() {
                    Type = "password",
                    Value = tempPassword,
                    Temporary = false
                }
            ],
            Enabled = true,
            // TODO: this is gross
            Groups = [$"{roleClaim}s"],
            RequiredActions = [],
            Attributes = new Dictionary<string, ICollection<string>> {
                { WellKnownClaims.PhoneNumber, [credentials.PhoneNumber] },
                { "phone", [credentials.PhoneNumber] },
                { "locale", [httpContext.Request.GetRequestCulture()] }
            }
        });

        // TODO: Clean up
        var op = await result.Match(err => Results.Problem(err.Message), async () =>
            await tokenClient.GetUserTokenAsync(credentials.PhoneNumber, tempPassword).Map(async token => {
                var tokenId = Guid.NewGuid().ToString("N");
                await cache.SetToken(tokenId, token.AccessToken, token.ExpiresIn);
                httpContext.AttachTokenIdCookie(token, tokenId);
                return Results.Ok();
            }));

        return op.Match<IResult>(x => x, x => Results.Problem(x.Message));
    }

    public static async Task<IResult> OAuthRedirect(string? code, string state, string? error, string? error_description, HttpContext httpContext, ICacheClient<string> cache, IKeycloakOidcClient userTokenClient) {
        if (!string.IsNullOrEmpty(error)) {
            return Results.BadRequest(new ErrorResponse(error_description ?? "Unknown OAuth error."));
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state)) {
            return Results.BadRequest(new ErrorResponse("Invalid OAuth request: missing code or state."));
        }

        var stateData = Base64UrlEncoder.Decode(state);
        var oauthState = JsonSerializer.Deserialize(stateData, OAuthStateSerializerContext.Default.OAuthState);
        var codeVerifier = await cache.GetOidcState(state);
        if (string.IsNullOrEmpty(codeVerifier)) {
            return Results.Unauthorized();
        }
        var tokenResponse = await userTokenClient.GrantTokenAsync(code!, codeVerifier, oauthState.InternalRedirect);

        return await tokenResponse.Match<IResult>(async token => {
            await cache.RemoveOidcState(state);
            var tokenId = Guid.NewGuid().ToString("N");
            await cache.SetToken(tokenId, token.AccessToken, token.ExpiresIn);
            httpContext.AttachTokenIdCookie(token, tokenId);
            return Results.Redirect(oauthState.RedirectUri);
        }, e => Results.Problem(e.Message));
    }

    public static async Task<IResult> OAuthLogin(string redirectUri, IConfiguration config, IOptions<OidcConfig> oidcConfig, HttpContext httpContext, ICacheClient<string> cache) {
        var baseUrl = new Uri($"{config.GetOidcServerAuthUrl()}/realms/{oidcConfig.Value.Realm}/protocol/openid-connect/auth");
        var host = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpContext.Request.Host.Host;
        var port = httpContext.Request.Headers["X-Forwarded-Port"].FirstOrDefault() ?? httpContext.Request.Host.Port?.ToString(CultureInfo.InvariantCulture)
                   ?? (httpContext.Request.Scheme == "https" ? "443" : "80");
        var portSuffix = (port is "80" or "443") ? "" : $":{port}";
        var redirectUrl = new Uri($"{httpContext.Request.Scheme}://{host}{portSuffix}/auth/redirect");
        var codeVerifier = PkceHelper.GenerateCodeVerifier();
        var codeChallenge = PkceHelper.ComputeCodeChallenge(codeVerifier);
        var state = JsonSerializer.Serialize(new OAuthState(redirectUri, redirectUrl.ToString(), Guid.NewGuid()), OAuthStateSerializerContext.Default.OAuthState);
        var stateKey = Base64UrlEncoder.Encode(state);

        var query = await new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", oidcConfig.Value.ClientId },
                { "response_type", "code" },
                { "redirect_uri", redirectUrl.ToString() },
                { "scope", "openid profile email phone" },
                { "code_challenge", codeChallenge },
                { "code_challenge_method", "S256" },
                { "state", stateKey },
            }).ReadAsStringAsync();
        var redirect = new UriBuilder(baseUrl) {
            Query = query
        }.ToString();

        await cache.SetOidcState(stateKey, codeVerifier);

        return Results.Redirect(redirect);
    }

    private static class PkceHelper {
        public static string GenerateCodeVerifier() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) // 32 bytes is PKCE spec-compliant
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        public static string ComputeCodeChallenge(string codeVerifier) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
    }
}

internal static class HttpRequestExtensions {
    public static string GetRequestCulture(this HttpRequest request) {
        var locale = request.Headers.AcceptLanguage.ToString().Split(',').FirstOrDefault();
        if (string.IsNullOrEmpty(locale)) {
            return CultureInfo.CurrentCulture.Name;
        }
        return locale;
    }

    public static void AttachTokenIdCookie(this HttpContext httpContext, TokenResponse token, string tokenId) =>
        httpContext.Response.Cookies.Append("TokenId", tokenId, new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn)
        });
}

public record struct OAuthState(string RedirectUri, string InternalRedirect, Guid Nonce);

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(OAuthState))]
public sealed partial class OAuthStateSerializerContext : JsonSerializerContext;
