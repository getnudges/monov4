namespace UnAd.Webhooks;

internal static partial class Logging {

    [LoggerMessage(Level = LogLevel.Critical, Message = "Unexpected Error")]
    internal static partial void LogException(this ILogger<Program> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stripe signature verification failed")]
    internal static partial void LogStripeVerificationFailure(this ILogger<StripeVerifier> logger, Exception exception);
}
