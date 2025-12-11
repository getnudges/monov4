using Nudges.Kafka.Events;

namespace KafkaConsumer.Middleware;


internal static partial class ForeignProductMessageMiddlewareLogs {

    [LoggerMessage(Level = LogLevel.Information)]
    public static partial void LogAction(this ILogger<ForeignProductMessageMiddleware> logger, string Message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Message with key {Key} received")]
    public static partial void LogMessageReceived(this ILogger<ForeignProductMessageMiddleware> logger, ForeignProductEventKey key);

    [LoggerMessage(Level = LogLevel.Information, Message = "Message with key {Key} handled")]
    public static partial void LogMessageHandled(this ILogger<ForeignProductMessageMiddleware> logger, ForeignProductEventKey key);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<ForeignProductMessageMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message with key {Key} not handled")]
    public static partial void LogMessageUhandled(this ILogger<ForeignProductMessageMiddleware> logger, ForeignProductEventKey key, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<ForeignProductMessageMiddleware> logger, AggregateException exception);

}
