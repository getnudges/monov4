using Nudges.Data.Products.Models;
using Nudges.Kafka.Events;

namespace ProductApi.Mutations;

public static class EventMappings {

    public static Plan ToPlan(this CreatePlanInput input) =>
        new() {
            Name = input.Name,
            Description = input.Description,
            IsActive = input.ActivateOnCreate,
            IconUrl = input.IconUrl,
            PriceTiers = input.PriceTiers?.Select(tier => new PriceTier {
                Name = tier.Name,
                Price = tier.Price,
                Duration = tier.Duration,
                Description = tier.Description,
                IconUrl = tier.IconUrl,
            }).ToList() ?? [],
            PlanFeature = input.Features is { } feature ? new PlanFeature {
                SupportTier = feature.SupportTier,
                AiSupport = feature.AiSupport,
                MaxMessages = feature.MaxMessages,
            } : default
        };

    public static Nudges.Contracts.Products.Plan ToContract(this Plan plan) =>
        new(
            Id: plan.Id,
            ForeignServiceId: plan.ForeignServiceId,
            Name: plan.Name,
            Description: plan.Description,
            IconUrl: plan.IconUrl,
            IsActive: plan.IsActive ?? false,
            Features: plan.PlanFeature is { } feature ? new Nudges.Contracts.Products.PlanFeature(
                SupportTier: feature.SupportTier,
                AiSupport: feature.AiSupport,
                MaxMessages: feature.MaxMessages) : null,
            PriceTiers: plan.PriceTiers.Select(pt => new Nudges.Contracts.Products.PriceTier(
                Id: pt.Id,
                PlanId: pt.PlanId,
                ForeignServiceId: pt.ForeignServiceId,
                Status: pt.Status,
                Price: pt.Price,
                Duration: pt.Duration,
                Name: pt.Name,
                Description: pt.Description,
                DiscountCodes: [],
                TrialOffers: [],
                IconUrl: pt.IconUrl))?.ToList() ?? []
        );

    public static PlanCreatedEvent ToPlanCreatedEvent(this Plan plan) =>
        new(plan.ToContract());

    public static PlanUpdatedEvent ToPlanUpdatedEvent(this Plan plan) =>
        new(plan.ToContract());
    public static PlanDeletedEvent ToPlanDeletedEvent(this Plan plan, DateTimeOffset deletedAt) =>
        new(plan.ToContract(), deletedAt);

    public static Nudges.Contracts.Products.PriceTier ToContract(this PriceTier tier) =>
        new(
            Id: tier.Id,
            PlanId: tier.PlanId,
            ForeignServiceId: tier.ForeignServiceId,
            Price: tier.Price,
            Duration: tier.Duration,
            Name: tier.Name,
            Description: tier.Description,
            IconUrl: tier.IconUrl,
            Status: tier.Status,
            DiscountCodes: [],
            TrialOffers: []
        );
    public static PriceTierCreatedEvent ToPriceTierCreatedEvent(this PriceTier tier) =>
        new(tier.ToContract());
    public static PriceTierUpdatedEvent ToPriceTierUpdatedEvent(this PriceTier tier) =>
        new(tier.ToContract());
    public static PriceTierDeletedEvent ToPriceTierDeletedEvent(this PriceTier tier, DateTimeOffset deletedAt) =>
        new(tier.ToContract(), deletedAt);
}
