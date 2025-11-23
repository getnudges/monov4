using System.Globalization;
using Confluent.Kafka;
using KafkaConsumer.GraphQL;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;
using Stripe;

namespace KafkaConsumer.Middleware;

internal class PlanMessageMiddleware(ILogger<PlanMessageMiddleware> logger,
                                     Func<INudgesClient> nudgesClientFactory,
                                     IForeignProductService foreignProductService) : IMessageMiddleware<PlanEventKey, PlanChangeEvent> {

    public async Task<MessageContext<PlanEventKey, PlanChangeEvent>> InvokeAsync(MessageContext<PlanEventKey, PlanChangeEvent> context, MessageHandler<PlanEventKey, PlanChangeEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogMessageHandled(context.ConsumeResult.Message.Key),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key, err));
        if (result.Error is not null) {
            throw result.Error;
        }

        return context with { Failure = FailureType.None };
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<PlanEventKey, PlanChangeEvent> cr, CancellationToken cancellationToken) {
        logger.LogMessageReceived(cr.Message.Key);
        return await (cr.Message.Value switch {
            PlanCreatedEvent created => HandlePlanCreated(created, cancellationToken),
            PlanUpdatedEvent updated => HandlePlanUpdated(updated, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandlePlanCreated(PlanCreatedEvent planCreatedEvent, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        return await CreateForeignProduct(planCreatedEvent.ToShopifyProductCreateOptions(), cancellationToken);
    }

    private async Task<Result<bool, Exception>> CreateForeignProduct(ProductCreateOptions plan, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        var foreignCreateResult = await foreignProductService.CreateForeignProduct(plan, cancellationToken);

        return await foreignCreateResult.Map(async foreignId =>
            await client.Instance.PatchPlan(new PatchPlanInput {
                Id = Convert.ToInt32(plan.Metadata["planId"], CultureInfo.InvariantCulture),
                ForeignServiceId = foreignId,
            }, cancellationToken), err => {
                logger.LogPlanUpdateError(err.Exception?.Message ?? err.Message);
                return err.Exception?.GetBaseException() ?? new GraphQLException(err.Message);
            });
    }

    private Task<Result<bool, Exception>> HandlePlanUpdated(PlanUpdatedEvent data, CancellationToken cancellationToken) {
        return Task.FromResult(Result.Success<bool, Exception>(true));
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

    private async Task<Result<bool, Exception>> UpdateForeignProduct(IGetPlan_Plan plan, CancellationToken cancellationToken) {
        var foreignUpdateResult = await foreignProductService.UpdateForeignProduct(plan, cancellationToken);
        return foreignUpdateResult.Match(success => {
            logger.LogPlanUpdated(plan.Id);
            return true;
        }, err => err.Exception?.GetBaseException() ?? new GraphQLException(err.Message));
    }
}
