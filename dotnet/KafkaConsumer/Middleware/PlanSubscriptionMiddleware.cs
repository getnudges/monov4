using Confluent.Kafka;
using KafkaConsumer.Services;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class PlanSubscriptionEventMiddleware(ILogger<PlanSubscriptionEventMiddleware> logger,
                                               Func<INudgesClient> nudgesClientFactory,
                                               KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer) : IMessageMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent> {

    public async Task<MessageContext<PlanSubscriptionKey, PlanSubscriptionEvent>> InvokeAsync(MessageContext<PlanSubscriptionKey, PlanSubscriptionEvent> context, MessageHandler<PlanSubscriptionKey, PlanSubscriptionEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully.");
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<PlanSubscriptionKey, PlanSubscriptionEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        switch (cr.Message.Value) {
            case PlanSubscriptionCreatedEvent created:
                await HandlePlanSubscriptionCreated(created, cancellationToken);
                break;
            default:
                throw new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.");
        }
    }

    private async Task HandlePlanSubscriptionCreated(PlanSubscriptionCreatedEvent data, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        await client.Instance.UpdateClient(new UpdateClientInput {
            Id = data.ClientId,
            SubscriptionId = data.PlanSubscriptionId.ToString(),
        }, cancellationToken);

        // send notification (best-effort; allow exception to bubble to be handled by middleware)
        await notificationProducer.Produce(
            NotificationKey.StartSubscription(data.PlanSubscriptionId),
            new StartSubscriptionNotificationEvent(string.Empty, "en-US", new Dictionary<string, string>()),
            cancellationToken);
    }
}
