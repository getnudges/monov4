using ErrorOr;
using Monads;
using Stripe;
using Nudges.Webhooks.GraphQL;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class CheckoutSessionCompletedCommand(INudgesClient nudgesClient, IStripeClient stripeClient) : IEventCommand<StripeEventContext> {
    private readonly InvoiceService _invoiceService = new(stripeClient);
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not global::Stripe.Checkout.Session session) {
            return new MissingDataException("Could not find Session in event data");
        }

        var result = await nudgesClient.GetClientByCustomerId(session.CustomerId, cancellationToken: cancellationToken);
        
        if (result.IsError) {
            return new GraphQLException(result.FirstError.Description);
        }

        var client = result.Value;
        var invoice = await _invoiceService.GetAsync(session.InvoiceId, cancellationToken: cancellationToken);
        
        var confirmationResult = await nudgesClient.CreatePaymentConfirmation(new CreatePaymentConfirmationInput {
            ConfirmationId = invoice.Id,
            MerchantServiceId = 1,
        }, cancellationToken);

        if (confirmationResult.IsError) {
            return new GraphQLException(confirmationResult.FirstError.Description);
        }

        var subscriptionResult = await nudgesClient.CreatePlanSubscription(new CreatePlanSubscriptionInput {
            ClientId = client.Id,
            PaymentConfirmationId = confirmationResult.Value.PaymentConfirmationId,
            PriceTierForeignServiceId = invoice.Lines.First().Id,
        }, cancellationToken);

        return subscriptionResult.IsError 
            ? new GraphQLException(subscriptionResult.FirstError.Description)
            : Maybe<Exception>.None;
    }
}
