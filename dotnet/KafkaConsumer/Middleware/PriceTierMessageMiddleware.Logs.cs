using Microsoft.Extensions.Logging;
using Stripe;

namespace KafkaConsumer.Middleware;

internal static partial class PriceTierHandlerLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "{Message}")]
    public static partial void LogAction(this ILogger<PriceTierMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogException(this ILogger<PriceTierMessageMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message with key {MessageKey} not handled: {Error}")]
    public static partial void LogMessageUhandled(this ILogger<PriceTierMessageMiddleware> logger, string messageKey, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message with key {MessageKey} not handled due to exception")]
    public static partial void LogMessageUhandled(this ILogger<PriceTierMessageMiddleware> logger, string messageKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<PriceTierMessageMiddleware> logger, AggregateException exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogStripeException(this ILogger<PriceTierMessageMiddleware> logger, StripeException exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Product {PriceTierId} created")]
    public static partial void LogPriceTierCreated(this ILogger<PriceTierMessageMiddleware> logger, string pricetierId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price Tier Update Failed: {Message}")]
    public static partial void LogPriceTierUpdateError(this ILogger<PriceTierMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price Tier could not be found: {Message}")]
    public static partial void LogPriceTierNotFoundError(this ILogger<PriceTierMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price Tier {PriceTierId} updated")]
    public static partial void LogPriceTierUpdated(this ILogger<PriceTierMessageMiddleware> logger, string priceTierId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price Tier {PriceTierId} updated")]
    public static partial void LogPriceTierAlreadyExists(this ILogger<PriceTierMessageMiddleware> logger, string priceTierId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price Tier {Id} deleted")]
    public static partial void LogPriceTierDeleted(this ILogger<PriceTierMessageMiddleware> logger, string id);

}
