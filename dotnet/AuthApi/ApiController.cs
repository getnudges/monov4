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
using Nudges.Security;
using Nudges.Telemetry;
using Precision.WarpCache.Grpc.Client;

namespace AuthApi;

[ApiController]
[Route("/")]
public sealed class ApiController(IOtpVerifier otpVerifier,
                                  ICacheClient<string> cache,
                                  IOidcClient tokenClient,
                                  KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                                  KafkaMessageProducer<UserAuthenticationEventKey, UserAuthenticationEvent> userEventProducer,
                                  IEncryptionService encryptionService,
                                  HashService hashService,
                                  ILogger<ApiController> logger) : ControllerBase {

    public static readonly ActivitySource ActivitySource = new($"{typeof(ApiController).FullName}");

    [HttpPost("otp")]
    public async Task<IResult> GenerateOtp([FromBody] OtpRequest request) {

        using var activity = ActivitySource.StartActivity(nameof(ValidateOtp), ActivityKind.Server, HttpContext.Request.GetActivityContext());
        activity?.Start();

        var (code, base32Key) = otpVerifier.GetOtp();

        var phoneHash = hashService.ComputeHash(request.PhoneNumber);
        await cache.SetOtpSecret(phoneHash, base32Key);

        var locale = HttpContext.Request.GetRequestCulture();
        var encryptedPhone = encryptionService.Encrypt(request.PhoneNumber);
        if (string.IsNullOrEmpty(encryptedPhone)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Failed to encrypt phone number.");
            logger.LogError("Failed to encrypt phone number for OTP generation.");
            return Results.InternalServerError(new {
                Message = "Failed to process phone number."
            });
        }
        await notificationProducer.ProduceSendOtp(encryptedPhone, locale, code, HttpContext.RequestAborted);

        // When developing locally, return the OTP code in the response for easier testing
        if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Results.Json(
                new OtpResponse(code, DateTimeOffset.UtcNow.Add(WarpCacheExtensions.OtpExpirationTime)),
                OtpResponseSerializerContext.Default.OtpResponse);
        }
        activity?.SetStatus(ActivityStatusCode.Ok);
        return Results.Ok();
    }

    [HttpPost("otp/verify")]
    public async Task<IResult> ValidateOtp([FromBody] OtpCredentials credentials) {

        using var activity = ActivitySource.StartActivity(nameof(ValidateOtp), ActivityKind.Server, HttpContext.Request.GetActivityContext());
        activity?.Start();

        if (!HttpContext.Request.Headers.TryGetValue("X-Role-Claim", out var role)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing required role claim.");
            logger.LogMissingClaim();
            return Results.BadRequest(new ErrorResponse("Missing required role claim."));
        }

        var roleClaim = role.ToString();
        if (roleClaim is not ClaimValues.Roles.Client and not ClaimValues.Roles.Subscriber) {
            activity?.SetStatus(ActivityStatusCode.Error, $"Invalid role claim: {roleClaim}");
            return Results.BadRequest(new ErrorResponse($"Invalid role: {roleClaim}"));
        }

        var phoneHash = hashService.ComputeHash(credentials.PhoneNumber);
        var key = await cache.GetOtpSecret(phoneHash);
        if (key is null) {
            activity?.SetStatus(ActivityStatusCode.Error, "OTP expired or not generated.");
            return Results.BadRequest(new ErrorResponse("OTP expired or not generated"));
        }

        if (!otpVerifier.ValidateOtp(key, credentials.Code)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid OTP.");
            return Results.BadRequest(new ErrorResponse("Invalid OTP"));
        }

        await cache.RemoveOtpSecret(phoneHash);

        var tempPassword = Guid.NewGuid().ToString("N");

        var encryptedPhone = encryptionService.Encrypt(credentials.PhoneNumber);
        if (string.IsNullOrEmpty(encryptedPhone)) {
            activity?.SetStatus(ActivityStatusCode.Error, "Failed to encrypt phone number.");
            logger.LogError("Failed to encrypt phone number for OTP verification.");
            return Results.InternalServerError(new {
                Message = "Failed to process phone number."
            });
        }

        var userCreate = await CreateNewUser(phoneHash, encryptedPhone, roleClaim, tempPassword);

        var result = await userCreate.ThenAsync(_ =>
            tokenClient.GetUserTokenAsync(phoneHash, tempPassword, HttpContext.RequestAborted)).ThenAsync(async token => {
                var tokenId = Guid.NewGuid().ToString("N");
                await cache.SetToken(tokenId, token.AccessToken, token.ExpiresIn);
                HttpContext.AttachTokenIdCookie(token, tokenId);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Results.Ok();
            }).Else(e => Results.Problem(string.Join(',', e.Select(i => i.Description))));
        return Results.Ok(result.Value);
    }

    private async Task<ErrorOr<Success>> CreateNewUser(string phoneNumberHash, string encryptedPhone, string roleClaim, string tempPassword) {
        var result = await tokenClient.CreateUser(new UserRepresentation {
            Username = phoneNumberHash,
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
                { WellKnownClaims.Locale, [HttpContext.Request.GetRequestCulture()] },
                { "phone", [encryptedPhone] }
            }
        }, HttpContext.RequestAborted);
        return result;
    }

    [HttpGet("redirect")]
    public async Task<IResult> OAuthRedirect([FromQuery] string? code, [FromQuery] string state, [FromQuery] string? error, [FromQuery] string? error_description) {
        using var activity = ActivitySource.StartActivity(nameof(OAuthRedirect), ActivityKind.Server, HttpContext.Request.GetActivityContext());

        if (!string.IsNullOrEmpty(error)) {
            activity?.SetStatus(ActivityStatusCode.Error, $"OAuth error: {error} - {error_description}");
            activity?.AddEvent(new ActivityEvent("oauth.error",
                tags: new ActivityTagsCollection {
                    { "error.type", error },
                    { "error.message", error_description }
                }));
            return Results.BadRequest(new ErrorResponse($"OAuth error: {error} - {error_description}"));
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

        var tokenResponse = await tokenClient.GrantTokenAsync(code!, codeVerifier, oauthState.InternalRedirect, HttpContext.RequestAborted);

        var result = await tokenResponse.ThenAsync(
            r => CreateRedirectResponse(r, state, oauthState))
            .Else(e => {
                var message = string.Join('\n', e.Select(i => i.Description));
                activity?.SetStatus(ActivityStatusCode.Error, $"Failed to grant token: {message}");
                return Results.Problem(message);
            }).ThenDo(_ => activity?.SetStatus(ActivityStatusCode.Ok));

        return result.Value;
    }

    private async Task<IResult> CreateRedirectResponse(TokenResponse token, string state, OAuthState oauthState) {

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
            return Results.Problem("Phone number claim is missing in the token.");
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

        return Results.Redirect(oauthState.RedirectUri);
    }

    [HttpGet("login")]
    public async Task<IResult> OAuthLogin([FromQuery] string redirectUri, [FromServices] IOptions<OidcSettings> settingsConfig) {

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
