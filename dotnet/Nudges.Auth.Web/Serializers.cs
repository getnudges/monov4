using System.Text.Json.Serialization;

namespace Nudges.Auth.Web;

public record ServerTokenRequest(string ApiKey);
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ServerTokenRequest))]
public sealed partial class TokenRequestSerializerContext : JsonSerializerContext { }

public record OtpCredentials(string PhoneNumber, string Code);
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(OtpCredentials))]
public sealed partial class OtpCredentialsSerializerContext : JsonSerializerContext { }

public record OtpResponse(string Code, DateTimeOffset Expiry);
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(OtpResponse))]
public sealed partial class OtpResponseSerializerContext : JsonSerializerContext { }

public record LoginResult(string TokenId);
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LoginResult))]
public sealed partial class LoginResultTypeSerializerContext : JsonSerializerContext { }

public record WhoAmIResult(string Role);
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WhoAmIResult))]
public sealed partial class WhoAmIResultResultTypeSerializerContext : JsonSerializerContext { }

public record ErrorResponse(string Message, string? Stack = null);
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ErrorResponse))]
public sealed partial class ErrorResponseSerializerContext : JsonSerializerContext { }
