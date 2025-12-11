using KafkaConsumer.GraphQL;

namespace KafkaConsumer.Services;

public static class NudgesClientExtensions {

    public static async Task<IGetPlanByForeignId_PlanByForeignId> GetPlanByForeignId(this INudgesClient client, string foreignId, CancellationToken cancellationToken) {
        var result = await client.GetPlanByForeignId.ExecuteAsync(foreignId, cancellationToken);
        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.PlanByForeignId is not IGetPlanByForeignId_PlanByForeignId data) {
            throw new ProductNotFoundException(foreignId);
        }
        return data;
    }

    public static async Task<IGetPlanById_PlanById> GetPlanById(this INudgesClient client, int planId, CancellationToken cancellationToken) {
        var result = await client.GetPlanById.ExecuteAsync(planId, cancellationToken);
        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.PlanById is not IGetPlanById_PlanById_Plan data) {
            throw new PlanNotFoundByIdException(planId);
        }
        return data;
    }

    public static async Task<IGetPlan_Plan> GetPlan(this INudgesClient client, string planNodeId, CancellationToken cancellationToken) {
        var result = await client.GetPlan.ExecuteAsync(planNodeId, cancellationToken);
        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.Plan is not IGetPlan_Plan_Plan data) {
            throw new PlanNotFoundException(planNodeId);
        }
        return data;
    }

    public static async Task<IGetPriceTierByForeignId_PriceTierByForeignId> GetPriceTierByForeignId(this INudgesClient client, string foreignId, CancellationToken cancellationToken) {
        var result = await client.GetPriceTierByForeignId.ExecuteAsync(foreignId, cancellationToken);
        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.PriceTierByForeignId is not IGetPriceTierByForeignId_PriceTierByForeignId data) {
            throw new PriceTierNotFoundException(foreignId);
        }
        return data;
    }

    public static async Task<IGetClientByCustomerId_ClientByCustomerId> GetClientByCustomerId(this INudgesClient client, string customerId, CancellationToken cancellationToken) {
        var result = await client.GetClientByCustomerId.ExecuteAsync(customerId, cancellationToken);
        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.ClientByCustomerId is not IGetClientByCustomerId_ClientByCustomerId data) {
            throw new ClientNotFoundException(customerId);
        }
        return data;
    }

    public static async Task DeletePlan(this INudgesClient client, DeletePlanInput input, CancellationToken cancellationToken) {
        var result = await client.DeletePlan.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.DeletePlan?.Errors?.Any() == true) {
            throw new AggregateException(result.Data.DeletePlan.Errors.Select(e => new GraphQLException(e?.ToString() ?? "Unknown Error")));
        }
    }

    public static async Task DeletePriceTier(this INudgesClient client, DeletePriceTierInput input, CancellationToken cancellationToken) {
        var result = await client.DeletePriceTier.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.DeletePriceTier?.Errors?.Any() == true) {
            throw new AggregateException(result.Data.DeletePriceTier.Errors.Select(e => new GraphQLException(e?.ToString() ?? "Unknown Error")));
        }
    }

    public static async Task<string> CreatePaymentConfirmation(this INudgesClient client, CreatePaymentConfirmationInput input, CancellationToken cancellationToken) {
        var result = await client.CreatePaymentConfirmation.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.CreatePaymentConfirmation?.PaymentConfirmation is not ICreatePaymentConfirmation_CreatePaymentConfirmation_PaymentConfirmation data) {
            throw new MissingDataException("CreatePaymentConfirmation returned no data.");
        }
        return data.PaymentConfirmationId;
    }

    public static async Task UpdateClient(this INudgesClient client, UpdateClientInput input, CancellationToken cancellationToken) {
        var result = await client.UpdateClient.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.UpdateClient?.Errors?.Any() == true) {
            throw new AggregateException(result.Data.UpdateClient.Errors.Select(e => new GraphQLException(e?.ToString() ?? "Unknown Error")));
        }
    }

    public static async Task PatchPriceTier(this INudgesClient client, PatchPriceTierInput input, CancellationToken cancellationToken) {
        var result = await client.PatchPriceTier.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.PatchPriceTier?.Errors?.Any() == true) {
            throw new AggregateException(result.Data.PatchPriceTier.Errors.Select(e => new GraphQLException(e?.ToString() ?? "Unknown Error")));
        }
    }

    public static async Task PatchPlan(this INudgesClient client, PatchPlanInput input, CancellationToken cancellationToken) {
        var result = await client.PatchPlan.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.PatchPlan?.Errors?.Any() == true) {
            throw new AggregateException(result.Data.PatchPlan.Errors.Select(e => new GraphQLException(e?.ToString() ?? "Unknown Error")));
        }
    }

    public static async Task CreatePlan(this INudgesClient client, CreatePlanInput input, CancellationToken cancellationToken) {
        var result = await client.CreatePlan.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
        }
        if (result.Data?.CreatePlan?.Errors?.Any() == true) {
            throw new AggregateException(result.Data.CreatePlan.Errors.Select(e => new GraphQLException(e?.ToString() ?? "Unknown Error")));
        }
    }

    public static async Task<ICreatePlanSubscription_CreatePlanSubscription> CreatePlanSubscription(this INudgesClient client, CreatePlanSubscriptionInput input, CancellationToken cancellationToken) {
        var result = await client.CreatePlanSubscription.ExecuteAsync(input, cancellationToken);

        if (result.Errors.Any()) {
            throw new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.CreatePlanSubscription is not ICreatePlanSubscription_CreatePlanSubscription sub) {
            throw new MissingDataException($"GetPlanSubscription returned no data.");
        }
        return sub;
    }
}

public class ProductNotFoundException(string foreignId) : Exception($"Couldn't find Plan by foreign ID: {foreignId}");

public class PlanNotFoundException(string planNodeId) : Exception($"Couldn't find plan by Node ID: {planNodeId}");

public class PlanNotFoundByIdException(int planId) : Exception($"Couldn't find plan by ID: {planId}");

public class PriceTierNotFoundException(string foreignId) : Exception($"Couldn't find price tier by foreign ID: {foreignId}");

public class ClientNotFoundException(string customerId) : Exception($"Couldn't find client by customer ID: {customerId}");
