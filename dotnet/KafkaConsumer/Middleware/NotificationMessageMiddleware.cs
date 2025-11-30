using Confluent.Kafka;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;
using Nudges.Localization.Client;

namespace KafkaConsumer.Middleware;

internal class NotificationMessageMiddleware(ILogger<NotificationMessageMiddleware> logger,
                                             ILocalizationClient localizer,
                                             INotifier notifier) : IMessageMiddleware<NotificationKey, NotificationEvent> {

    public async Task<MessageContext<NotificationKey, NotificationEvent>> InvokeAsync(MessageContext<NotificationKey, NotificationEvent> context, MessageHandler<NotificationKey, NotificationEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err)); // TODO: handle errors better (maybe retry?)
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<NotificationKey, NotificationEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        return await (cr.Message.Value switch {
            SendSmsNotificationEvent sendSms => HandleSendSms(cr.Message.Key.EventKey, sendSms.ResourceKey, sendSms.Locale, sendSms.Replacements, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandleSendSms(string phoneNumber, string resourceKey, string locale, Dictionary<string, string> replacements, CancellationToken cancellationToken) {
        logger.LogAction($"Handling SendOtp for {phoneNumber}");
        try {
            var msg = await localizer.GetLocalizedStringAsync(resourceKey, locale, replacements, cancellationToken);
            await notifier.Notify(phoneNumber, msg, cancellationToken);
            return true;
        } catch (Exception e) {
            return e;
        }
    }
}
