using Microsoft.EntityFrameworkCore;
using Nudges.Data.Users;
using Nudges.Data.Users.Models;

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
        descriptor.Field(s => s.Id).ID<Subscriber>();
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<UserDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync(context.RequestAborted);
                var result = await dbContext.Subscribers.FindAsync([id], context.RequestAborted);
                return result;
            });
        descriptor.Ignore(f => f.IdNavigation);
        descriptor.Field("subscriptionCount")
            .ResolveWith<SubscriberType>(r => r.GetSubscriptionCount(default!, default!));
        descriptor.Field(s => s.Clients)
            .Type<ClientType>()
            .ResolveWith<SubscriberType>(r => r.GetClients(default!, default!))
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
        descriptor.Field("phoneNumberHash").Resolve(async p => {
            var subscriber = p.Parent<Subscriber>();
            if (subscriber.IdNavigation is null) {
                var factory = p.Service<IDbContextFactory<UserDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync(p.RequestAborted);
                await dbContext.Entry(subscriber).Reference(c => c.IdNavigation).LoadAsync();
            }

            return subscriber.IdNavigation!.PhoneNumberHash;
        }).Type<NonNullType<StringType>>();
        descriptor.Field("locale").Resolve(async p => {
            var subscriber = p.Parent<Subscriber>();
            if (subscriber.IdNavigation is null) {
                var factory = p.Service<IDbContextFactory<UserDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync(p.RequestAborted);
                await dbContext.Entry(subscriber).Reference(c => c.IdNavigation).LoadAsync();
            }

            return subscriber.IdNavigation!.Locale;
        }).Type<NonNullType<StringType>>();

    }
}



