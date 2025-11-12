using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class UserAuthenticationMessageMiddleware(ILogger<UserAuthenticationMessageMiddleware> logger, KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer)
    : IMessageMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent> {

    public async Task<MessageContext<UserAuthenticationEventKey, UserAuthenticationEvent>> InvokeAsync(MessageContext<UserAuthenticationEventKey, UserAuthenticationEvent> context, MessageHandler<UserAuthenticationEventKey, UserAuthenticationEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err));
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<UserAuthenticationEventKey, UserAuthenticationEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        return await (cr switch {
            { Message.Key.EventKey.Type: nameof(UserAuthenticationEventKey.UserLoggedIn), Message.Value: var data } =>
                HandleUserLoggedIn(data, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandleUserLoggedIn(UserAuthenticationEvent userData, CancellationToken cancellationToken) {

        // throw to test the error handling
        throw new ArgumentException("test");

        try {
            var result = await notificationProducer.ProduceSendSms(userData.PhoneNumber, "UserLoggedIn", userData.Locale, [], cancellationToken);
            return true;
        } catch (Exception e) {
            return e;
        }
    }
}
