using Confluent.Kafka;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class PlanSubscriptionEventMiddleware(ILogger<PlanSubscriptionEventMiddleware> logger,
                                               Func<INudgesClient> nudgesClientFactory,
                                               KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer) : IMessageMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent> {

    public async Task<MessageContext<PlanSubscriptionKey, PlanSubscriptionEvent>> InvokeAsync(MessageContext<PlanSubscriptionKey, PlanSubscriptionEvent> context, MessageHandler<PlanSubscriptionKey, PlanSubscriptionEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken).Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err)); // TODO: handle errors better (maybe retry?)
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<PlanSubscriptionKey, PlanSubscriptionEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        return await (cr.Message.Value switch {
            PlanSubscriptionCreatedEvent created => HandlePlanSubscriptionCreated(created, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandlePlanSubscriptionCreated(PlanSubscriptionCreatedEvent data, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        return await client.Instance.UpdateClient(new UpdateClientInput {
            Id = data.ClientId,
            SubscriptionId = "GET_THIS_FROM_STRIPE", //data.PlanSubscriptionId,
        }, cancellationToken).Map(async _ => {
            try {
                await notificationProducer.Produce(
                    NotificationKey.StartSubscription(data.PlanSubscriptionId),
                    // TODO: populate with real data
                    new StartSubscriptionNotificationEvent(string.Empty, "en-US", []),
                    cancellationToken);
                return Result.Success<bool, Exception>(true);
            } catch (Exception e) {
                return Result.Exception<bool>(e);
            }
        });
    }
}
