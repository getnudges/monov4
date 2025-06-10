using Microsoft.EntityFrameworkCore;
using Nudges.Data.Products;
using Nudges.Data.Products.Models;

namespace ProductApi.Models;

public class PlanType : ObjectType<Plan> {
    protected override void Configure(IObjectTypeDescriptor<Plan> descriptor) {
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<ProductDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync();
                var result = await dbContext.Plans.FindAsync(id);
                return result;
            });
        descriptor.Field(f => f.PlanFeature).Name("features").Type<NonNullType<PlanFeatureType>>();
        descriptor.Field("foreignServiceUrl").Type<UrlType>().Resolve(context =>
            // TODO: make this config-based
            $"https://dashboard.stripe.com/test/products/{context.Parent<Plan>().ForeignServiceId}");

    }
}



