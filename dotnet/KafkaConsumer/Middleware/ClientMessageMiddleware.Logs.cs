using Microsoft.Extensions.Logging;

namespace KafkaConsumer.Middleware;

internal static partial class ClientMessageMiddlewareLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "{Message}")]
    public static partial void LogAction(this ILogger<ClientMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<ClientMessageMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message with key {MessageKey} not handled")]
    public static partial void LogMessageUhandled(this ILogger<ClientMessageMiddleware> logger, string messageKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<ClientMessageMiddleware> logger, AggregateException exception);

}
