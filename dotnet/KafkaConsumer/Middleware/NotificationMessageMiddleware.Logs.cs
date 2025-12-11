namespace KafkaConsumer.Middleware;

internal static partial class NotificationMessageMiddlewareLogs {
    [LoggerMessage(Level = LogLevel.Debug, Message = "NotificationHandler Starting")]
    public static partial void LogServiceStarting(this ILogger<NotificationMessageMiddleware> logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "NotificationHandler Stopping")]
    public static partial void LogServiceStopping(this ILogger<NotificationMessageMiddleware> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Message}")]
    public static partial void LogAction(this ILogger<NotificationMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Client with ID {ClientNodeId} not found")]
    public static partial void LogClientNotFound(this ILogger<NotificationMessageMiddleware> logger, string clientNodeId);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogException(this ILogger<NotificationMessageMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message with key {MessageKey} not handled")]
    public static partial void LogMessageUhandled(this ILogger<NotificationMessageMiddleware> logger, string messageKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<NotificationMessageMiddleware> logger, AggregateException exception);
}
