using HotChocolate.Authorization;
using UnAd.Auth;
using UnAd.Data.Products;
using UnAd.Data.Products.Models;

namespace ProductApi;

public class Subscription {

    [Subscribe(MessageType = typeof(Plan))]
    [Authorize(PolicyNames.Admin)]
    public async Task<Plan?> OnPlanUpdated([ID<Plan>] int id, [EventMessage] Plan plan, ProductDbContext dbContext, CancellationToken cancellationToken) {
        if (plan.Id != id) {
            return default;
        }
        if (plan.PlanFeature is null) {
            await dbContext.Entry(plan).Reference(p => p.PlanFeature).LoadAsync(cancellationToken);
        }
        if (plan.PriceTiers is null) {
            await dbContext.Entry(plan).Collection(p => p.PriceTiers).LoadAsync(cancellationToken);
        }
        return plan;
    }

    [Subscribe(MessageType = typeof(PriceTier))]
    [Authorize(PolicyNames.Admin)]
    public async Task<Plan?> OnPriceTierUpdated([ID<PriceTier>] int id, [EventMessage] PriceTier tier, ProductDbContext dbContext, CancellationToken cancellationToken) {
        if (tier.Id != id) {
            return default!;
        }
        if (tier.Plan is null) {
            await dbContext.Entry(tier).Reference(p => p.Plan).LoadAsync(cancellationToken);
            if (tier.Plan!.PriceTiers is null) {
                await dbContext.Entry(tier.Plan).Collection(p => p.PriceTiers).LoadAsync(cancellationToken);
            }
        }
        return tier.Plan;
    }
}

public class SubscriptionObjectType : ObjectType<Subscription> { }
