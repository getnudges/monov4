using Monads;

namespace Nudges.Webhooks.GraphQL;

public static class NudgesClientExtensions {
    public static async Task<Result<IGetPlan_Plan, Exception>> GetPlan(this INudgesClient client, string planNodeId, CancellationToken cancellationToken) {
        var result = await client.GetPlan.ExecuteAsync(planNodeId, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.Plan is not IGetPlan_Plan plan) {
            return new GraphQLException("Couldn't find plan");
        }
        return Result.Success<IGetPlan_Plan, Exception>(plan);
    }

    public static async Task<Result<string, Exception>> SmsLocaleLookup(this INudgesClient client, string phoneNumber, CancellationToken cancellationToken) {
        var result = await client.SmsLocaleLookup.ExecuteAsync(phoneNumber, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.ClientByPhoneNumber is ISmsLocaleLookup_ClientByPhoneNumber clientSms && !string.IsNullOrEmpty(clientSms.Locale)) {
            return clientSms.Locale;
        }
        if (result.Data?.SubscriberByPhoneNumber is ISmsLocaleLookup_SubscriberByPhoneNumber subSms && !string.IsNullOrEmpty(subSms.Locale)) {
            return subSms.Locale;
        }
        return new NotCustomerException();
    }

    public static async Task<Result<IGetClientByPhoneNumber_ClientByPhoneNumber, Exception>> GetClientByPhoneNumber(this INudgesClient client, string phoneNumber, CancellationToken cancellationToken) {
        var result = await client.GetClientByPhoneNumber.ExecuteAsync(phoneNumber, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.ClientByPhoneNumber is IGetClientByPhoneNumber_ClientByPhoneNumber clientData) {
            return Result.Success<IGetClientByPhoneNumber_ClientByPhoneNumber, Exception>(clientData);
        }
        return new GraphQLException("Couldn't find client");
    }

    public static async Task<Result<Maybe<IGetPlanByForeignId_PlanByForeignId>, Exception>> GetPlanByForeignId(this INudgesClient client, string foreignId, CancellationToken cancellationToken) {
        var result = await client.GetPlanByForeignId.ExecuteAsync(foreignId, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.PlanByForeignId is not IGetPlanByForeignId_PlanByForeignId data) {
            return Result.Success<Maybe<IGetPlanByForeignId_PlanByForeignId>, Exception>(Maybe<IGetPlanByForeignId_PlanByForeignId>.None);
        }
        return Result.Success<Maybe<IGetPlanByForeignId_PlanByForeignId>, Exception>(Maybe.Some(data));
    }

    public static async Task<Result<IGetPriceTierByForeignId_PriceTierByForeignId, Exception>> GetPriceTierByForeignId(this INudgesClient client, string foreignId, CancellationToken cancellationToken) {
        var result = await client.GetPriceTierByForeignId.ExecuteAsync(foreignId, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.PriceTierByForeignId is not IGetPriceTierByForeignId_PriceTierByForeignId data) {
            return new GraphQLException("Couldn't find plan");
        }
        return Result.Success<IGetPriceTierByForeignId_PriceTierByForeignId, Exception>(data);
    }

    public static async Task<Result<IGetClientByCustomerId_ClientByCustomerId, Exception>> GetClientByCustomerId(this INudgesClient client, string customerId, CancellationToken cancellationToken) {
        var result = await client.GetClientByCustomerId.ExecuteAsync(customerId, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.ClientByCustomerId is not IGetClientByCustomerId_ClientByCustomerId plan) {
            return new GraphQLException("Couldn't find client");
        }
        return Result.Success<IGetClientByCustomerId_ClientByCustomerId, Exception>(plan);
    }

    public static async Task<Result<ICreatePlanSubscription_CreatePlanSubscription, Exception>> CreatePlanSubscription(this INudgesClient client, CreatePlanSubscriptionInput input, CancellationToken cancellationToken) {
        var result = await client.CreatePlanSubscription.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.CreatePlanSubscription is not ICreatePlanSubscription_CreatePlanSubscription plan) {
            return new GraphQLException("Couldn't get subscription");
        }
        return Result.Success<ICreatePlanSubscription_CreatePlanSubscription, Exception>(plan);
    }

    public static async Task<Result<ICreatePaymentConfirmation_CreatePaymentConfirmation_PaymentConfirmation, Exception>> CreatePaymentConfirmation(this INudgesClient client, CreatePaymentConfirmationInput input, CancellationToken cancellationToken) {
        var result = await client.CreatePaymentConfirmation.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.CreatePaymentConfirmation?.PaymentConfirmation is not ICreatePaymentConfirmation_CreatePaymentConfirmation_PaymentConfirmation data) {
            return new GraphQLException("Couldn't get confirmation");
        }
        return Result.Success<ICreatePaymentConfirmation_CreatePaymentConfirmation_PaymentConfirmation, Exception>(data);
    }

    public static async Task<Result<IPatchPriceTier_PatchPriceTier_Plan, Exception>> PatchPriceTier(this INudgesClient client, PatchPriceTierInput input, CancellationToken cancellationToken) {
        var result = await client.PatchPriceTier.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.PatchPriceTier?.Errors?.Any() == true) {
            return new GraphQLException($"Unknown error in {nameof(PatchPriceTier)}");
        }
        if (result.Data?.PatchPriceTier?.Plan is not IPatchPriceTier_PatchPriceTier_Plan data) {
            return new GraphQLException("Couldn't get confirmation");
        }
        return Result.Success<IPatchPriceTier_PatchPriceTier_Plan, Exception>(data);
    }

    public static async Task<Result<IPatchPlan_PatchPlan_Plan, Exception>> PatchPlan(this INudgesClient client, PatchPlanInput input, CancellationToken cancellationToken) {
        var result = await client.PatchPlan.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.PatchPlan?.Errors?.Any() == true) {
            return new GraphQLException($"Unknown error in {nameof(PatchPlan)}");
        }
        if (result.Data?.PatchPlan?.Plan is not IPatchPlan_PatchPlan_Plan data) {
            return new GraphQLException("Couldn't get confirmation");
        }
        return Result.Success<IPatchPlan_PatchPlan_Plan, Exception>(data);
    }

    public static async Task<Result<IDeletePriceTier_DeletePriceTier_Plan, Exception>> DeletePriceTier(this INudgesClient client, DeletePriceTierInput input, CancellationToken cancellationToken) {
        var result = await client.DeletePriceTier.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.DeletePriceTier?.Errors?.Any() == true) {
            return new GraphQLException($"Unknown error in {nameof(DeletePriceTier)}");
        }
        if (result.Data?.DeletePriceTier?.Plan is not IDeletePriceTier_DeletePriceTier_Plan data) {
            return new GraphQLException("Couldn't get confirmation");
        }
        return Result.Success<IDeletePriceTier_DeletePriceTier_Plan, Exception>(data);
    }

    public static async Task<Result<IDeletePlan_DeletePlan, Exception>> DeletePlan(this INudgesClient client, DeletePlanInput input, CancellationToken cancellationToken) {
        var result = await client.DeletePlan.ExecuteAsync(input, cancellationToken);
        if (result.Errors.Any()) {
            return new AggregateException(result.Errors.Select(e => e.Exception ?? new GraphQLException(e.Message)));
        }
        if (result.Data?.DeletePlan?.Errors?.Any() == true) {
            return new GraphQLException($"Unknown error in {nameof(DeletePlan)}");
        }
        if (result.Data?.DeletePlan?.Plan is not IDeletePlan_DeletePlan data) {
            return new GraphQLException("Couldn't get confirmation");
        }
        return Result.Success<IDeletePlan_DeletePlan, Exception>(data);
    }
}

public class NotCustomerException() : Exception("It seems you are not a customer of Nudges.");
