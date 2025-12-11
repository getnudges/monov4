using ErrorOr;
using Monads;
using Stripe;
using Nudges.Webhooks.GraphQL;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class ProductDeletedCommand(INudgesClient nudgesClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }
        
        var planResult = await nudgesClient.GetPlanByForeignId(product.Id, cancellationToken);
        
        if (planResult.IsError) {
            return new GraphQLException(planResult.FirstError.Description);
        }

        var deleteResult = await nudgesClient.DeletePlan(new DeletePlanInput {
            Id = planResult.Value.Id
        }, cancellationToken);

        return deleteResult.IsError 
            ? new GraphQLException(deleteResult.FirstError.Description)
            : Maybe<Exception>.None;
    }
}
