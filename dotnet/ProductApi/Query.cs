using Microsoft.EntityFrameworkCore;
using ProductApi.Models;
using UnAd.Auth;
using UnAd.Data.Products;
using UnAd.Data.Products.Models;

namespace ProductApi;

public class Query {
    /// <summary>
    /// Return a plan by id. If the id is null, return null.
    /// </summary>
    /// <remarks>
    /// This nullablility feels gross, but it makes the schema easier to work with. I'm not sure if there's a better way to do this.
    /// </remarks>
    public async ValueTask<Plan?> GetPlan(ProductDbContext context, int? id, CancellationToken cancellationToken) {
        // TODO: make id non-nullable
        try {
            var plan = await context.Plans
                .Include(p => p.PriceTiers)
                .Include(p => p.PlanFeature)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            return plan;
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async ValueTask<DiscountCode?> GetDiscountCode(ProductDbContext context, int? id, CancellationToken cancellationToken) {
        try {
            var plan = await context.DiscountCodes
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            return plan;
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async ValueTask<DiscountCode?> GetDiscountCodeByCode(ProductDbContext context, string code, CancellationToken cancellationToken) {
        try {
            var plan = await context.DiscountCodes
                .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
            return plan;
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public IQueryable<DiscountCode> GetDiscountCodes(ProductDbContext context) => context.DiscountCodes;

    public async ValueTask<Plan?> GetPlanByForeignId(ProductDbContext context, string id, CancellationToken cancellationToken) {
        try {
            var plan = await context.Plans
                .Include(p => p.PriceTiers)
                .Include(p => p.PlanFeature)
                .FirstOrDefaultAsync(p => p.ForeignServiceId == id, cancellationToken);
            return plan;
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async ValueTask<PriceTier?> GetPriceTierByForeignId(ProductDbContext context, string id, CancellationToken cancellationToken) {
        try {
            var plan = await context.PriceTiers
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.ForeignServiceId == id, cancellationToken);
            return plan;
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public IQueryable<Plan> GetPlans(ProductDbContext context, TimeSpan? duration) {
        if (duration is not null) {
            return context.Plans.Where(p => p.PriceTiers.Any(t => t.Duration == duration));
        }
        return context.Plans;
    }

    public async ValueTask<PriceTier?> GetPriceTier(ProductDbContext context, int id, CancellationToken cancellationToken) =>
        await context.PriceTiers.Include(p => p.Plan).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public IQueryable<PlanSubscription> GetPlanSubscriptions(ProductDbContext context) => context.PlanSubscriptions;

    public async Task<PlanSubscription?> GetPlanSubscription(ProductDbContext context, [ID<PlanSubscription>] Guid id) => await context.PlanSubscriptions.FindAsync([id]);

    public async Task<PlanSubscription?> GetPlanSubscriptionById(ProductDbContext context, Guid id) => await context.PlanSubscriptions.FindAsync([id]);

    public async ValueTask<int> GetTotalPlanSubscriptions(ProductDbContext context) => await context.PlanSubscriptions.CountAsync();

    public async ValueTask<int> TotalPlans(ProductDbContext context) => await context.Plans.CountAsync();
}

public sealed class QueryObjectType : ObjectType<Query> {
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor) {
        descriptor.Field(f => f.GetPlan(default!, default!, default!))
            .Authorize(ClaimValues.Roles.Admin, ClaimValues.Roles.Client)
            .Argument("id", a => a.Type<IdType>().ID(nameof(Plan)))
            .Type<PlanType>();
        descriptor.Field(f => f.GetPlans(default!, default))
            .Authorize(ClaimValues.Roles.Admin, ClaimValues.Roles.Client)
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
        descriptor.Field(f => f.GetPlanByForeignId(default!, default!, default!))
            .Authorize(ClaimValues.Roles.Admin, ClaimValues.Roles.Client)
            .Argument("id", a => a.Type<NonNullType<StringType>>())
            .Type<PlanType>();

        descriptor.Field(f => f.GetPriceTierByForeignId(default!, default!, default!))
            .Authorize(ClaimValues.Roles.Admin, ClaimValues.Roles.Client)
            .Argument("id", a => a.Type<NonNullType<StringType>>())
            .Type<PriceTierType>();
        descriptor.Field(f => f.GetPriceTier(default!, default!, default!))
            .Authorize(ClaimValues.Roles.Admin, ClaimValues.Roles.Client)
            .Argument("id", a => a.Type<NonNullType<IdType>>().ID(nameof(PriceTier)))
            .Type<PriceTierType>();

        descriptor.Field(f => f.GetPlanSubscription(default!, default!))
            .Authorize(ClaimValues.Roles.Admin, ClaimValues.Roles.Client)
            .Type<PlanSubscriptionType>();
        descriptor.Field(f => f.GetPlanSubscriptionById(default!, default!))
            .Authorize(ClaimValues.Roles.Admin, ClaimValues.Roles.Client)
            .Type<PlanSubscriptionType>();
        descriptor.Field(f => f.GetPlanSubscriptions(default!))
            .Authorize(ClaimValues.Roles.Admin, ClaimValues.Roles.Client)
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
        descriptor.Field(f => f.GetTotalPlanSubscriptions(default!))
            .Authorize(PolicyNames.Admin);

        descriptor.Field(f => f.GetDiscountCode(default!, default!, default!))
            .Type<DiscountCodeType>()
            .Argument("id", a => a.ID(nameof(DiscountCode)))
            .Authorize(PolicyNames.Admin);
        descriptor.Field(f => f.GetDiscountCodeByCode(default!, default!, default!))
            .Type<DiscountCodeType>()
            .Authorize(PolicyNames.Admin);
        descriptor.Field(f => f.GetDiscountCodes(default!))
            .Authorize(PolicyNames.Admin)
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}



