using Monads;
using Stripe;
using UnAd.Webhooks.GraphQL;

namespace UnAd.Webhooks.Stripe.Commands;

internal sealed class PriceUpdatedCommand(IUnAdClient unAdClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Price price) {
            return new MissingDataException("Could not find price in event data");
        }
        var result = await unAdClient.GetPriceTierByForeignId(price.Id, cancellationToken).Map(async tier =>
            await unAdClient.PatchPriceTier(new PatchPriceTierInput {
                Id = tier.Id,
                Price = price.UnitAmountDecimal / 100,
                // TODO: more fields
            }, cancellationToken)
        );

        return result.Match<Maybe<Exception>>(e => Maybe<Exception>.None, e => e.GetBaseException());
    }
}
