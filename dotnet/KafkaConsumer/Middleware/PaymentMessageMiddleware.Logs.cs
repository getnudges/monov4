namespace KafkaConsumer.Middleware;

internal static partial class PaymentMessageMiddlewareLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "{Message}")]
    public static partial void LogAction(this ILogger<PaymentMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<PaymentMessageMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message with key {MessageKey} not handled")]
    public static partial void LogMessageUhandled(this ILogger<PaymentMessageMiddleware> logger, string messageKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<PaymentMessageMiddleware> logger, AggregateException exception);

}
