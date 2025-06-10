using Confluent.Kafka;
using KafkaConsumer.GraphQL;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class PlanMessageMiddleware(ILogger<PlanMessageMiddleware> logger,
                                     Func<INudgesClient> nudgesClientFactory,
                                     IForeignProductService foreignProductService) : IMessageMiddleware<PlanKey, PlanEvent> {

    public async Task<MessageContext<PlanKey, PlanEvent>> InvokeAsync(MessageContext<PlanKey, PlanEvent> context, MessageHandler<PlanKey, PlanEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err)); // TODO: handle errors better (maybe retry?)
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<PlanKey, PlanEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        return await (cr switch {
            { Message.Key.EventType: nameof(PlanKey.PlanCreated), Message.Key.EventKey: var planNodeId } =>
                HandlePlanCreated(planNodeId, cancellationToken),
            { Message.Key.EventType: nameof(PlanKey.PlanUpdated), Message.Key.EventKey: var planNodeId } =>
                HandlePlanUpdated(planNodeId, cancellationToken),
            { Message.Key.EventType: nameof(PlanKey.PlanDeleted), Message.Key.EventKey: var planNodeId } =>
                HandlePlanDeleted(planNodeId, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandlePlanCreated(string planNodeId, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        var planResult = await client.Instance.GetPlan(planNodeId, cancellationToken);

        return await planResult.Map(plan => CreateForeignProduct(plan, cancellationToken));
    }

    private async Task<Result<bool, Exception>> CreateForeignProduct(IGetPlan_Plan plan, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        var foreignCreateResult = await foreignProductService.CreateForeignProduct(plan, cancellationToken);
        return await foreignCreateResult.Map(async foreignId =>
            await client.Instance.PatchPlan(new PatchPlanInput {
                Id = plan.Id,
                ForeignServiceId = foreignId,
                PriceTiers = [.. plan.PriceTiers.Select(tier => new PatchPlanPriceTierInput {
                        Id = tier.Id,
                        ForeignServiceId = tier.ForeignServiceId,
                    })],
            }, cancellationToken), err => {
                logger.LogPlanUpdateError(err.Exception?.Message ?? err.Message);
                return err.Exception?.GetBaseException() ?? new GraphQLException(err.Message);
            });
    }

    private async Task<Result<bool, Exception>> HandlePlanUpdated(string planNodeId, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        var planResult = await client.Instance.GetPlan(planNodeId, cancellationToken);

        return await planResult.Map(async plan => {
            if (string.IsNullOrEmpty(plan.ForeignServiceId)) {
                return await CreateForeignProduct(plan, cancellationToken);
            }
            return await UpdateForeignProduct(plan, cancellationToken);
        });
    }

    private async Task<Result<bool, Exception>> HandlePlanDeleted(string planForeignServiceId, CancellationToken cancellationToken) =>
        await DeleteForeignProduct(planForeignServiceId, cancellationToken);

    private async Task<Result<bool, Exception>> DeleteForeignProduct(string id, CancellationToken cancellationToken) {
        var foreignUpdateResult = await foreignProductService.DeleteForeignProduct(id, cancellationToken);
        return foreignUpdateResult.Match(success => {
            logger.LogPlanDeleted(id);
            return true;
        }, err => err.Exception?.GetBaseException() ?? new GraphQLException(err.Message));
    }


    private async Task<Result<bool, Exception>> UpdateForeignProduct(IGetPlan_Plan plan, CancellationToken cancellationToken) {
        var foreignUpdateResult = await foreignProductService.UpdateForeignProduct(plan, cancellationToken);
        return foreignUpdateResult.Match(success => {
            logger.LogPlanUpdated(plan.Id);
            return true;
        }, err => err.Exception?.GetBaseException() ?? new GraphQLException(err.Message));
    }
}
