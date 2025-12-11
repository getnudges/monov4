using Confluent.Kafka;
using KafkaConsumer.Services;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;
using Nudges.Localization.Client;

namespace KafkaConsumer.Middleware;

internal class NotificationMessageMiddleware(ILogger<NotificationMessageMiddleware> logger,
                                             ILocalizationClient localizer,
                                             INotifier notifier) : IMessageMiddleware<NotificationKey, NotificationEvent> {

    public async Task<MessageContext<NotificationKey, NotificationEvent>> InvokeAsync(MessageContext<NotificationKey, NotificationEvent> context, MessageHandler<NotificationKey, NotificationEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully.");
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<NotificationKey, NotificationEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        switch (cr.Message.Value) {
            case SendSmsNotificationEvent sendSms:
                await HandleSendSms(cr.Message.Key.EventKey, sendSms.ResourceKey, sendSms.Locale, sendSms.Replacements, cancellationToken);
                break;
            default:
                throw new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.");
        }
    }

    private async Task HandleSendSms(string phoneNumber, string resourceKey, string locale, Dictionary<string, string> replacements, CancellationToken cancellationToken) {
        logger.LogAction($"Handling SendOtp for {phoneNumber}");
        var msg = await localizer.GetLocalizedStringAsync(resourceKey, locale, replacements, cancellationToken);
        await notifier.Notify(phoneNumber, msg, cancellationToken);
    }
}
