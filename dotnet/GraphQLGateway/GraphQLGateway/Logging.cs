namespace GraphQLGateway;

internal static partial class Logging {

    [LoggerMessage(Level = LogLevel.Information, Message = "Gateway Starting")]
    public static partial void LogAppStarting(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Forwaring Request: {Url}")]
    public static partial void LogRequest(this ILogger<LoggerExecutionEventListener> logger, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Forward Request Exception")]
    public static partial void LogRequestException(this ILogger<LoggerExecutionEventListener> logger, Exception exception);
}
