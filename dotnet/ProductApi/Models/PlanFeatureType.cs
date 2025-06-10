using UnAd.Data.Products.Models;

namespace ProductApi.Models;

public enum PlanSupportTier {
    Basic,
    Standard,
    Premium
}
public class PlanSupportTierType : EnumType<PlanSupportTier> {
}
public class PlanFeatureType : ObjectType<PlanFeature> {
    protected override void Configure(IObjectTypeDescriptor<PlanFeature> descriptor) {
        descriptor.Field(f => f.PlanId).ID(nameof(Plan));
        descriptor.Field(f => f.SupportTier).Type<PlanSupportTierType>();
    }
}



