using ErrorOr;

namespace Nudges.Webhooks.GraphQL;

public static class NudgesClientExtensions {
    public static async Task<ErrorOr<IGetPlan_Plan>> GetPlan(this INudgesClient client, string planNodeId, CancellationToken cancellationToken) {
        var result = await client.GetPlan.ExecuteAsync(planNodeId, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.Plan is not IGetPlan_Plan plan) {
            return Error.NotFound("Plan.NotFound", "Couldn't find plan");
        }
        return ErrorOrFactory.From(plan);
    }

    public static async Task<ErrorOr<string>> SmsLocaleLookup(this INudgesClient client, string phoneNumber, CancellationToken cancellationToken) {
        var result = await client.SmsLocaleLookup.ExecuteAsync(phoneNumber, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.ClientByPhoneNumber is ISmsLocaleLookup_ClientByPhoneNumber clientSms && !string.IsNullOrEmpty(clientSms.Locale)) {
            return clientSms.Locale;
        }
        if (result.Data?.SubscriberByPhoneNumber is ISmsLocaleLookup_SubscriberByPhoneNumber subSms && !string.IsNullOrEmpty(subSms.Locale)) {
            return subSms.Locale;
        }
        return Error.NotFound("Customer.NotFound", "It seems you are not a customer of Nudges.");
    }

    public static async Task<ErrorOr<IGetClientByPhoneNumber_ClientByPhoneNumber>> GetClientByPhoneNumber(this INudgesClient client, string phoneNumber, CancellationToken cancellationToken) {
        var result = await client.GetClientByPhoneNumber.ExecuteAsync(phoneNumber, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.ClientByPhoneNumber is IGetClientByPhoneNumber_ClientByPhoneNumber clientData) {
            return ErrorOrFactory.From(clientData);
        }
        return Error.NotFound("Client.NotFound", "Couldn't find client");
    }

    public static async Task<ErrorOr<IGetPlanByForeignId_PlanByForeignId>> GetPlanByForeignId(this INudgesClient client, string foreignId, CancellationToken cancellationToken) {
        var result = await client.GetPlanByForeignId.ExecuteAsync(foreignId, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.PlanByForeignId is not IGetPlanByForeignId_PlanByForeignId data) {
            return Error.NotFound("Plan.NotFound", "Couldn't find plan by foreign ID");
        }
        return ErrorOrFactory.From(data);
    }

    public static async Task<ErrorOr<IGetPriceTierByForeignId_PriceTierByForeignId>> GetPriceTierByForeignId(this INudgesClient client, string foreignId, CancellationToken cancellationToken) {
        var result = await client.GetPriceTierByForeignId.ExecuteAsync(foreignId, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.PriceTierByForeignId is not IGetPriceTierByForeignId_PriceTierByForeignId data) {
            return Error.NotFound("PriceTier.NotFound", "Couldn't find plan");
        }
        return ErrorOrFactory.From(data);
    }

    public static async Task<ErrorOr<IGetClientByCustomerId_ClientByCustomerId>> GetClientByCustomerId(this INudgesClient client, string customerId, CancellationToken cancellationToken) {
        var result = await client.GetClientByCustomerId.ExecuteAsync(customerId, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.ClientByCustomerId is not IGetClientByCustomerId_ClientByCustomerId plan) {
            return Error.NotFound("Client.NotFound", "Couldn't find client");
        }
        return ErrorOrFactory.From(plan);
    }

    public static async Task<ErrorOr<ICreatePlanSubscription_CreatePlanSubscription>> CreatePlanSubscription(this INudgesClient client, CreatePlanSubscriptionInput input, CancellationToken cancellationToken) {
        var result = await client.CreatePlanSubscription.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.CreatePlanSubscription is not ICreatePlanSubscription_CreatePlanSubscription plan) {
            return Error.NotFound("Subscription.NotFound", "Couldn't get subscription");
        }
        return ErrorOrFactory.From(plan);
    }

    public static async Task<ErrorOr<ICreatePaymentConfirmation_CreatePaymentConfirmation_PaymentConfirmation>> CreatePaymentConfirmation(this INudgesClient client, CreatePaymentConfirmationInput input, CancellationToken cancellationToken) {
        var result = await client.CreatePaymentConfirmation.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.CreatePaymentConfirmation?.PaymentConfirmation is not ICreatePaymentConfirmation_CreatePaymentConfirmation_PaymentConfirmation data) {
            return Error.NotFound("PaymentConfirmation.NotFound", "Couldn't get confirmation");
        }
        return ErrorOrFactory.From(data);
    }

    public static async Task<ErrorOr<IPatchPriceTier_PatchPriceTier_Plan>> PatchPriceTier(this INudgesClient client, PatchPriceTierInput input, CancellationToken cancellationToken) {
        var result = await client.PatchPriceTier.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.PatchPriceTier?.Errors?.Any() == true) {
            return Error.Failure("PatchPriceTier.UnknownError", $"Unknown error in {nameof(PatchPriceTier)}");
        }
        if (result.Data?.PatchPriceTier?.Plan is not IPatchPriceTier_PatchPriceTier_Plan data) {
            return Error.NotFound("PatchPriceTier.NotFound", "Couldn't get confirmation");
        }
        return ErrorOrFactory.From(data);
    }

    public static async Task<ErrorOr<IPatchPlan_PatchPlan_Plan>> PatchPlan(this INudgesClient client, PatchPlanInput input, CancellationToken cancellationToken) {
        var result = await client.PatchPlan.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.PatchPlan?.Errors?.Any() == true) {
            return Error.Failure("PatchPlan.UnknownError", $"Unknown error in {nameof(PatchPlan)}");
        }
        if (result.Data?.PatchPlan?.Plan is not IPatchPlan_PatchPlan_Plan data) {
            return Error.NotFound("PatchPlan.NotFound", "Couldn't get confirmation");
        }
        return ErrorOrFactory.From(data);
    }

    public static async Task<ErrorOr<IDeletePriceTier_DeletePriceTier_Plan>> DeletePriceTier(this INudgesClient client, DeletePriceTierInput input, CancellationToken cancellationToken) {
        var result = await client.DeletePriceTier.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.DeletePriceTier?.Errors?.Any() == true) {
            return Error.Failure("DeletePriceTier.UnknownError", $"Unknown error in {nameof(DeletePriceTier)}");
        }
        if (result.Data?.DeletePriceTier?.Plan is not IDeletePriceTier_DeletePriceTier_Plan data) {
            return Error.NotFound("DeletePriceTier.NotFound", "Couldn't get confirmation");
        }
        return ErrorOrFactory.From(data);
    }

    public static async Task<ErrorOr<IDeletePlan_DeletePlan>> DeletePlan(this INudgesClient client, DeletePlanInput input, CancellationToken cancellationToken) {
        var result = await client.DeletePlan.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return Error.Failure("GraphQL.ExecutionError", "GraphQL errors occurred", new Dictionary<string, object> {
                ["Errors"] = result.Errors.Select(e => e.Message).ToArray()
            });
        }
        if (result.Data?.DeletePlan?.Errors?.Any() == true) {
            return Error.Failure("DeletePlan.UnknownError", $"Unknown error in {nameof(DeletePlan)}");
        }
        if (result.Data?.DeletePlan?.Plan is not IDeletePlan_DeletePlan data) {
            return Error.NotFound("DeletePlan.NotFound", "Couldn't get confirmation");
        }
        return ErrorOrFactory.From(data);
    }
}

public class NotCustomerException() : Exception("It seems you are not a customer of Nudges.");
