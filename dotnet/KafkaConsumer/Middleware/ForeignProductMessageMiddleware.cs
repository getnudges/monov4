using Confluent.Kafka;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class ForeignProductMessageMiddleware(ILogger<ForeignProductMessageMiddleware> logger,
                                               Func<INudgesClient> nudgesClientFactory)
    : IMessageMiddleware<ForeignProductEventKey, ForeignProductEvent> {

    public async Task<MessageContext<ForeignProductEventKey, ForeignProductEvent>> InvokeAsync(MessageContext<ForeignProductEventKey, ForeignProductEvent> context, MessageHandler<ForeignProductEventKey, ForeignProductEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogMessageHandled(context.ConsumeResult.Message.Key),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key, err));
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<ForeignProductEventKey, ForeignProductEvent> cr, CancellationToken cancellationToken) {
        logger.LogMessageReceived(cr.Message.Key);
        return await (cr.Message.Value switch {
            ForeignProductCreatedEvent created =>
                HandleForeignProductCreated(created, cancellationToken),
            ForeignProductSynchronizedEvent synced =>
                HandleForeignProductSynchronized(synced, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandleForeignProductCreated(ForeignProductCreatedEvent createdEvent, CancellationToken cancellationToken) {

        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        return await client.Instance.CreatePlan(createdEvent.ToCreatePlanInput(), cancellationToken);
    }

    private async Task<Result<bool, Exception>> HandleForeignProductSynchronized(ForeignProductSynchronizedEvent syncEvent, CancellationToken cancellationToken) {

        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        return await client.Instance.PatchPlan(syncEvent.ToPatchPlanInput(), cancellationToken);
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
            Id = @event.PlanId,
            ForeignServiceId = @event.ForeignProductId,
            Name = @event.Name,
            Description = @event.Description,
            IconUrl = @event.IconUrl,
            //IsActive = @
            // TODO: etc...
        };
}
