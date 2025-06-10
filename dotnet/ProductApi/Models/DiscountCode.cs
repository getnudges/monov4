using Microsoft.EntityFrameworkCore;
using Nudges.Data.Products;
using Nudges.Data.Products.Models;
using Nudges.Models;

namespace ProductApi.Models;

public class DiscountCodeType : ObjectType<DiscountCode> {
    protected override void Configure(IObjectTypeDescriptor<DiscountCode> descriptor) {
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<ProductDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync();
                var result = await dbContext.DiscountCodes.FindAsync(id);
                return result;
            });
        descriptor.Field(f => f.PriceTierId).ID(nameof(PriceTier));
        descriptor.Field(f => f.Duration).Type<NonNullType<BasicDurationType>>()
            .Resolve(descriptor => descriptor.Parent<PriceTier>().Duration.ToBasicDuration());

    }
}



