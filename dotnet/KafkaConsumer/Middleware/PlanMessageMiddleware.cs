using Confluent.Kafka;
using KafkaConsumer.GraphQL;
using KafkaConsumer.Services;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;
using Stripe;

namespace KafkaConsumer.Middleware;

internal class PlanMessageMiddleware(ILogger<PlanMessageMiddleware> logger,
                                     Func<INudgesClient> nudgesClientFactory,
                                     IForeignProductService foreignProductService) : IMessageMiddleware<PlanEventKey, PlanChangeEvent> {

    public async Task<MessageContext<PlanEventKey, PlanChangeEvent>> InvokeAsync(MessageContext<PlanEventKey, PlanChangeEvent> context, MessageHandler<PlanEventKey, PlanChangeEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogMessageHandled(context.ConsumeResult.Message.Key);
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<PlanEventKey, PlanChangeEvent> cr, CancellationToken cancellationToken) {
        logger.LogMessageReceived(cr.Message.Key);

        switch (cr.Message.Value) {
            case PlanCreatedEvent planCreatedEvent:
                await HandlePlanCreated(planCreatedEvent, cancellationToken);
                break;
            case PlanUpdatedEvent planUpdatedEvent:
                await HandlePlanUpdated(planUpdatedEvent, cancellationToken);
                break;
            //case PlanDeletedEvent planDeletedEvent:
            //    await HandlePlanDeleted(planDeletedEvent.PlanForeignServiceId, cancellationToken);
            //    break;
            default:
                throw new GraphQLException($"Unknown Plan event type: {cr.Message.Value?.GetType().FullName ?? "null"}");
        }
    }

    private async Task HandlePlanCreated(PlanCreatedEvent planCreatedEvent, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        await CreateForeignProduct(planCreatedEvent.ToShopifyProductCreateOptions(), cancellationToken);
    }

    private async Task CreateForeignProduct(ProductCreateOptions plan, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        await foreignProductService.CreateForeignProduct(plan, cancellationToken);
        logger.LogPlanCreated(plan.Metadata["planId"]);

    }

    private Task HandlePlanUpdated(PlanUpdatedEvent data, CancellationToken cancellationToken) {
        throw new HttpRequestException("Simulated Stripe outage");
        //using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        //    return await UpdateForeignProduct(data.Plan.ToForeignProduct(), cancellationToken);
    }

    //private async Task<Result<bool, Exception>> HandlePlanDeleted(string planForeignServiceId, CancellationToken cancellationToken) =>
    //    await DeleteForeignProduct(planForeignServiceId, cancellationToken);

    //private async Task<Result<bool, Exception>> DeleteForeignProduct(string id, CancellationToken cancellationToken) {
    //    var foreignUpdateResult = await foreignProductService.DeleteForeignProduct(id, cancellationToken);
    //    return foreignUpdateResult.Match(success => {
    //        logger.LogPlanDeleted(id);
    //        return true;
    //    }, err => err.Exception?.GetBaseException() ?? new GraphQLException(err.Message));
    //}

    private async Task UpdateForeignProduct(Nudges.Contracts.Products.Plan plan, CancellationToken cancellationToken) {
        await foreignProductService.UpdateForeignProduct(plan, cancellationToken);
        logger.LogPlanUpdated(plan.Id);
    }
}
