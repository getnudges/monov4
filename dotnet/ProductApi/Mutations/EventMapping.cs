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

    public static PlanCreatedEvent ToPlanCreatedEvent(this Plan plan) =>
        new(
            PlanId: plan.Id,
            Name: plan.Name,
            Description: plan.Description,
            IconUrl: plan.IconUrl,
            Features: plan.PlanFeature is { } feature ? new PlanFeatureCreatedData(
                SupportTier: feature.SupportTier,
                AiSupport: feature.AiSupport,
                MaxMessages: feature.MaxMessages) : null,
            PriceTiers: plan.PriceTiers.Select(pt => new PriceTierCreatedData(
                PriceTierId: pt.Id,
                Price: pt.Price,
                Duration: pt.Duration,
                Name: pt.Name,
                Description: pt.Description,
                IconUrl: pt.IconUrl))?.ToList() ?? []
        );

    public static PlanUpdatedEvent ToPlanUpdatedEvent(this Plan plan) =>
        new(
            PlanId: plan.Id,
            Name: plan.Name,
            Description: plan.Description,
            IconUrl: plan.IconUrl,
            Features: plan.PlanFeature is { } feature ? new PlanFeatureCreatedData(
                SupportTier: feature.SupportTier,
                AiSupport: feature.AiSupport,
                MaxMessages: feature.MaxMessages) : null,
            PriceTiers: plan.PriceTiers.Select(pt => new PriceTierCreatedData(
                PriceTierId: pt.Id,
                Price: pt.Price,
                Duration: pt.Duration,
                Name: pt.Name,
                Description: pt.Description,
                IconUrl: pt.IconUrl))?.ToList() ?? []
        );
}
