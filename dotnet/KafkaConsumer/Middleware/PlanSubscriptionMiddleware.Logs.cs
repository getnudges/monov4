using Microsoft.Extensions.Logging;

namespace KafkaConsumer.Middleware;

internal static partial class PlanSubscriptionMiddlewareLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "{Message}")]
    public static partial void LogAction(this ILogger<PlanSubscriptionEventMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<PlanSubscriptionEventMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message with key {MessageKey} not handled")]
    public static partial void LogMessageUhandled(this ILogger<PlanSubscriptionEventMiddleware> logger, string messageKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<PlanSubscriptionEventMiddleware> logger, AggregateException exception);

}
