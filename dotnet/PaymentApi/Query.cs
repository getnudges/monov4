using Microsoft.EntityFrameworkCore;
using PaymentApi.Types;
using UnAd.Auth;
using UnAd.Data.Payments;
using UnAd.Data.Payments.Models;

namespace PaymentApi;

public class Query {
    public async Task<MerchantService?> GetMerchantService(int id, PaymentDbContext context) => await context.MerchantServices.FindAsync(id);
    public async Task<PaymentConfirmation?> GetPaymentConfirmation(Guid id, PaymentDbContext context) =>
        await context.PaymentConfirmations.FindAsync([id]);
    public async Task<PaymentConfirmation?> GetPaymentConfirmationByCode(string confirmationCode, PaymentDbContext context) =>
        await context.PaymentConfirmations.FirstOrDefaultAsync(c => c.ConfirmationCode == confirmationCode);
    public IQueryable<MerchantService> GetMerchantServices(PaymentDbContext context) => context.MerchantServices;

}

public class QueryType : ObjectType<Query> {

    protected override void Configure(IObjectTypeDescriptor<Query> descriptor) {
        descriptor.Field(f => f.GetMerchantService(default!, default!))
            .Authorize([ClaimValues.Roles.Admin])
            .Argument("id", a => a.ID(nameof(MerchantService)))
            .Type<MerchantServiceType>();

        descriptor.Field(f => f.GetPaymentConfirmationByCode(default!, default!))
            .Authorize([ClaimValues.Roles.Admin])
            .Argument("confirmationCode", a => a.Type<NonNullType<StringType>>())
            .Type<PaymentConfirmationType>();

        descriptor.Field(f => f.GetMerchantServices(default!))
            .Authorize([ClaimValues.Roles.Admin])
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}
