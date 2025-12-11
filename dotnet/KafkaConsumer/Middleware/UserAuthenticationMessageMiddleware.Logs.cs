using Microsoft.Extensions.Logging;
using Nudges.Kafka.Events;

namespace KafkaConsumer.Middleware;

internal static partial class UserAuthenticationMessageMiddlewareLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled message with key {MessageKey}")]
    public static partial void LogMessageHandled(this ILogger<UserAuthenticationMessageMiddleware> logger, UserAuthenticationEventKey messageKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Received message with key {MessageKey}")]
    public static partial void LogMessageReceived(this ILogger<UserAuthenticationMessageMiddleware> logger, UserAuthenticationEventKey messageKey);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<UserAuthenticationMessageMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message with key {MessageKey} not handled")]
    public static partial void LogMessageUnhandled(this ILogger<UserAuthenticationMessageMiddleware> logger, UserAuthenticationEventKey messageKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<UserAuthenticationMessageMiddleware> logger, AggregateException exception);

}
