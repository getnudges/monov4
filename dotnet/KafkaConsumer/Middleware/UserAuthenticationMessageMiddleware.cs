using Confluent.Kafka;
using KafkaConsumer.GraphQL;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class UserAuthenticationMessageMiddleware(ILogger<UserAuthenticationMessageMiddleware> logger,
                                                   KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer)
    : IMessageMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent> {

    public async Task<MessageContext<UserAuthenticationEventKey, UserAuthenticationEvent>> InvokeAsync(MessageContext<UserAuthenticationEventKey, UserAuthenticationEvent> context, MessageHandler<UserAuthenticationEventKey, UserAuthenticationEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogMessageHandled(context.ConsumeResult.Message.Key);
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<UserAuthenticationEventKey, UserAuthenticationEvent> cr, CancellationToken cancellationToken) {
        logger.LogMessageReceived(cr.Message.Key);

        switch (cr.Message.Value) {
            case UserLoggedInEvent userLoggedInEvent:
                await HandleUserLoggedIn(userLoggedInEvent, cancellationToken);
                break;
            default:
                throw new GraphQLException($"Unknown UserAuthentication event type: {cr.Message.Value?.GetType().FullName ?? "null"}");
        }
    }

    private async Task HandleUserLoggedIn(UserLoggedInEvent userData, CancellationToken cancellationToken) {
        throw new Exception("Simulated Notification Service outage");
        await notificationProducer.ProduceSendSms(userData.PhoneNumber, "UserLoggedIn", userData.Locale, [], cancellationToken);
    }
}
