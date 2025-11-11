using System.Security.Claims;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Monads;
using Moq;
using Nudges.Auth.Keycloak;
using OtpNet;
using Precision.WarpCache.Grpc.Client;
using Nudges.Auth;
using Nudges.Auth.Web;
using Nudges.Kafka;

namespace AuthApi.Tests;

public class HandlersTests {

    private readonly Mock<ICacheClient<string>> _mockCache;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly Mock<IResponseCookies> _mockResponseCookies;
    private readonly Mock<IAuthenticator> _mockAuthenticator;
    private readonly Mock<IOtpVerifier> _mockVerifier;
    private readonly Mock<IKeycloakOidcClient> _mockTokenClient;

    public HandlersTests() {
        _mockCache = new Mock<ICacheClient<string>>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockResponseCookies = new Mock<IResponseCookies>();
        _mockAuthenticator = new Mock<IAuthenticator>();
        _mockVerifier = new Mock<IOtpVerifier>();
        _mockTokenClient = new Mock<IKeycloakOidcClient>();

        _mockConfiguration.Setup(c => c[AppConfig.ApiTokenRequestKey]).Returns("test");
        _mockConfiguration.Setup(c => c[AppConfig.SimpleAuthSigningKey]).Returns("test");
        _mockConfiguration.Setup(c => c[AppConfig.ServerAuthSigningKey]).Returns("test");
        _mockConfiguration.Setup(c => c[AppConfig.OidcServerUrl]).Returns("test");
        _mockConfiguration.Setup(c => c[AppConfig.OidcRealm]).Returns("test");
        _mockConfiguration.Setup(c => c[AppConfig.OidcClientId]).Returns("test");
        _mockConfiguration.Setup(c => c[AppConfig.OidcClientSercret]).Returns("test");
        _mockConfiguration.Setup(c => c[AppConfig.CacheServerAddress]).Returns("test");
        _mockConfiguration.Setup(c => c[AppConfig.KafkaBrokerList]).Returns("test");

        _mockWebHostEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWebHostEnvironment))).Returns(_mockWebHostEnvironment.Object);

        _mockHttpContext.Setup(c => c.RequestServices).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(s => s.GetService(typeof(IConfiguration))).Returns(_mockConfiguration.Object);
        _mockServiceProvider.Setup(s => s.GetService(typeof(IWebHostEnvironment))).Returns(_mockWebHostEnvironment.Object);
        _mockHttpContext.Setup(c => c.Response.Cookies).Returns(_mockResponseCookies.Object);

    }


    [Fact]
    public async Task ValidateOtpReturnsBadRequestWhenRoleClaimIsMissing() {
        // Arrange
        var credentials = new OtpCredentials("1234567890", "123456");
        _mockHttpContext.Setup(c => c.Request.Headers.TryGetValue("X-Role-Claim", out It.Ref<StringValues>.IsAny)).Returns(false);
        // Act
        var result = await Handlers.ValidateOtp(credentials, _mockVerifier.Object, _mockTokenClient.Object, _mockCache.Object, _mockHttpContext.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<ErrorResponse>>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("Who are you?", errorResponse.Message);
    }

    [Fact]
    public async Task ValidateOtpReturnsBadRequestWhenOtpIsExpiredOrNotGenerated() {
        // Arrange
        var credentials = new OtpCredentials("1234567890", "123456");
        _mockHttpContext.Setup(c => c.Request.Headers.TryGetValue("X-Role-Claim", out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) => {
                value = new StringValues(ClaimValues.Roles.Client);
                return true;
            });
        _mockCache.Setup(c => c.GetAsync($"otp:{credentials.PhoneNumber}:secret", It.IsAny<CancellationToken>())).ReturnsAsync((string)null);

        _mockVerifier.Setup(v => v.ValidateOtp(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        var result = await Handlers.ValidateOtp(credentials, _mockVerifier.Object, _mockTokenClient.Object, _mockCache.Object, _mockHttpContext.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<ErrorResponse>>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("OTP expired or not generated", errorResponse.Message);
    }

    [Fact]
    public async Task ValidateOtpReturnsBadRequestWhenOtpIsInvalid() {
        // Arrange
        var credentials = new OtpCredentials("1234567890", "123456");
        _mockHttpContext.Setup(c => c.Request.Headers.TryGetValue("X-Role-Claim", out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) => {
                value = new StringValues(ClaimValues.Roles.Subscriber);
                return true;
            });
        _mockCache.Setup(c => c.GetAsync($"otp:{credentials.PhoneNumber}:secret", It.IsAny<CancellationToken>())).ReturnsAsync("base32Key");
        var otp = new Totp(Base32Encoding.ToBytes("base32Key"), 300);
        _mockAuthenticator.Setup(a => a.ValidateToken(It.IsAny<string>())).Returns(new ClaimsPrincipal());


        _mockVerifier.Setup(v => v.ValidateOtp(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        // Act
        var result = await Handlers.ValidateOtp(credentials, _mockVerifier.Object, _mockTokenClient.Object, _mockCache.Object, _mockHttpContext.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<ErrorResponse>>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("Invalid OTP", errorResponse.Message);
    }

    [Fact]
    public async Task ValidateOtpReturnsJsonResultWhenRequestIsValid() {
        // Arrange
        var credentials = new OtpCredentials("1234567890", "123456");
        _mockHttpContext.Setup(c => c.Request.Headers.TryGetValue("X-Role-Claim", out It.Ref<StringValues>.IsAny))
            .Returns((string key, out StringValues value) => {
                value = new StringValues(ClaimValues.Roles.Client);
                return true;
            });
        _mockCache.Setup(c => c.GetAsync($"otp:{credentials.PhoneNumber}:secret", It.IsAny<CancellationToken>())).ReturnsAsync("base32Key");
        var otp = new Totp(Base32Encoding.ToBytes("base32Key"), 300);
        _mockTokenClient.Setup(a => a.GetUserTokenAsync(credentials.PhoneNumber, It.IsAny<string>()))
            .Returns(Task.FromResult(Result.Success<TokenResponse, OidcException>(new TokenResponse("", 0))));
        _mockTokenClient.Setup(a => a.CreateUser(It.IsAny<UserRepresentation>()))
            .Returns(Task.FromResult(Maybe<OidcException>.None));


        _mockVerifier.Setup(v => v.ValidateOtp(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        // Act
        var result = await Handlers.ValidateOtp(credentials, _mockVerifier.Object, _mockTokenClient.Object, _mockCache.Object, _mockHttpContext.Object);

        // Assert
        var jsonResult = Assert.IsType<Ok>(result);
        _mockResponseCookies.Verify(cookies => cookies.Append("TokenId", It.IsAny<string>(), It.IsAny<CookieOptions>()), Times.Once);
    }

    [Fact]
    public async Task GenerateOtpShouldStoreOtpInCache() {
        // Arrange
        var notificationMock = new Mock<KafkaMessageProducer<NotificationKey, NotificationEvent>>();
        var httpContext = new DefaultHttpContext();
        var environment = new Mock<IWebHostEnvironment>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        environment.Setup(e => e.EnvironmentName).Returns("Development");
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IWebHostEnvironment)))
                           .Returns(environment.Object);
        httpContext.RequestServices = serviceProviderMock.Object;

        _mockVerifier.Setup(v => v.GetOtp()).Returns(("test", "test"));

        // Act
        var result = await Handlers.GenerateOtp("+1234567890", _mockVerifier.Object, _mockCache.Object, notificationMock.Object, httpContext);

        // Assert
        _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsType<JsonHttpResult<OtpResponse>>(result);
    }

    [Fact]
    public async Task GenerateOtpShouldNotReturnCodeInNonDevelopmentEnvironments() {
        // Arrange
        var notificationMock = new Mock<KafkaMessageProducer<NotificationKey, NotificationEvent>>();
        var httpContext = new DefaultHttpContext();
        var environment = new Mock<IWebHostEnvironment>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        environment.Setup(e => e.EnvironmentName).Returns("Production");
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IWebHostEnvironment)))
                           .Returns(environment.Object);
        httpContext.RequestServices = serviceProviderMock.Object;

        _mockVerifier.Setup(v => v.GetOtp()).Returns(("test", "test"));

        // Act
        var result = await Handlers.GenerateOtp("+1234567890", _mockVerifier.Object, _mockCache.Object, notificationMock.Object, httpContext);

        // Assert
        Assert.IsType<Ok>(result);
    }
}
