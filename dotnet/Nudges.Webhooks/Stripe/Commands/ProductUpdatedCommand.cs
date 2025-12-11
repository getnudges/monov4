using ErrorOr;
using Monads;
using Stripe;
using Nudges.Webhooks.GraphQL;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class ProductUpdatedCommand(INudgesClient nudgesClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }

        if (!product.Metadata.TryGetValue("planId", out var planId) || string.IsNullOrEmpty(planId)) {
            return new MissingDataException($"Could not find planId in product {product.Id} metadata");
        }

        var planResult = await nudgesClient.GetPlanByForeignId(product.Id, cancellationToken);
        
        if (planResult.IsError) {
            return new GraphQLException(planResult.FirstError.Description);
        }

        var patchResult = await nudgesClient.PatchPlan(product.ToPatchPlanInput(), cancellationToken);

        return patchResult.IsError 
            ? new GraphQLException(patchResult.FirstError.Description)
            : Maybe<Exception>.None;
    }
}
