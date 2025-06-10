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

        var result = await nudgesClient.GetPlanByForeignId(product.Id, cancellationToken).Map(async plan =>
            await nudgesClient.PatchPlan(product.ToPatchPlanInput(), cancellationToken));

        return result.Match<Maybe<Exception>>(e => Maybe<Exception>.None, e => e.GetBaseException());
    }
}
