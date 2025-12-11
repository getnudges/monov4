using Confluent.Kafka;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class PriceTierMessageMiddleware(ILogger<PriceTierMessageMiddleware> logger,
                                          Func<INudgesClient> nudgesClientFactory,
                                          IForeignProductService foreignProductService)
    : IMessageMiddleware<PriceTierEventKey, PriceTierChangeEvent> {

    public async Task<MessageContext<PriceTierEventKey, PriceTierChangeEvent>> InvokeAsync(MessageContext<PriceTierEventKey, PriceTierChangeEvent> context, MessageHandler<PriceTierEventKey, PriceTierChangeEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully.");
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<PriceTierEventKey, PriceTierChangeEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");

        switch (cr.Message.Value) {
            case PriceTierCreatedEvent createdEvent:
                await HandlePriceTierCreated(createdEvent, cancellationToken);
                break;
            case PriceTierUpdatedEvent updatedEvent:
                await HandlePriceTierUpdated(updatedEvent, cancellationToken);
                break;
            case PriceTierDeletedEvent deletedEvent:
                await HandlePriceTierDeleted(deletedEvent, cancellationToken);
                break;
            default:
                throw new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.");
        }
    }

    private async Task HandlePriceTierCreated(PriceTierCreatedEvent data, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        if (string.IsNullOrEmpty(data.PriceTier.ForeignServiceId)) {
            logger.LogWarning("PriceTierCreatedEvent received with null or empty ForeignServiceId. Cannot create foreign price.");
            return;
        }

        await client.Instance.PatchPriceTier(new PatchPriceTierInput {
            Id = data.PriceTier.ForeignServiceId,
            Name = data.PriceTier.Name,
            Description = data.PriceTier.Description,
            Price = data.PriceTier.Price,
            Duration = data.PriceTier.Duration,
            IconUrl = data.PriceTier.IconUrl,
            Status = data.PriceTier.Status,
            ForeignServiceId = data.PriceTier.ForeignServiceId
        }, cancellationToken);
    }

    private Task HandlePriceTierUpdated(PriceTierUpdatedEvent data, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    private async Task HandlePriceTierDeleted(PriceTierDeletedEvent data, CancellationToken cancellationToken) {
        var foreignId = data.PriceTier.ForeignServiceId;
        if (string.IsNullOrEmpty(foreignId)) {
            logger.LogWarning("PriceTierDeletedEvent received with null or empty ForeignServiceId. Nothing to delete.");
            return;
        }

        await foreignProductService.DeleteForeignPrice(foreignId, cancellationToken);
        logger.LogPriceTierDeleted(foreignId);
    }

    private async Task DeleteForeignPrice(string id, CancellationToken cancellationToken) {
        await foreignProductService.DeleteForeignPrice(id, cancellationToken);
        logger.LogPriceTierDeleted(id);
    }
}
