using Monads;
using Stripe;
using UnAd.Webhooks.GraphQL;

namespace UnAd.Webhooks.Stripe.Commands;

internal sealed class ProductCreatedCommand(IUnAdClient unAdClient) : IEventCommand<StripeEventContext> {
    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }
        var result = await unAdClient.GetPlanByForeignId(product.Id, cancellationToken).Map(async plan => {
            return await unAdClient.PatchPlan(new PatchPlanInput {
                Id = plan.Id,
                IsActive = product.Active,
                PriceTiers = plan.PriceTiers?.Select(pt =>
                    new PatchPlanPriceTierInput { Id = pt.Id, ForeignServiceId = pt.ForeignServiceId }).ToList() ?? [],
                Description = product.Description,
                IconUrl = product.Images.FirstOrDefault(),
                Name = product.Name,
                ForeignServiceId = product.Id,
            }, cancellationToken);
        });

        return result.Match<Maybe<Exception>>(e => Maybe<Exception>.None, e => e.GetBaseException());
    }
}
