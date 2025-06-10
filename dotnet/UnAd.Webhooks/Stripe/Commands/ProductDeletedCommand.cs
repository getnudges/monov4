using Monads;
using Stripe;
using UnAd.Webhooks.GraphQL;

namespace UnAd.Webhooks.Stripe.Commands;

internal sealed class ProductDeletedCommand(IUnAdClient unAdClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }
        var result = await unAdClient.GetPlanByForeignId(product.Id, cancellationToken).Map(async plan =>
            await unAdClient.DeletePlan(new DeletePlanInput {
                Id = plan.Id
            }, cancellationToken));

        return result.Match<Maybe<Exception>>(e => Maybe<Exception>.None, e => e.GetBaseException());
    }
}
