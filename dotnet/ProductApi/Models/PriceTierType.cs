using Microsoft.EntityFrameworkCore;
using Nudges.Data.Products;
using Nudges.Data.Products.Models;
using Nudges.Models;

namespace ProductApi.Models;

public class PriceTierType : ObjectType<PriceTier> {
    protected override void Configure(IObjectTypeDescriptor<PriceTier> descriptor) {
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<ProductDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync();
                var result = await dbContext.PriceTiers.FindAsync(id);
                return result;
            });
        descriptor.Field(f => f.PlanId).ID(nameof(Plan));
        descriptor.Field(f => f.Duration).Type<NonNullType<BasicDurationType>>()
            .Resolve(descriptor => descriptor.Parent<PriceTier>().Duration.ToBasicDuration());
        descriptor.Field(f => f.Status).Type<NonNullType<BasicStatusType>>()
            .Resolve(descriptor => descriptor.Parent<PriceTier>().Status.ToPriceTierStatus());
        //descriptor.Field(f => f.DiscountCodes)
        //    .Type<NonNullType<ListType<NonNullType<DiscountCodeType>>>>()
        //    .Resolve(async (context, cancellationToken) => {
        //        var factory = context.Service<IDbContextFactory<ProductDbContext>>();
        //        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        //        var result = dbContext.DiscountCodes.Where(c => c.PriceTierId == context.Parent<PriceTier>().Id).ToArrayAsync(cancellationToken);
        //        return result;
        //    });
    }
}
