using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Nudges.Auth;
using Nudges.Auth.Web;
using Nudges.Configuration;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Telemetry;
using Precision.WarpCache.Grpc.Client;

namespace AuthApi;

[ApiController]
[Route("/")]
public sealed class Handlers(IOtpVerifier otpVerifier,
                             ICacheClient<string> cache,
                             IOidcClient tokenClient,
                             KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                             KafkaMessageProducer<UserAuthenticationEventKey, UserAuthenticationEvent> userEventProducer,
                             ILogger<Handlers> logger) : ControllerBase {

    public static readonly ActivitySource ActivitySource = new($"{typeof(Handlers).FullName}");

    [HttpPost("otp")]
    public async Task<IActionResult> GenerateOtp([FromBody] OtpRequest request) {

        using var activity = ActivitySource.StartActivity(nameof(ValidateOtp), ActivityKind.Server, HttpContext.Request.GetActivityContext());
        activity?.Start();

        var (code, base32Key) = otpVerifier.GetOtp();

        await cache.SetOtpSecret(request.PhoneNumber, base32Key);
        var locale = HttpContext.Request.GetRequestCulture();
        await notificationProducer.ProduceOtpRequested(request.PhoneNumber, locale, code, HttpContext.RequestAborted);

        // When developing locally, return the OTP code in the response for easier testing
        if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
            activity?.SetStatus(ActivityStatusCode.Ok);
            return new JsonResult(new OtpResponse(code, DateTimeOffset.UtcNow.Add(WarpCacheExtensions.OtpExpirationTime))) {
                SerializerSettings = OtpResponseSerializerContext.Default.OtpResponse
            };
        }
        activity?.SetStatus(ActivityStatusCode.Ok);
        return Ok();
    }

    [HttpPost("otp/verify")]
    public async Task<IActionResult> ValidateOtp([FromBody] OtpCredentials credentials) {

        using var activity = ActivitySource.StartActivity(nameof(ValidateOtp), ActivityKind.Server, HttpContext.Request.GetActivityContext());
        activity?.Start();

        if (!HttpContext.Request.Headers.TryGetValue("X-Role-Claim", out var role)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing required role claim.");
            logger.LogMissingClaim();
            return BadRequest(new ErrorResponse("Missing required role claim."));
        }

        var roleClaim = role.ToString();
        if (roleClaim is not ClaimValues.Roles.Client and not ClaimValues.Roles.Subscriber) {
            activity?.SetStatus(ActivityStatusCode.Error, $"Invalid role claim: {roleClaim}");
            return BadRequest(new ErrorResponse($"Invalid role: {roleClaim}"));
        }

        var key = await cache.GetOtpSecret(credentials.PhoneNumber);
        if (key is null) {
            activity?.SetStatus(ActivityStatusCode.Error, "OTP expired or not generated.");
            return BadRequest(new ErrorResponse("OTP expired or not generated"));
        }

        if (!otpVerifier.ValidateOtp(key, credentials.Code)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid OTP.");
            return BadRequest(new ErrorResponse("Invalid OTP"));
        }

        await cache.RemoveOtpSecret(credentials.PhoneNumber);

        var tempPassword = Guid.NewGuid().ToString("N");
        var userCreate = await CreateNewUser(credentials, roleClaim, tempPassword);

        var result = await userCreate.ThenAsync(_ =>
            tokenClient.GetUserTokenAsync(credentials.PhoneNumber, tempPassword, HttpContext.RequestAborted)).ThenAsync(async token => {
                var tokenId = Guid.NewGuid().ToString("N");
                await cache.SetToken(tokenId, token.AccessToken, token.ExpiresIn);
                HttpContext.AttachTokenIdCookie(token, tokenId);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return ErrorOrFactory.From<IActionResult>(Ok());
            }).Else(e => Problem(string.Join(',', e.Select(i => i.Description))));
        return result.Value;
    }

    private async Task<ErrorOr<Success>> CreateNewUser(OtpCredentials credentials, string roleClaim, string tempPassword) {
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
            // disable required password change and email validation
            RequiredActions = [],
            Attributes = new Dictionary<string, ICollection<string>> {
                { WellKnownClaims.PhoneNumber, [credentials.PhoneNumber] },
                { "phone", [credentials.PhoneNumber] },
                { "locale", [HttpContext.Request.GetRequestCulture()] }
            }
        }, HttpContext.RequestAborted);
        return result;
    }

    [HttpGet("redirect")]
    public async Task<IActionResult> OAuthRedirect([FromQuery] string? code, [FromQuery] string state, [FromQuery] string? error, [FromQuery] string? error_description) {
        using var activity = ActivitySource.StartActivity(nameof(OAuthRedirect), ActivityKind.Server, HttpContext.Request.GetActivityContext());

        if (!string.IsNullOrEmpty(error)) {
            activity?.SetStatus(ActivityStatusCode.Error, $"OAuth error: {error} - {error_description}");
            activity?.AddEvent(new ActivityEvent("oauth.error",
                tags: new ActivityTagsCollection {
                    { "error.type", error },
                    { "error.message", error_description }
                }));
            return BadRequest(new ErrorResponse($"OAuth error: {error} - {error_description}"));
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing code or state in OAuth redirect.");
            return BadRequest(new ErrorResponse("Invalid OAuth request: missing code or state."));
        }

        var stateData = Base64UrlEncoder.Decode(state);
        var oauthState = JsonSerializer.Deserialize(stateData, OAuthStateSerializerContext.Default.OAuthState);
        var codeVerifier = await cache.GetOidcState(state);
        if (string.IsNullOrEmpty(codeVerifier)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid or expired OAuth state.");
            return Unauthorized();
        }

        var tokenResponse = await tokenClient.GrantTokenAsync(code!, codeVerifier, oauthState.InternalRedirect, HttpContext.RequestAborted);

        var result = await tokenResponse.ThenAsync(
            r => CreateRedirectResponse(r, state, oauthState))
            .Else(e => {
                var message = string.Join('\n', e.Select(i => i.Description));
                activity?.SetStatus(ActivityStatusCode.Error, $"Failed to grant token: {message}");
                return Problem(message);
            }).ThenDo(_ => activity?.SetStatus(ActivityStatusCode.Ok));

        return result.Value;
    }

    private async Task<IActionResult> CreateRedirectResponse(TokenResponse token, string state, OAuthState oauthState) {

        await cache.RemoveOidcState(state);
        var tokenId = Guid.NewGuid().ToString("N");
        await cache.SetToken(tokenId, token.AccessToken, token.ExpiresIn);

        HttpContext.AttachTokenIdCookie(token, tokenId);

        // Extract phone number from token claims
        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(token.AccessToken);
        var phoneNumber = jwt.Claims.FirstOrDefault(c => c.Type == WellKnownClaims.PhoneNumber)?.Value;
        var locale = jwt.Claims.FirstOrDefault(c => c.Type == WellKnownClaims.Locale)?.Value
            ?? HttpContext.Request.GetRequestCulture();

        if (string.IsNullOrEmpty(phoneNumber)) {
            return Problem("Phone number claim is missing in the token.");
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
        cts.CancelAfter(TimeSpan.FromSeconds(2));
        /*
         * TODO: This call can hang if it can't connect to Kafka.  We need to address that.
         */
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

        await userEventProducer.ProduceUserLoggedIn(phoneNumber, locale, cts.Token).ContinueWith(t => {
            if (t.IsFaulted) {
                // TODO
            }
        }, TaskScheduler.Default);

        return Redirect(oauthState.RedirectUri);
    }

    [HttpGet("login")]
    public async Task<IActionResult> OAuthLogin([FromQuery] string redirectUri, [FromServices] IOptions<OidcSettings> settingsConfig) {

        using var activity = ActivitySource.StartActivity(nameof(OAuthLogin), ActivityKind.Server, HttpContext.Request.GetActivityContext());
        activity?.Start();

        var baseUrl = new Uri($"{settingsConfig.Value.ServerUrl}/realms/{settingsConfig.Value.Realm}/protocol/openid-connect/auth");
        var host = HttpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? HttpContext.Request.Host.Host;
        var port = HttpContext.Request.Headers["X-Forwarded-Port"].FirstOrDefault() ?? HttpContext.Request.Host.Port?.ToString(CultureInfo.InvariantCulture)
                   ?? (HttpContext.Request.Scheme == "https" ? "443" : "80");
        var portSuffix = (port is "80" or "443") ? "" : $":{port}";
        var redirectUrl = new Uri($"{HttpContext.Request.Scheme}://{host}{portSuffix}/auth/redirect");
        var codeVerifier = PkceHelper.GenerateCodeVerifier();
        var codeChallenge = PkceHelper.ComputeCodeChallenge(codeVerifier);
        var state = JsonSerializer.Serialize(new OAuthState(redirectUri, redirectUrl.ToString(), Guid.NewGuid()), OAuthStateSerializerContext.Default.OAuthState);
        var stateKey = Base64UrlEncoder.Encode(state);

        var query = await new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", settingsConfig.Value.ClientId },
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
        return Redirect(redirect);
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
