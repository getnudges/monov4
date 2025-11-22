using Confluent.Kafka;
using KafkaConsumer.GraphQL;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class PriceTierMessageMiddleware(ILogger<PriceTierMessageMiddleware> logger,
                                          Func<INudgesClient> nudgesClientFactory,
                                          IForeignProductService foreignProductService)
    : IMessageMiddleware<PriceTierEventKey, PriceTierChangeEvent> {

    public async Task<MessageContext<PriceTierEventKey, PriceTierChangeEvent>> InvokeAsync(MessageContext<PriceTierEventKey, PriceTierChangeEvent> context, MessageHandler<PriceTierEventKey, PriceTierChangeEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err));
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<PriceTierEventKey, PriceTierChangeEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");

        return await (cr.Message.Value switch {
            PriceTierCreatedEvent createdEvent => HandlePriceTierCreated(createdEvent, cancellationToken),
            PriceTierUpdatedEvent updatedEvent => HandlePriceTierUpdated(updatedEvent, cancellationToken),
            PriceTierDeletedEvent deletedEvent => HandlePriceTierDeleted(deletedEvent, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandlePriceTierCreated(PriceTierCreatedEvent data, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        if (string.IsNullOrEmpty(data.PriceTier.ForeignServiceId)) {
            logger.LogWarning("PriceTierCreatedEvent received with null or empty ForeignServiceId. Cannot create foreign price.");
            return Result.Success<bool, Exception>(true);
        }

        var result = await client.Instance.PatchPriceTier(new PatchPriceTierInput {
            Id = data.PriceTier.ForeignServiceId,
            Name = data.PriceTier.Name,
            Description = data.PriceTier.Description,
            Price = data.PriceTier.Price,
            Duration = data.PriceTier.Duration,
            IconUrl = data.PriceTier.IconUrl,
            Status = data.PriceTier.Status,
            ForeignServiceId = data.PriceTier.ForeignServiceId
        }, cancellationToken);

        return result.Match(Result.Success<bool, Exception>(true), Result.Exception);

    }

    private Task<Result<bool, Exception>> HandlePriceTierUpdated(PriceTierUpdatedEvent data, CancellationToken cancellationToken) {
        return Task.FromResult(Result.Success<bool, Exception>(true));
    }

    private Task<Result<bool, Exception>> HandlePriceTierDeleted(PriceTierDeletedEvent data, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<bool, Exception>(true));

    private async Task<Result<bool, Exception>> DeleteForeignPrice(string id, CancellationToken cancellationToken) {
        var foreignUpdateResult = await foreignProductService.DeleteForeignPrice(id, cancellationToken);
        return foreignUpdateResult.Match(success => {
            logger.LogPriceTierDeleted(id);
            return true;
        }, err => err.Exception?.GetBaseException() ?? new GraphQLException(err.Message));
    }
}
