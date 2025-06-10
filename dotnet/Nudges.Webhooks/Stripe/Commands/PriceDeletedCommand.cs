using Monads;
using Stripe;
using Nudges.Webhooks.GraphQL;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class PriceDeletedCommand(INudgesClient nudgesClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Price price) {
            return new MissingDataException("Could not find price in event data");
        }
        var result = await nudgesClient.GetPriceTierByForeignId(price.Id, cancellationToken).Map(async tier =>
            await nudgesClient.DeletePriceTier(new DeletePriceTierInput {
                Id = tier.Id
            }, cancellationToken));

        return result.Match<Maybe<Exception>>(e => Maybe<Exception>.None, e => e.GetBaseException());
    }
}
