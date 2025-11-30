using Microsoft.Extensions.Logging;

namespace Nudges.AuthInit;

internal static partial class AuthInitServiceLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthInitService is stopping.")]
    public static partial void LogServiceStopping(this ILogger<AuthInitService> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthInitService is starting.")]
    public static partial void LogServiceStarting(this ILogger<AuthInitService> logger);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<AuthInitService> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogErrors(this ILogger<AuthInitService> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default admin with ID '{AdminId}' has been stored.")]
    public static partial void LogStoredDefaultAdmin(this ILogger<AuthInitService> logger, Guid adminId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default admin with ID '{AdminId}' already exists.")]
    public static partial void LogSkippingAdmin(this ILogger<AuthInitService> logger, Guid adminId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default user with phone number '{PhoneNumber}' not found.")]
    public static partial void LogDefaultUserNotFound(this ILogger<AuthInitService> logger, string phoneNumber);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default admin user with phone number '{PhoneNumber}' not found in OIDC.")]
    public static partial void LogDefaultAdminNotFound(this ILogger<AuthInitService> logger, string phoneNumber);
}
