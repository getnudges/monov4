using Microsoft.Extensions.Logging;

namespace KafkaConsumer.Middleware;

internal static partial class UserAuthenticationMessageMiddlewareLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "{Message}")]
    public static partial void LogAction(this ILogger<UserAuthenticationMessageMiddleware> logger, string message);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<UserAuthenticationMessageMiddleware> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message with key {MessageKey} not handled")]
    public static partial void LogMessageUhandled(this ILogger<UserAuthenticationMessageMiddleware> logger, string messageKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogGraphqlClientError(this ILogger<UserAuthenticationMessageMiddleware> logger, AggregateException exception);

}
