using ErrorOr;
using Monads;
using Stripe;
using Nudges.Webhooks.GraphQL;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class PriceCreatedCommand(INudgesClient nudgesClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Price price) {
            return new MissingDataException("Could not find price in event data");
        }
        
        var planResult = await nudgesClient.GetPlanByForeignId(price.ProductId, cancellationToken);
        
        if (planResult.IsError) {
            return new GraphQLException(planResult.FirstError.Description);
        }

        var patchResult = await nudgesClient.PatchPriceTier(price.ToPatchPriceTierInput(), cancellationToken);

        return patchResult.IsError 
            ? new GraphQLException(patchResult.FirstError.Description)
            : Maybe<Exception>.None;
    }
}
