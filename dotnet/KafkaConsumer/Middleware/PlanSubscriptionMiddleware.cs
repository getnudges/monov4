using Confluent.Kafka;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class PlanSubscriptionEventMiddleware(ILogger<PlanSubscriptionEventMiddleware> logger,
                                               Func<INudgesClient> nudgesClientFactory,
                                               IForeignProductService foreignProductService,
                                               KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                                               KafkaMessageProducer<ClientKey, ClientEvent> clientEventProducer) : IMessageMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent> {

    public async Task<MessageContext<PlanSubscriptionKey, PlanSubscriptionEvent>> InvokeAsync(MessageContext<PlanSubscriptionKey, PlanSubscriptionEvent> context, MessageHandler<PlanSubscriptionKey, PlanSubscriptionEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken).Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err)); // TODO: handle errors better (maybe retry?)
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<PlanSubscriptionKey, PlanSubscriptionEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        return await (cr switch {
            { Message.Key.EventType: nameof(PlanSubscriptionKey.PlanSubscriptionCreated), Message.Key.EventKey: var planSubscriptionId } =>
                HandlePlanSubscriptionCreated(planSubscriptionId, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandlePlanSubscriptionCreated(Guid planSubscriptionId, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        return await client.Instance.GetPlanSubscriptionById(planSubscriptionId, cancellationToken).Map(async sub =>
            await client.Instance.UpdateClient(new UpdateClientInput {
                Id = sub.ClientId,
                SubscriptionId = planSubscriptionId,
            }, cancellationToken).Map(async _ => {
                try {
                    await notificationProducer.Produce(
                        NotificationKey.StartSubscription(planSubscriptionId.ToString()), NotificationEvent.Empty, cancellationToken);
                    return Result.Success<bool, Exception>(true);
                } catch (Exception e) {
                    return Result.Exception<bool>(e);
                }
            }));
    }
}
