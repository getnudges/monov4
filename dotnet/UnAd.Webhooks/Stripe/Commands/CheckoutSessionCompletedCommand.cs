using Monads;
using Stripe;
using UnAd.Webhooks.GraphQL;

namespace UnAd.Webhooks.Stripe.Commands;

internal sealed class CheckoutSessionCompletedCommand(IUnAdClient unAdClient, IStripeClient stripeClient) : IEventCommand<StripeEventContext> {
    private readonly InvoiceService _invoiceService = new(stripeClient);
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not global::Stripe.Checkout.Session session) {
            return new MissingDataException("Could not find Session in event data");
        }

        var result = await unAdClient.GetClientByCustomerId(session.CustomerId, cancellationToken: cancellationToken).Map(async client => {
            var invoice = await _invoiceService.GetAsync(session.InvoiceId, cancellationToken: cancellationToken);
            return await unAdClient.CreatePaymentConfirmation(new CreatePaymentConfirmationInput {
                ConfirmationId = invoice.Id,
                MerchantServiceId = 1, // TODO: this is a placeholder
            }, cancellationToken).Map(async confirmation =>
                await unAdClient.CreatePlanSubscription(new CreatePlanSubscriptionInput {
                    ClientId = client.ClientId,
                    PaymentConfirmationId = confirmation.PaymentConfirmationId,
                    // TODO: First() is not safe here, obviously
                    PriceTierForeignServiceId = invoice.Lines.First().Price.Id,
                }, cancellationToken));
        });

        return result.Match<Maybe<Exception>>(e => Maybe<Exception>.None, e => e.GetBaseException());
    }
}
