using Nudges.Webhooks.Endpoints.Handlers;

namespace Nudges.Webhooks.Twilio.Commands;

internal static partial class AnnouncementConfirmCommandLogs {
    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogSendFailure(this ILogger<AnnouncementConfirmCommand> logger, Exception exception);
}

internal static partial class AnnouncementCommandLogs {
    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogSendFailure(this ILogger<AnnouncementCommand> logger, Exception exception);
}
