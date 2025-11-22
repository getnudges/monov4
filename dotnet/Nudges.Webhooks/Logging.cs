namespace Nudges.Webhooks;

internal static partial class Logging {

    [LoggerMessage(Level = LogLevel.Critical, Message = "Unexpected Error")]
    internal static partial void LogException(this ILogger<Program> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Unauthorized request to {Path}: missing or invalid code parameter")]
    internal static partial void LogUnauthorized(this ILogger<Program> logger, PathString path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stripe signature verification failed")]
    internal static partial void LogStripeVerificationFailure(this ILogger<StripeVerifier> logger, Exception exception);
}
