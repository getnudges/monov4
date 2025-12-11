using System.Diagnostics;
using KafkaConsumer.GraphQL;
using Monads;

namespace KafkaConsumer.Services;

public static class NudgesClientExtensions {

    public static async Task UpdateClient(this INudgesClient client, UpdateClientInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.UpdateClient.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.UpdateClient?.Errors?.Any() == true) {
                throw new GraphQLException("UpdateClient Error");
            }
            return;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public static async Task PatchPriceTier(this INudgesClient client, PatchPriceTierInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.PatchPriceTier.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.PatchPriceTier?.Errors?.Any() == true) {
                throw new GraphQLException("PatchPriceTier Error");
            }
            return;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public static async Task PatchPlan(this INudgesClient client, PatchPlanInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.PatchPlan.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.PatchPlan?.Errors?.Any() == true) {
                throw new GraphQLException("PatchPlan Error");
            }
            return;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public static async Task CreatePlan(this INudgesClient client, CreatePlanInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.CreatePlan.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => new GraphQLException(e.Message)));
            }
            if (result.Data?.CreatePlan?.Errors?.Any() == true) {
                // TODO: figure out how to get the actual errors out of here.
                throw new AggregateException(result.Data.CreatePlan.Errors.Select(e => new GraphQLException(e.ToString())));
            }
            return;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public static async Task<ICreatePlanSubscription_CreatePlanSubscription> CreatePlanSubscription(this INudgesClient client, CreatePlanSubscriptionInput input, CancellationToken cancellationToken) {
        try {
            var result = await client.CreatePlanSubscription.ExecuteAsync(input, cancellationToken);

            if (result.Errors.Any()) {
                throw new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
            }
            if (result.Data?.CreatePlanSubscription is not ICreatePlanSubscription_CreatePlanSubscription sub) {
                throw new MissingDataException($"GetPlanSubscription returned no data.");
            }
            return sub;
        } catch (OperationCanceledException) {
            throw; // preserve cancellation semantics
        } catch (Exception ex) {
            Activity.Current?.AddException(ex);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
