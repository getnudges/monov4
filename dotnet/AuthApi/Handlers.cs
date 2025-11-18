using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Monads;
using Nudges.Auth;
using Nudges.Auth.Web;
using Nudges.Configuration.Extensions;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Telemetry;
using Precision.WarpCache.Grpc.Client;

namespace AuthApi;

internal static class Handlers {
    public static readonly ActivitySource ActivitySource = new($"{typeof(Handlers).FullName}");

    public static async Task<IResult> GenerateOtp(OtpRequest request, IOtpVerifier otpVerifier, ICacheClient<string> cache, KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer, HttpContext httpContext) {

        using var activity = ActivitySource.StartActivity(nameof(ValidateOtp), ActivityKind.Server, httpContext.Request.GetActivityContext());
        activity?.Start();

        var (code, base32Key) = otpVerifier.GetOtp();

        await cache.SetOtpSecret(request.PhoneNumber, base32Key);
        var locale = httpContext.Request.GetRequestCulture();
        await notificationProducer.ProduceOtpRequested(request.PhoneNumber, locale, code, httpContext.RequestAborted);

        // When developing locally, return the OTP code in the response for easier testing
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
            return Results.Json(new OtpResponse(code, DateTimeOffset.UtcNow.Add(WarpCacheExtensions.OtpExpirationTime)), OtpResponseSerializerContext.Default.OtpResponse);
        }
        activity?.SetStatus(ActivityStatusCode.Ok);
        return Results.Ok();
    }

    public static async Task<IResult> ValidateOtp(OtpCredentials credentials, IOtpVerifier otpVerifier, IOidcClient tokenClient, ICacheClient<string> cache, HttpContext httpContext) {

        using var activity = ActivitySource.StartActivity(nameof(ValidateOtp), ActivityKind.Server, httpContext.Request.GetActivityContext());
        activity?.Start();

        if (!httpContext.Request.Headers.TryGetValue("X-Role-Claim", out var role)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing required role claim.");
            return Results.BadRequest(new ErrorResponse("Missing required role claim."));
        }

        var roleClaim = role.ToString();
        if (roleClaim is not ClaimValues.Roles.Client and not ClaimValues.Roles.Subscriber) {
            activity?.SetStatus(ActivityStatusCode.Error, $"Invalid role claim: {roleClaim}");
            return Results.BadRequest(new ErrorResponse($"Invalid role: {roleClaim}"));
        }

        var key = await cache.GetOtpSecret(credentials.PhoneNumber);
        if (key is null) {
            activity?.SetStatus(ActivityStatusCode.Error, "OTP expired or not generated.");
            return Results.BadRequest(new ErrorResponse("OTP expired or not generated"));
        }

        if (!otpVerifier.ValidateOtp(key, credentials.Code)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid OTP.");
            return Results.BadRequest(new ErrorResponse("Invalid OTP"));
        }

        await cache.RemoveOtpSecret(credentials.PhoneNumber);

        var tempPassword = Guid.NewGuid().ToString("N");
        var userCreate = await CreateNewUser(credentials, tokenClient, httpContext, roleClaim, tempPassword);

        var result = await userCreate.Match(err => Results.Problem(err.Message), async () =>
            await tokenClient.GetUserTokenAsync(credentials.PhoneNumber, tempPassword, httpContext.RequestAborted).Map(async token => {
                var tokenId = Guid.NewGuid().ToString("N");
                await cache.SetToken(tokenId, token.AccessToken, token.ExpiresIn);
                httpContext.AttachTokenIdCookie(token, tokenId);
                return Results.Ok();
            }));

        return result.Match<IResult>(x => {
            activity?.SetStatus(ActivityStatusCode.Ok);
            return x;
        }, x => {
            activity?.SetStatus(ActivityStatusCode.Error, $"Error during user creation or token retrieval: {x.Message}");
            return Results.Problem(x.Message);
        });
    }

    private static async Task<Maybe<OidcException>> CreateNewUser(OtpCredentials credentials, IOidcClient tokenClient, HttpContext httpContext, string roleClaim, string tempPassword) {
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
        }, httpContext.RequestAborted);
        return result;
    }

    public static async Task<IResult> OAuthRedirect(string? code, string state, string? error, string? error_description, HttpContext httpContext, ICacheClient<string> cache, IOidcClient userTokenClient, KafkaMessageProducer<UserAuthenticationEventKey, UserAuthenticationEvent> eventProducer) {

        using var activity = ActivitySource.StartActivity(nameof(OAuthRedirect), ActivityKind.Server, httpContext.Request.GetActivityContext());
        activity?.Start();

        if (!string.IsNullOrEmpty(error)) {
            activity?.SetStatus(ActivityStatusCode.Error, $"OAuth error: {error} - {error_description}");
            return Results.BadRequest(new ErrorResponse(error_description ?? "Unknown OAuth error."));
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing code or state in OAuth redirect.");
            return Results.BadRequest(new ErrorResponse("Invalid OAuth request: missing code or state."));
        }

        var stateData = Base64UrlEncoder.Decode(state);
        var oauthState = JsonSerializer.Deserialize(stateData, OAuthStateSerializerContext.Default.OAuthState);
        var codeVerifier = await cache.GetOidcState(state);
        if (string.IsNullOrEmpty(codeVerifier)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid or expired OAuth state.");
            return Results.Unauthorized();
        }

        var tokenResponse = await userTokenClient.GrantTokenAsync(code!, codeVerifier, oauthState.InternalRedirect, httpContext.RequestAborted);

        var result = await tokenResponse.Match<IResult>(
            CreateRedirectResponse(state, httpContext, cache, eventProducer, oauthState),
            e => {
                activity?.SetStatus(ActivityStatusCode.Error, $"Failed to grant token: {e.Message}");
                return Results.Problem(e.Message);
            });

        activity?.SetStatus(ActivityStatusCode.Ok);
        return result;
    }

    private static Func<TokenResponse, Task<IResult>> CreateRedirectResponse(
        string state, HttpContext httpContext, ICacheClient<string> cache, KafkaMessageProducer<UserAuthenticationEventKey, UserAuthenticationEvent> eventProducer, OAuthState oauthState) => async token => {

            await cache.RemoveOidcState(state);
            var tokenId = Guid.NewGuid().ToString("N");
            await cache.SetToken(tokenId, token.AccessToken, token.ExpiresIn);

            httpContext.AttachTokenIdCookie(token, tokenId);

            // Extract phone number from token claims
            var handler = new JsonWebTokenHandler();
            var jwt = handler.ReadJsonWebToken(token.AccessToken);
            var phoneNumber = jwt.Claims.FirstOrDefault(c => c.Type == WellKnownClaims.PhoneNumber)?.Value;
            var locale = jwt.Claims.FirstOrDefault(c => c.Type == WellKnownClaims.Locale)?.Value
                ?? httpContext.Request.GetRequestCulture();

            if (string.IsNullOrEmpty(phoneNumber)) {
                return Results.Problem("Phone number claim is missing in the token.");
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            /*
             * TODO: This call can hang if it can't connect to Kafka.  We need to address that.
             */
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            await eventProducer.ProduceUserLoggedIn(phoneNumber, locale, cts.Token).ContinueWith(t => {
                if (t.IsFaulted) {
                }
            }, TaskScheduler.Default);

            return Results.Redirect(oauthState.RedirectUri);
        };

    public static async Task<IResult> OAuthLogin(string redirectUri, IConfiguration config, IOptions<OidcConfig> oidcConfig, HttpContext httpContext, ICacheClient<string> cache) {

        using var activity = ActivitySource.StartActivity(nameof(OAuthLogin), ActivityKind.Server, httpContext.Request.GetActivityContext());
        activity?.Start();

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

        activity?.SetStatus(ActivityStatusCode.Ok);
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
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn),
        });
}

public record struct OAuthState(string RedirectUri, string InternalRedirect, Guid Nonce);

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(OAuthState))]
public sealed partial class OAuthStateSerializerContext : JsonSerializerContext;
