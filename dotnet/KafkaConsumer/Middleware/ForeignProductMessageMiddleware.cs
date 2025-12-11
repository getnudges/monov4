using Confluent.Kafka;
using KafkaConsumer.Services;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class ForeignProductMessageMiddleware(ILogger<ForeignProductMessageMiddleware> logger,
                                               Func<INudgesClient> nudgesClientFactory)
    : IMessageMiddleware<ForeignProductEventKey, ForeignProductEvent> {

    public async Task<MessageContext<ForeignProductEventKey, ForeignProductEvent>> InvokeAsync(MessageContext<ForeignProductEventKey, ForeignProductEvent> context, MessageHandler<ForeignProductEventKey, ForeignProductEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogMessageHandled(context.ConsumeResult.Message.Key);
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<ForeignProductEventKey, ForeignProductEvent> cr, CancellationToken cancellationToken) {
        logger.LogMessageReceived(cr.Message.Key);
        switch (cr.Message.Value) {
            case ForeignProductCreatedEvent created:
                await HandleForeignProductCreated(created, cancellationToken);
                break;
            case ForeignProductSynchronizedEvent synced:
                await HandleForeignProductSynchronized(synced, cancellationToken);
                break;
            default:
                throw new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.");
        }
    }

    private async Task HandleForeignProductCreated(ForeignProductCreatedEvent createdEvent, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        await client.Instance.CreatePlan(createdEvent.ToCreatePlanInput(), cancellationToken);
    }

    private async Task HandleForeignProductSynchronized(ForeignProductSynchronizedEvent syncEvent, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        var input = syncEvent.ToPatchPlanInput();
        await client.Instance.PatchPlan(input, cancellationToken);
    }
}


public static class EventMappingExtensions {
    public static CreatePlanInput ToCreatePlanInput(this ForeignProductCreatedEvent @event) =>
        new() {
            Name = @event.Name,
            Description = @event.Description,
            ActivateOnCreate = false,
            IconUrl = @event.IconUrl,
        };

    public static PatchPlanInput ToPatchPlanInput(this ForeignProductSynchronizedEvent @event) =>
        new() {
            Id = @event.PlanNodeId,
            ForeignServiceId = @event.ForeignProductId,
            Name = @event.Name,
            Description = @event.Description,
            IconUrl = @event.IconUrl,
            //IsActive = @
            // TODO: etc...
        };
}
