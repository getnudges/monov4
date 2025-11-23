using Monads;
using Stripe;
using Nudges.Webhooks.GraphQL;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class ProductDeletedCommand(INudgesClient nudgesClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }
        var result = await nudgesClient.GetPlanByForeignId(product.Id, cancellationToken).Map(async maybePlan =>
            // TODO: this should be an event rather than a direct delete.
            maybePlan.Map(async plan => await nudgesClient.DeletePlan(new DeletePlanInput {
                Id = plan.Id
            }, cancellationToken)));

        return result.Match<Maybe<Exception>>(e => Maybe<Exception>.None, e => e);
    }
}
