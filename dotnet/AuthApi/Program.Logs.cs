
internal static partial class HandlerLogs {
    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogException(this ILogger<Program> logger, string message, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not generate OTP")]
    public static partial void LogOtpGenerationException(this ILogger<Program> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not validate OTP")]
    public static partial void LogOtpValidationException(this ILogger<Program> logger, Exception exception);


    [LoggerMessage(Level = LogLevel.Warning, Message = "Login attempt with missing or invalid role claim.  Role Claim {Role} is invalid")]
    public static partial void LogRoleClaimMissing(this ILogger<Program> logger, string? role);


    [LoggerMessage(Level = LogLevel.Warning, Message = "Login attempt with missing or invalid user.  User {Username} is invalid")]
    public static partial void LogInvalidUser(this ILogger<Program> logger, string username);


    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed authentication for user: {Username}")]
    public static partial void LogInvalidUserCredentials(this ILogger<Program> logger, string username);


    [LoggerMessage(Level = LogLevel.Information, Message = "User {Username} successfully logged in")]
    public static partial void LogSuccessfulLogin(this ILogger<Program> logger, string username);


    [LoggerMessage(Level = LogLevel.Warning, Message = "No OTP secret found for phone number: {PhoneNumber}")]
    public static partial void LogMissingOtpSecret(this ILogger<Program> logger, string phoneNumber);


    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid OTP code provided for phone number: {PhoneNumber}")]
    public static partial void LogInvalidOtp(this ILogger<Program> logger, string phoneNumber);


    [LoggerMessage(Level = LogLevel.Warning, Message = "No user found for phone number: {PhoneNumber}")]
    public static partial void LogOtpUserNotFound(this ILogger<Program> logger, string phoneNumber);


    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed authentication for user: {PhoneNumber}")]
    public static partial void LogAuthenticationFailed(this ILogger<Program> logger, string phoneNumber);


    [LoggerMessage(Level = LogLevel.Warning, Message = "User with phone number {PhoneNumber} successfully validated OTP")]
    public static partial void LogSuccessfulOtpValidation(this ILogger<Program> logger, string phoneNumber);


    [LoggerMessage(Level = LogLevel.Information, Message = "User successfully logged out with token ID: {TokenId}")]
    public static partial void LogSuccessfulLogout(this ILogger<Program> logger, string tokenId);


    [LoggerMessage(Level = LogLevel.Warning, Message = "WhoAmI request with missing token ID")]
    public static partial void LogMissingTokenId(this ILogger<Program> logger);


    [LoggerMessage(Level = LogLevel.Warning, Message = "WhoAmI request with missing token ID")]
    public static partial void LogInvalidTokenId(this ILogger<Program> logger);


    [LoggerMessage(Level = LogLevel.Information, Message = "WhoAmI request successful for role: {Role}")]
    public static partial void LogInvalidTokenId(this ILogger<Program> logger, string role);


    [LoggerMessage(Level = LogLevel.Warning, Message = "WhoAmI request failed: role not found in token")]
    public static partial void LogMissingTokenRole(this ILogger<Program> logger);
}
