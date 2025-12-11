using ErrorOr;
using Monads;
using Stripe;
using Nudges.Webhooks.GraphQL;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class PriceUpdatedCommand(INudgesClient nudgesClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Price price) {
            return new MissingDataException("Could not find price in event data");
        }
        
        var tierResult = await nudgesClient.GetPriceTierByForeignId(price.Id, cancellationToken);
        
        if (tierResult.IsError) {
            return new GraphQLException(tierResult.FirstError.Description);
        }

        var patchResult = await nudgesClient.PatchPriceTier(new PatchPriceTierInput {
            Id = tierResult.Value.Id,
            Price = price.UnitAmountDecimal / 100,
        }, cancellationToken);

        return patchResult.IsError 
            ? new GraphQLException(patchResult.FirstError.Description)
            : Maybe<Exception>.None;
    }
}
