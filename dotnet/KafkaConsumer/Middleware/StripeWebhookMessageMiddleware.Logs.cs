using Nudges.Kafka.Events;

namespace KafkaConsumer.Middleware;

internal static partial class StripeWebhookMessageMiddlewareLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "Message with key {Key} received")]
    public static partial void LogMessageReceived(this ILogger<StripeWebhookMessageMiddleware> logger, StripeWebhookKey key);

    [LoggerMessage(Level = LogLevel.Information, Message = "Message with key {Key} handled")]
    public static partial void LogMessageHandled(this ILogger<StripeWebhookMessageMiddleware> logger, StripeWebhookKey key);

    [LoggerMessage(Level = LogLevel.Error, Message = "New plan detected from Stripe for Foreign Product ID: {ForeignProductId}, ignoring")]
    public static partial void LogNewPlanFromStripe(this ILogger<StripeWebhookMessageMiddleware> logger, string foreignProductId, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plan {PlanId} already exists for Foreign Product ID: {ForeignProductId}, producing SYNC event")]
    public static partial void LogPlanAlreadyExists(this ILogger<StripeWebhookMessageMiddleware> logger, string planId, string foreignProductId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductId} updated for Plan {PlanNodeId}")]
    public static partial void LogProductUpdated(this ILogger<StripeWebhookMessageMiddleware> logger, string productId, string planNodeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductId} deleted")]
    public static partial void LogProductDeleted(this ILogger<StripeWebhookMessageMiddleware> logger, string productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price {PriceId} created for Product {ProductId}")]
    public static partial void LogPriceCreated(this ILogger<StripeWebhookMessageMiddleware> logger, string priceId, string productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price {PriceId} updated")]
    public static partial void LogPriceUpdated(this ILogger<StripeWebhookMessageMiddleware> logger, string priceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price {PriceId} deleted")]
    public static partial void LogPriceDeleted(this ILogger<StripeWebhookMessageMiddleware> logger, string priceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Checkout session {SessionId} completed for client {ClientId}")]
    public static partial void LogCheckoutCompleted(this ILogger<StripeWebhookMessageMiddleware> logger, string sessionId, string clientId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message with key {Key} not handled")]
    public static partial void LogMessageUnhandled(this ILogger<StripeWebhookMessageMiddleware> logger, StripeWebhookKey key, Exception exception);
}
