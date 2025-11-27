using Microsoft.EntityFrameworkCore;
using Nudges.Data.Users;
using Nudges.Data.Users.Models;

namespace UserApi.Models;

public class ClientResolvers {

    public async Task<int> GetSubscriberCount([Parent] Client client, UserDbContext dbContext) =>
        await dbContext.Entry(client).Collection(c => c.Subscribers).Query().CountAsync();

    public async Task<IQueryable<Subscriber>> GetSubscribers([Parent] Client client, UserDbContext dbContext) {
        var collection = dbContext.Entry(client).Collection(c => c.Subscribers);
        await collection.LoadAsync();
        return collection.Query();
    }
}

public class ClientType : ObjectType<Client> {
    protected override void Configure(IObjectTypeDescriptor<Client> descriptor) {
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<UserDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync(context.RequestAborted);
                var result = await dbContext.Clients.FindAsync([id], context.RequestAborted);
                return result;
            });
        descriptor.Ignore(f => f.IdNavigation);
        descriptor.Field("subscriberCount")
            .ResolveWith<ClientResolvers>(r => r.GetSubscriberCount(default!, default!));
        descriptor.Field(f => f.Subscribers).Ignore();
        descriptor.Field("subscribers")
            .ResolveWith<ClientResolvers>(r => r.GetSubscribers(default!, default!))
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
        //descriptor.Field("subscribeLink")
        //    .Type<NonNullType<StringType>>()
        //    .Resolve(context => {
        //        var client = context.Parent<Client>();
        //        var baseUrl = context.Service<IConfiguration>().GetSubscribeHost();
        //        return $"{baseUrl}/{client.Slug}";
        //    });
        descriptor.Field(f => f.SubscriptionId).ID(nameof(PlanSubscription))
            .Resolve(p =>
                string.IsNullOrEmpty(p.Parent<Client>().SubscriptionId)
                    ? null
                    : Guid.Parse(p.Parent<Client>().SubscriptionId!));
        descriptor.Field("subscription").Type<PlanSubscriptionType>()
            .Resolve(p =>
                string.IsNullOrEmpty(p.Parent<Client>().SubscriptionId)
                    ? null
                    : new PlanSubscription(Guid.Parse(p.Parent<Client>().SubscriptionId!)));
    }
}

public record PlanSubscription(Guid Id);

public class PlanSubscriptionType : ObjectType<PlanSubscription> {
    protected override void Configure(IObjectTypeDescriptor<PlanSubscription> descriptor) =>
        descriptor.Field(f => f.Id).ID();
}


