using Microsoft.Extensions.Logging;
using Stripe;

namespace UnAd.Stripe;

internal static partial class Logging {

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogStripeVerificationFailure(this ILogger<StripeVerifier> logger, StripeException exception);
}
