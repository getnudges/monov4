using System.Diagnostics;
using KafkaConsumer.GraphQL;
using Monads;

namespace KafkaConsumer.Services;

public static class NudgesClientExtensions {
    public static async Task<Result<IGetPlan_Plan, Exception>> GetPlan(this INudgesClient client, string planNodeId, CancellationToken cancellationToken) {
        try {
            var result = await client.GetPlan.ExecuteAsync(planNodeId, cancellationToken);
            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
            }
            if (result.Data?.Plan is not IGetPlan_Plan plan) {
                throw new GraphQLException("GetPriceTier returned no data");
            }
            return Result.Success<IGetPlan_Plan, Exception>(plan);
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<IGetPriceTier_PriceTier, Exception>> GetPriceTier(this INudgesClient client, string priceTierNodeId, CancellationToken cancellationToken) {
        try {
            var result = await client.GetPriceTier.ExecuteAsync(priceTierNodeId, cancellationToken);
            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
            }
            if (result.Data?.PriceTier is not IGetPriceTier_PriceTier priceTier) {
                throw new GraphQLException("GetPriceTier returned no data");
            }
            return Result.Success<IGetPriceTier_PriceTier, Exception>(priceTier);
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<IGetPlanSubscriptionById_PlanSubscriptionById, Exception>> GetPlanSubscriptionById(this INudgesClient client, Guid planSubscriptionId, CancellationToken cancellationToken) {
        try {
            var result = await client.GetPlanSubscriptionById.ExecuteAsync(planSubscriptionId, cancellationToken);
            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.PlanSubscriptionById is not IGetPlanSubscriptionById_PlanSubscriptionById subscriptionById) {
                throw new GraphQLException("GetPlanSubscriptionById returned no data");
            }
            return Result.Success<IGetPlanSubscriptionById_PlanSubscriptionById, Exception>(subscriptionById);
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<bool, Exception>> UpdateClient(this INudgesClient client, UpdateClientInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.UpdateClient.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.UpdateClient?.Errors?.Any() == true) {
                throw new GraphQLException("UpdateClient Error");
            }
            return true;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Maybe<Exception>> PatchPriceTier(this INudgesClient client, PatchPriceTierInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.PatchPriceTier.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.PatchPriceTier?.Errors?.Any() == true) {
                throw new GraphQLException("PatchPriceTier Error");
            }
            return Maybe<Exception>.None;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<bool, Exception>> PatchPlan(this INudgesClient client, PatchPlanInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.PatchPlan.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.PatchPlan?.Errors?.Any() == true) {
                throw new GraphQLException("PatchPlan Error");
            }
            return true;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<bool, Exception>> CreatePlan(this INudgesClient client, CreatePlanInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.CreatePlan.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.CreatePlan?.Errors?.Any() == true) {
                // TODO: figure out how to get the actual errors out of here.
                throw new AggregateException(result.Data.CreatePlan.Errors.Select(e => new GraphQLException(e.ToString())));
            }
            return true;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<IGetClient_Client, Exception>> GetClient(this INudgesClient client, string clientNodeId, CancellationToken cancellationToken) {
        try {
            var result = await client.GetClient.ExecuteAsync(clientNodeId, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
            }
            if (result.Data?.Client is not IGetClient_Client gotClient) {
                throw new MissingDataException($"GetClient returned no data.");
            }
            return Result.Success<IGetClient_Client, Exception>(gotClient);
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<IGetPlanSubscription_PlanSubscription, Exception>> GetPlanSubscription(this INudgesClient client, string planSubscriptionId, CancellationToken cancellationToken) {
        try {
            var result = await client.GetPlanSubscription.ExecuteAsync(planSubscriptionId, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
            }
            if (result.Data?.PlanSubscription is not IGetPlanSubscription_PlanSubscription gotClient) {
                throw new MissingDataException($"GetPlanSubscription returned no data.");
            }
            return Result.Success<IGetPlanSubscription_PlanSubscription, Exception>(gotClient);
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<IGetPriceTierByForeignId_PriceTierByForeignId, Exception>> GetPriceTierByForeignId(this INudgesClient client, string priceForeignServiceId, CancellationToken cancellationToken) {
        try {
            var result = await client.GetPriceTierByForeignId.ExecuteAsync(priceForeignServiceId, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
            }
            if (result.Data?.PriceTierByForeignId is not IGetPriceTierByForeignId_PriceTierByForeignId tier) {
                throw new MissingDataException($"GetPriceTierByForeignId returned no data.");
            }
            return Result.Success<IGetPriceTierByForeignId_PriceTierByForeignId, Exception>(tier);
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }

    public static async Task<Result<ICreatePlanSubscription_CreatePlanSubscription, Exception>> CreatePlanSubscription(this INudgesClient client, CreatePlanSubscriptionInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.CreatePlanSubscription.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
            }
            if (result.Data?.CreatePlanSubscription is not ICreatePlanSubscription_CreatePlanSubscription sub) {
                throw new MissingDataException($"GetPlanSubscription returned no data.");
            }
            return Result.Success<ICreatePlanSubscription_CreatePlanSubscription, Exception>(sub);
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return ex;
        }
    }
}
