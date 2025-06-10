using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Nudges.Auth;
using Nudges.Data.Users;
using Nudges.Data.Users.Models;
using UserApi.Models;

namespace UserApi;

public class Query {
    [Authorize(PolicyNames.Admin)]
    public async Task<Client?> GetClient([ID<Client>] Guid id, UserDbContext context) => await context.Clients.FindAsync(id);

    [Authorize(PolicyNames.Admin)]
    public async Task<Client?> GetClientByPhoneNumber(string phoneNumber, UserDbContext context, CancellationToken cancellationToken) =>
        await context.Clients.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);

    public async Task<Client?> GetClientBySlug(string slug, UserDbContext context, CancellationToken cancellationToken) =>
        await context.Clients.FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

    [Authorize(PolicyNames.Admin)]
    public async Task<Subscriber?> GetSubscriberByPhoneNumber(string phoneNumber, UserDbContext context, CancellationToken cancellationToken) =>
        await context.Subscribers.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);

    [Authorize(PolicyNames.Admin)]
    public async Task<Client?> GetClientByCustomerId(string customerId, UserDbContext context, CancellationToken cancellationToken) =>
        await context.Clients.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

    [Authorize(PolicyNames.Admin)]
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Client> GetClients(UserDbContext context, IResolverContext resolverContext) {
        if (resolverContext.GetUser() is { } user && user?.FindFirst(WellKnownClaims.Role)?.Value is ClaimValues.Roles.Admin) {
            return context.Clients.AsSplitQuery();
        }
        return Enumerable.Empty<Client>().AsQueryable();
    }

    public Task<int> TotalClients(UserDbContext context, CancellationToken cancellationToken) => context.Clients.CountAsync(cancellationToken);

    [Authorize(Roles = [ClaimValues.Roles.Admin, ClaimValues.Roles.Client])]
    public async Task<Subscriber?> GetSubscriber(UserDbContext context, string subPhone, IResolverContext resolverContext, CancellationToken cancellationToken) {
        if (resolverContext.GetUser() is { } user) {
            if (user?.FindFirst(WellKnownClaims.Role)?.Value is ClaimValues.Roles.Client && user?.FindFirst(WellKnownClaims.Username)?.Value is string clientPhone) {
                // the user is a client and the subscriber is one of their subscribers
                return await context.Clients
                    .Where(c => c.PhoneNumber == clientPhone && c.SubscriberPhoneNumbers.Any(s => s.PhoneNumber == subPhone))
                    .SelectMany(c => c.SubscriberPhoneNumbers)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            if (user?.FindFirst(WellKnownClaims.Role)?.Value is ClaimValues.Roles.Subscriber && user?.FindFirst(WellKnownClaims.Username)?.Value is string phone) {
                // the user is a subscriber and the subscriber is their selves
                return await context.Subscribers.FirstOrDefaultAsync(s => s.PhoneNumber == phone, cancellationToken);
            }
            if (user?.FindFirst(WellKnownClaims.Role)?.Value is ClaimValues.Roles.Admin) {
                // the user is an admin
                return await context.Subscribers.FindAsync([subPhone], cancellationToken);
            }
        }
        return null;
    }

    [Authorize(Roles = [ClaimValues.Roles.Admin, ClaimValues.Roles.Client])]
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Subscriber> GetSubscribers(UserDbContext context, IResolverContext resolverContext) {
        if (resolverContext.GetUser() is { } user) {
            if (user?.FindFirst(WellKnownClaims.Role)?.Value is ClaimValues.Roles.Client && user?.FindFirst(WellKnownClaims.Username)?.Value is string phoneNumber) {
                return context.Clients
                    .Where(c => c.PhoneNumber == phoneNumber)
                    .SelectMany(c => c.SubscriberPhoneNumbers);
            }
            if (user?.FindFirst(WellKnownClaims.Role)?.Value is ClaimValues.Roles.Admin) {
                return context.Subscribers;
            }
        }
        return Enumerable.Empty<Subscriber>().AsQueryable();
    }

    public async Task<int> TotalSubscribers(UserDbContext context, CancellationToken cancellationToken) =>
        await context.Subscribers.CountAsync(cancellationToken);
}

public class AdminType : ObjectType<Admin> {

    protected override void Configure(IObjectTypeDescriptor<Admin> descriptor) =>
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) => {
                var factory = context.Service<IDbContextFactory<UserDbContext>>();
                await using var dbContext = await factory.CreateDbContextAsync();
                var result = await dbContext.Admins.FindAsync(id);
                return result;
            });
}

public sealed class QueryObjectType : ObjectType<Query> {
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor) {

        descriptor.Field(f => f.GetClient(default!, default!))
            .Type<ClientType>();

        descriptor.Field(f => f.GetClientByCustomerId(default!, default!, default!))
            .Type<ClientType>();

        descriptor.Field(f => f.GetClients(default!, default!))
            .Type<ClientType>();

        descriptor.Field(f => f.GetSubscriber(default!, default!, default!, default!))
            .Type<SubscriberType>();

        descriptor.Field(f => f.GetSubscribers(default!, default!))
            .Type<SubscriberType>();

        descriptor.Field("viewer")
            .Type<UserType>()
            .Resolve(async context => {
                var user = context.GetUser();
                using var dbContext = await context.Services.GetRequiredService<IDbContextFactory<UserDbContext>>().CreateDbContextAsync();
                var roles = user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                if ((roles?.Any(r => r == ClaimValues.Roles.Admin) == true || roles?.Any(r => r == ClaimValues.Roles.Service) == true) && user?.FindFirst(WellKnownClaims.Sub)?.Value is string adminSub) {
                    var client = await dbContext.Clients.FirstOrDefaultAsync(s => s.Subject == adminSub);
                    if (client is null) {
                        return null;
                    }
                    var admin = await dbContext.Admins.FirstOrDefaultAsync(a => a.Id == client.Id);
                    if (admin is null) {
                        return null;
                    }
                    return admin;
                }
                if (roles?.Any(r => r == ClaimValues.Roles.Client) == true && user?.FindFirst(ClaimTypes.NameIdentifier)?.Value is string clientSub) {
                    return await dbContext.Clients.FirstOrDefaultAsync(s => s.Subject == clientSub);
                }
                if (roles?.Any(r => r == ClaimValues.Roles.Subscriber) == true && user?.FindFirst(WellKnownClaims.PhoneNumber)?.Value is string subPhone) {
                    return await dbContext.Subscribers.FirstOrDefaultAsync(s => s.PhoneNumber == subPhone);
                }
                return null;
            });
    }
}

public class UserType : UnionType {
    protected override void Configure(IUnionTypeDescriptor descriptor) =>
        descriptor
            .Name("User")
            .Type<AdminType>()
            .Type<ClientType>()
            .Type<SubscriberType>();
}
