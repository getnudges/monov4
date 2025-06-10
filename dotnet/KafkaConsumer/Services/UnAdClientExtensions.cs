using KafkaConsumer.GraphQL;
using Monads;

namespace KafkaConsumer.Services;

public static class UnAdClientExtensions {
    public static async Task<Result<IGetPlan_Plan, Exception>> GetPlan(this IUnAdClient client, string planNodeId, CancellationToken cancellationToken) {
        var result = await client.GetPlan.ExecuteAsync(planNodeId, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.Plan is not IGetPlan_Plan plan) {
            return new GraphQLException("Couldn't find plan");
        }
        return Result.Success<IGetPlan_Plan, Exception>(plan);
    }

    public static async Task<Result<IGetPriceTier_PriceTier, Exception>> GetPriceTier(this IUnAdClient client, string priceTierNodeId, CancellationToken cancellationToken) {
        var result = await client.GetPriceTier.ExecuteAsync(priceTierNodeId, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.PriceTier is not IGetPriceTier_PriceTier priceTier) {
            return new GraphQLException("Couldn't find price tier");
        }
        return Result.Success<IGetPriceTier_PriceTier, Exception>(priceTier);
    }

    public static async Task<Result<IGetPlanSubscriptionById_PlanSubscriptionById, Exception>> GetPlanSubscriptionById(this IUnAdClient client, Guid planSubscriptionId, CancellationToken cancellationToken) {
        var result = await client.GetPlanSubscriptionById.ExecuteAsync(planSubscriptionId, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.PlanSubscriptionById is not IGetPlanSubscriptionById_PlanSubscriptionById subscriptionById) {
            return new GraphQLException("Couldn't find plan subscription");
        }
        return Result.Success<IGetPlanSubscriptionById_PlanSubscriptionById, Exception>(subscriptionById);
    }

    public static async Task<Result<bool, Exception>> UpdateClient(this IUnAdClient client, UpdateClientInput input, CancellationToken cancellationToken) {
        var result = await client.UpdateClient.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.UpdateClient?.Errors?.Any() == true) {
            return new GraphQLException("UpdateClient Error");
        }
        return true;
    }

    public static async Task<Maybe<Exception>> PatchPriceTier(this IUnAdClient client, PatchPriceTierInput input, CancellationToken cancellationToken) {
        var result = await client.PatchPriceTier.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.PatchPriceTier?.Errors?.Any() == true) {
            return new GraphQLException("PatchPriceTier Error");
        }
        return Maybe<Exception>.None;
    }

    public static async Task<Result<bool, Exception>> PatchPlan(this IUnAdClient client, PatchPlanInput input, CancellationToken cancellationToken) {
        var result = await client.PatchPlan.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.PatchPlan?.Errors?.Any() == true) {
            return new GraphQLException("PatchPlan Error");
        }
        return true;
    }

    public static async Task<Result<IGetClient_Client, Exception>> GetClient(this IUnAdClient client, string clientNodeId, CancellationToken cancellationToken) {
        var result = await client.GetClient.ExecuteAsync(clientNodeId, cancellationToken);

        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.Client is not IGetClient_Client gotClient) {
            return new MissingDataException($"GetClient returned no data.");
        }
        return Result.Success<IGetClient_Client, Exception>(gotClient);
    }

    public static async Task<Result<IGetPlanSubscription_PlanSubscription, Exception>> GetPlanSubscription(this IUnAdClient client, string planSubscriptionId, CancellationToken cancellationToken) {
        var result = await client.GetPlanSubscription.ExecuteAsync(planSubscriptionId, cancellationToken);

        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.PlanSubscription is not IGetPlanSubscription_PlanSubscription gotClient) {
            return new MissingDataException($"GetPlanSubscription returned no data.");
        }
        return Result.Success<IGetPlanSubscription_PlanSubscription, Exception>(gotClient);
    }

    public static async Task<Result<IGetPriceTierByForeignId_PriceTierByForeignId, Exception>> GetPriceTierByForeignId(this IUnAdClient client, string priceForeignServiceId, CancellationToken cancellationToken) {
        var result = await client.GetPriceTierByForeignId.ExecuteAsync(priceForeignServiceId, cancellationToken);

        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.PriceTierByForeignId is not IGetPriceTierByForeignId_PriceTierByForeignId tier) {
            return new MissingDataException($"GetPlanSubscription returned no data.");
        }
        return Result.Success<IGetPriceTierByForeignId_PriceTierByForeignId, Exception>(tier);
    }

    public static async Task<Result<ICreatePlanSubscription_CreatePlanSubscription, Exception>> CreatePlanSubscription(this IUnAdClient client, CreatePlanSubscriptionInput input, CancellationToken cancellationToken) {
        var result = await client.CreatePlanSubscription.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.CreatePlanSubscription is not ICreatePlanSubscription_CreatePlanSubscription sub) {
            return new MissingDataException($"GetPlanSubscription returned no data.");
        }
        return Result.Success<ICreatePlanSubscription_CreatePlanSubscription, Exception>(sub);
    }
}
