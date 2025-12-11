using KafkaConsumer.Services;

namespace KafkaConsumer.Notifications;

internal class LocalNotifier(ILogger<LocalNotifier> logger) : INotifier {
    public Task Notify(string phoneNumber, string message, CancellationToken cancellationToken = default) {
        logger.LogMessage(phoneNumber, message);
        return Task.CompletedTask;
    }
}
internal static partial class LocalNotifierLogs {
    [LoggerMessage(Level = LogLevel.Information, Message = "Sending SMS to {PhoneNumber}: {Message}")]
    public static partial void LogMessage(this ILogger<LocalNotifier> logger, string phoneNumber, string message);
}


