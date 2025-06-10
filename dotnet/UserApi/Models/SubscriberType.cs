using Microsoft.EntityFrameworkCore;
using UnAd.Auth;
using UnAd.Data.Users;
using UnAd.Data.Users.Models;

namespace UserApi.Models;

public class SubscriberType : ObjectType<Subscriber> {
    public async Task<int> GetSubscriptionCount([Parent] Subscriber subscriber, UserDbContext dbContext) =>
        await dbContext.Entry(subscriber).Collection(c => c.Clients).Query().CountAsync();

    public async Task<IQueryable<Client>> GetClients([Parent] Subscriber subscriber, UserDbContext dbContext) {
        var collection = dbContext.Entry(subscriber).Collection(c => c.Clients);
        await collection.LoadAsync();
        return collection.Query();
    }

    protected override void Configure(IObjectTypeDescriptor<Subscriber> descriptor) {
        descriptor.Field(s => s.PhoneNumber).ID(nameof(Subscriber));
        descriptor.Field("fullPhone")
            .Resolve(context => context.Parent<Subscriber>().PhoneNumber)
            .Authorize(ClaimValues.Roles.Admin);
        descriptor.Field("maskedPhone").Resolve(context =>
            Util.MaskString(context.Parent<Subscriber>().PhoneNumber, 4));
        descriptor
            .ImplementsNode()
            .IdField(f => f.PhoneNumber)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<UserDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync(context.RequestAborted);
                var result = await dbContext.Subscribers.FindAsync([id], context.RequestAborted);
                return result;
            });
        descriptor.Field("subscriptionCount")
            .ResolveWith<SubscriberType>(r => r.GetSubscriptionCount(default!, default!));
        descriptor.Field(s => s.Clients)
            .Type<ClientType>()
            .ResolveWith<SubscriberType>(r => r.GetClients(default!, default!))
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}



