using Microsoft.EntityFrameworkCore;
using Nudges.Data.Payments;
using Nudges.Data.Payments.Models;

namespace PaymentApi.Types;

public class MerchantServiceType : ObjectType<MerchantService> {
    protected override void Configure(IObjectTypeDescriptor<MerchantService> descriptor) {

        descriptor.Field(f => f.Id).ID(nameof(MerchantService));
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<PaymentDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync();
                var result = await dbContext.MerchantServices.FindAsync(id);
                return result;
            });
    }
}
