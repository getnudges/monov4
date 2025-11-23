using Microsoft.Extensions.Logging;
using Nudges.Kafka.Events;
using Stripe;

namespace KafkaConsumer.Middleware;

internal static partial class PlanMessageMiddlewareLogs {

    [LoggerMessage(Level = LogLevel.Information)]
    public static partial void LogAction(this ILogger<PlanMessageMiddleware> logger, string Message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Message with key {Key} received")]
    public static partial void LogMessageReceived(this ILogger<PlanMessageMiddleware> logger, PlanEventKey key);

    [LoggerMessage(Level = LogLevel.Information, Message = "Message with key {Key} handled")]
    public static partial void LogMessageHandled(this ILogger<PlanMessageMiddleware> logger, PlanEventKey key);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<PlanMessageMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message with key {Key} not handled")]
    public static partial void LogMessageUhandled(this ILogger<PlanMessageMiddleware> logger, PlanEventKey key, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<PlanMessageMiddleware> logger, AggregateException exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogStripeException(this ILogger<PlanMessageMiddleware> logger, StripeException exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plan {planId} created")]
    public static partial void LogPlanCreated(this ILogger<PlanMessageMiddleware> logger, string planId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plan Creation Failed: {Message}")]
    public static partial void LogPlanUpdateError(this ILogger<PlanMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plan could not be found: {Message}")]
    public static partial void LogPlanNotFoundError(this ILogger<PlanMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plan {planId} updated")]
    public static partial void LogPlanUpdated(this ILogger<PlanMessageMiddleware> logger, string planId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plan {planId} deleted")]
    public static partial void LogPlanDeleted(this ILogger<PlanMessageMiddleware> logger, string planId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price Tier Update Failed: {Message}")]
    public static partial void LogPriceTierUpdateError(this ILogger<PlanMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price Tier delete Failed: {Message}")]
    public static partial void LogPriceTierDeletedError(this ILogger<PlanMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price Tier {Id} deleted")]
    public static partial void LogPriceTierDeleted(this ILogger<PlanMessageMiddleware> logger, string id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Message to update Price Tier sent: {MessageKey}")]
    public static partial void LogPriceTierUpdateMessageSent(this ILogger<PlanMessageMiddleware> logger, string messageKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Message to create Price Tier sent: {MessageKey}")]
    public static partial void LogPriceTierCreateMessageSent(this ILogger<PlanMessageMiddleware> logger, string messageKey);
}
