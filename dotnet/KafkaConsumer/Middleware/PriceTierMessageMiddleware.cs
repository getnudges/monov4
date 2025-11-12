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
                                          IForeignProductService foreignProductService) : IMessageMiddleware<PriceTierEventKey, PriceTierEvent> {

    public async Task<MessageContext<PriceTierEventKey, PriceTierEvent>> InvokeAsync(MessageContext<PriceTierEventKey, PriceTierEvent> context, MessageHandler<PriceTierEventKey, PriceTierEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err));
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<PriceTierEventKey, PriceTierEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        return await (cr switch {
            { Message.Key.EventType: nameof(PriceTierEventKey.PriceTierCreated), Message.Key.EventKey: var priceTierNodeId } =>
                HandlePriceTierCreated(priceTierNodeId, cancellationToken),
            { Message.Key.EventType: nameof(PriceTierEventKey.PriceTierUpdated), Message.Key.EventKey: var priceTierNodeId } =>
                HandlePriceTierUpdated(priceTierNodeId, cancellationToken),
            { Message.Key.EventType: nameof(PriceTierEventKey.PriceTierDeleted), Message.Key.EventKey: var priceTierNodeId } =>
                HandlePriceTierDeleted(priceTierNodeId, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandlePriceTierCreated(string priceTierNodeId, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        return await client.Instance.GetPriceTier(priceTierNodeId, cancellationToken).Map(async priceTier =>
            await foreignProductService.GetPriceIdByLookupId(priceTier.Id, cancellationToken).Match(async foreignId => {
                var result = await client.Instance.PatchPriceTier(new PatchPriceTierInput {
                    Id = priceTierNodeId,
                    ForeignServiceId = foreignId,
                }, cancellationToken);
                logger.LogPriceTierCreated(priceTierNodeId);
                var x = result.Match<Result<bool, Exception>>(true, e => e.GetBaseException());
                return x;
            }, () => new MissingDataException("Could not find PriceTier")));
    }

    private async Task<Result<bool, Exception>> HandlePriceTierUpdated(string priceTierNodeId, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        return await client.Instance.GetPriceTier(priceTierNodeId, cancellationToken).Map(async priceTier => {
            var foreignUpdateResult = await foreignProductService.UpdateForeignPrice(priceTier, cancellationToken);
            return foreignUpdateResult.Match(success => {
                logger.LogPriceTierUpdated(priceTierNodeId);
                return true;
            }, err => err.Exception?.GetBaseException() ?? new GraphQLException(err.Message));
        });
    }

    private async Task<Result<bool, Exception>> HandlePriceTierDeleted(string priceTierForeignServiceId, CancellationToken cancellationToken) =>
        await DeleteForeignPrice(priceTierForeignServiceId, cancellationToken);

    private async Task<Result<bool, Exception>> DeleteForeignPrice(string id, CancellationToken cancellationToken) {
        var foreignUpdateResult = await foreignProductService.DeleteForeignPrice(id, cancellationToken);
        return foreignUpdateResult.Match(success => {
            logger.LogPriceTierDeleted(id);
            return true;
        }, err => err.Exception?.GetBaseException() ?? new GraphQLException(err.Message));
    }
}
