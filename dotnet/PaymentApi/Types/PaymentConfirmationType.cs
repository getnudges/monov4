using Microsoft.EntityFrameworkCore;
using Nudges.Data.Payments;
using Nudges.Data.Payments.Models;

namespace PaymentApi.Types;

public class PaymentConfirmationType : ObjectType<PaymentConfirmation> {
    protected override void Configure(IObjectTypeDescriptor<PaymentConfirmation> descriptor) {

        descriptor.Field(f => f.Id).ID(nameof(PaymentConfirmation));
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<PaymentDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync();
                var result = await dbContext.PaymentConfirmations.FindAsync(id);
                return result;
            });
        descriptor.Field("paymentConfirmationId")
            .Deprecated("Use `id` field instead.")
            .Type<NonNullType<StringType>>()
            .Resolve(descriptor => descriptor.Parent<PaymentConfirmation>().Id.ToString());
    }
}
