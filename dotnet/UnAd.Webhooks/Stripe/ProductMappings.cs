using Monads;
using Stripe;

namespace UnAd.Webhooks.Stripe;

internal static class ProductMappings {
    public static PatchPlanInput ToPatchPlanInput(this Product product) =>
        new() {
            Description = product.Description,
            //Features = product.MarketingFeatures. // TODO: deal with this later
            IconUrl = product.Images.FirstOrDefault(),
            Id = product.Metadata["planId"],
            IsActive = product.Active,
            Name = product.Name,
            ForeignServiceId = product.Id
        };

    public static PatchPlanPriceTierInput ToPatchPlanPriceTierInput(this Price price) =>
        new() {
            ForeignServiceId = price.Id,
            Name = price.Nickname ?? "TODO", // TODO
            Price = price.UnitAmountDecimal / 100,
            //Duration = BasicDuration.P7d,
            // TODO: unless this enum serialization issue is solved in HC14,
            //       I need another solution here
            //Status = price.Active ? PriceTierStatus.Active : PriceTierStatus.Inactive,
            //Description = price.Description,
            //IconUrl = price.Image
        };

    public static PatchPriceTierInput ToPatchPriceTierInput(this Price price) =>
        new() {
            ForeignServiceId = price.Id,
            //Description = price.,
            //Duration = price.dur,
            //IconUrl = price.,
            Name = price.Nickname,
            //Status = price.
            Price = price.UnitAmountDecimal / 100
        };
}
