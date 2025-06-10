namespace Nudges.Webhooks.Endpoints;

public static partial class TwilioWebhookHandlerLogs {
    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing {Type} request")]
    public static partial void LogProcessingRequest(this ILogger<TwilioWebhookHandler> logger, string type);
    [LoggerMessage(Level = LogLevel.Warning, Message = "{Type} request unhandled")]
    public static partial void LogUnhandledRequest(this ILogger<TwilioWebhookHandler> logger, string type);
    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<TwilioWebhookHandler> logger, Exception exception);
    [LoggerMessage(Level = LogLevel.Warning, Message = "{Message}")]
    public static partial void LogErrorWarning(this ILogger<TwilioWebhookHandler> logger, string message);
    [LoggerMessage(Level = LogLevel.Warning, Message = "{Message}")]
    public static partial void LogMissingData(this ILogger<TwilioWebhookHandler> logger, string message);
}
