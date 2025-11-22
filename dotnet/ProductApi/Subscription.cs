using System.Diagnostics;
using HotChocolate.Authorization;
using Nudges.Auth;
using Nudges.Data.Products;
using Nudges.Data.Products.Models;
using ProductApi.Telemetry;

namespace ProductApi;

[Authorize(PolicyNames.Admin)]
public class Subscription {

    [Subscribe(MessageType = typeof(TracedMessage<Plan>))]
    public async Task<Plan?> OnPlanUpdated(
        [ID<Plan>] int id,
        [EventMessage] TracedMessage<Plan> message,
        ProductDbContext dbContext,
        ITracePropagator tracePropagator,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken) {

        var parent = tracePropagator.Extract(message.Trace);
        using var activity = Mutation.ActivitySource
            .StartConsumerActivity(nameof(OnPlanUpdated), parent);

        var plan = message.Payload;
        activity?.SetTag("plan.id", plan.Id);
        activity?.SetTag("plan.name", plan.Name);
        activity?.SetTag("graphql.subscription.requestedId", id);
        if (httpContextAccessor.HttpContext is { } httpContext) {
            activity?.SetTag("net.sock.client_id", httpContext.Connection.Id);
            activity?.SetTag("graphql.subscriber.user", httpContext.User.Identity?.Name);
        }
        if (plan.Id != id) {
            activity?.SetStatus(ActivityStatusCode.Ok, "Filtered out");
            return default;
        }
        if (plan.PlanFeature is null) {
            await dbContext.Entry(plan).Reference(p => p.PlanFeature).LoadAsync(cancellationToken);
        }
        if (plan.PriceTiers is null) {
            await dbContext.Entry(plan).Collection(p => p.PriceTiers).LoadAsync(cancellationToken);
        }
        activity?.SetStatus(ActivityStatusCode.Ok);
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
