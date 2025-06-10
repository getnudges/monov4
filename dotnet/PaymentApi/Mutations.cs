using System.Diagnostics;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using PaymentApi.Services;
using PaymentApi.Types;
using Nudges.Auth;
using Nudges.Data.Payments;
using Nudges.Data.Payments.Models;
using Nudges.HotChocolate.Utils;
using Nudges.Telemetry;

namespace PaymentApi;

public class Mutation(ILogger<Mutation> logger) {
    public static readonly ActivitySource ActivitySource = new($"{typeof(Mutation).FullName}");

    [Error<CheckoutSessionException>]
    [Error<UnauthorizedException>]
    [Authorize(PolicyNames.Client)]
    public async Task<CheckoutSession> CreateCheckoutSession(IResolverContext resolverContext,
                                                             IPaymentProvider paymentProvider,
                                                             CreateCheckoutSessionInput input,
                                                             HttpContext httpContext,
                                                             INodeIdSerializer idSerializer,
                                                             CancellationToken cancellationToken) {

        using var activity = ActivitySource.StartActivity(nameof(CreateCheckoutSession), ActivityKind.Server, httpContext.Request.GetActivityContext());
        activity?.Start();

        try {
            var checkoutSession = await paymentProvider.CreateCheckoutSessionAsync(
                input.PriceForeignServiceId, input.CustomerId, input.SuccessUrl, input.CancelUrl, cancellationToken);
            // TODO: notify checkout session created
            logger.LogCreatedCheckoutSession(input.CustomerId, input.PriceForeignServiceId);
            return new CheckoutSession(
                checkoutSession.Id,
                checkoutSession.CheckoutUrl,
                checkoutSession.Expiration);
        } catch (CheckoutSessionException ex) {
            activity?.AddException(ex);
            logger.LogCreateCheckoutSessionFailed(input.CustomerId, input.PriceForeignServiceId, ex);
            throw;
        }
    }

    [Authorize(PolicyNames.Client)]
    public async Task<CheckoutSessionCancel> CancelCheckoutSession(IPaymentProvider paymentProvider,
                                                                   CancelCheckoutSessionInput input,
                                                                   CancellationToken cancellationToken) {

        logger.LogCancelingCheckoutSession(input.SessionId);
        await paymentProvider.CancelCheckoutSessionAsync(input.SessionId, cancellationToken);
        logger.LogCanceledCheckoutSession(input.SessionId);
        return new CheckoutSessionCancel(input.SessionId);
    }

    [Authorize(PolicyNames.Admin)]
    [GraphQLType<PaymentConfirmationType>]
    public async Task<PaymentConfirmation> CreatePaymentConfirmation(PaymentDbContext dbContext,
                                                                     CreatePaymentConfirmationInput input,
                                                                     CancellationToken cancellationToken) {

        var newRecord = await dbContext.PaymentConfirmations.AddAsync(new PaymentConfirmation {
            ConfirmationCode = input.ConfirmationId,
            MerchantServiceId = input.MerchantServiceId
        }, cancellationToken);

        try {
            await dbContext.SaveChangesAsync(cancellationToken);
            return newRecord.Entity;
        } catch (Exception ex) {
            throw new CreatePaymentConfirmationException(ex.Message);
        }
    }
}


#region Models
public class UnauthorizedException() : Exception("Unauthorized");

public class CreatePaymentConfirmationException(string message) : Exception(message);

public record CreateCheckoutSessionInput(string CustomerId, string PriceForeignServiceId, Uri SuccessUrl, Uri CancelUrl);

public record CancelCheckoutSessionInput(string SessionId);

public record CreatePaymentConfirmationInput(string ConfirmationId, [ID<MerchantService>] int MerchantServiceId);

public class CreatePaymentConfirmationInputType : InputObjectType<CreatePaymentConfirmationInput> {
    protected override void Configure(IInputObjectTypeDescriptor<CreatePaymentConfirmationInput> descriptor) {
        descriptor.Field(f => f.ConfirmationId).Type<NonNullType<StringType>>();
    }
}

public record CompleteCheckoutSessionInput(string SessionId);

public record CheckoutSession(string Id, Uri CheckoutUrl, DateTimeOffset Expiration);

public record CheckoutSessionCancel(string Id);

public record PaymentProcessedConfirmation([ID<PaymentConfirmation>] Guid Id);

public record CheckoutSessionSuccess(string Id);

public record CreatePriceTierInput(string Name, decimal Price, TimeSpan Duration);

public record PriceTierNotFoundError(int PriceTierId) {
    public string Message => $"Price Tier with ID {PriceTierId} not found";
}
public record InvalidMerchantServiceError(int MerchantServiceId) {
    public string Message => $"Merchant Service with ID {MerchantServiceId} not found";
}

public class CreateCheckoutSessionInputType : InputObjectType<CreateCheckoutSessionInput> {
    protected override void Configure(IInputObjectTypeDescriptor<CreateCheckoutSessionInput> descriptor) {
        descriptor.Field(f => f.PriceForeignServiceId).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.SuccessUrl).Type<NonNullType<UrlType>>();
        descriptor.Field(f => f.CancelUrl).Type<NonNullType<UrlType>>();
    }
}

public class CancelCheckoutSessionInputType : InputObjectType<CancelCheckoutSessionInput> {
    protected override void Configure(IInputObjectTypeDescriptor<CancelCheckoutSessionInput> descriptor) {
        descriptor.Field(f => f.SessionId).Type<NonNullType<StringType>>();
    }
}
#endregion

public class MutationType : ObjectType<Mutation> {
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor) {
        descriptor
            .Field(f => f.CreateCheckoutSession(default!, default!, default!, default!, default!, default!))
            .Use<TracingMiddleware>()
            .Argument("input", a => a.Type<NonNullType<CreateCheckoutSessionInputType>>())
            .UseMutationConvention();
        descriptor
            .Field(f => f.CancelCheckoutSession(default!, default!, default!))
            .Use<TracingMiddleware>()
            .Argument("input", a => a.Type<NonNullType<CancelCheckoutSessionInputType>>())
            .UseMutationConvention();
        descriptor
            .Field(f => f.CreatePaymentConfirmation(default!, default!, default!))
            .Use<TracingMiddleware>()
            .Argument("input", a => a.Type<NonNullType<CreatePaymentConfirmationInputType>>())
            .UseMutationConvention();
    }
}
