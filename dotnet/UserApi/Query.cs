using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Nudges.Auth;
using Nudges.Data.Users;
using Nudges.Data.Users.Models;
using Nudges.Security;
using UserApi.Models;

namespace UserApi;

public class Query {
    [Authorize(PolicyNames.Admin)]
    public async Task<Client?> GetClient([ID<Client>] Guid id, UserDbContext context) => await context.Clients.FindAsync(id);

    [Authorize(PolicyNames.Admin)]
    public async Task<Client?> GetClientByPhoneNumber(string phoneNumber, UserDbContext context, HashService hashService, CancellationToken cancellationToken) {
        var normalized = ValidationHelpers.NormalizePhoneNumber(phoneNumber);
        var phoneHash = hashService.ComputeHash(normalized);
        var user = await context.Users.Include(u => u.Client).SingleOrDefaultAsync(c => c.PhoneNumberHash == phoneHash, cancellationToken);
        return user?.Client;
    }

    public async Task<Client?> GetClientBySlug(string slug, UserDbContext context, CancellationToken cancellationToken) =>
        await context.Clients.FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

    [Authorize(PolicyNames.Admin)]
    public async Task<Subscriber?> GetSubscriberByPhoneNumber(string phoneNumber, UserDbContext context, CancellationToken cancellationToken) {
        var user = await context.Users.Include(u => u.Subscriber).SingleOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);
        return user?.Subscriber;
    }

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
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Subscriber> GetSubscribers(UserDbContext context, HashService hashService, IResolverContext resolverContext) {
        if (resolverContext.GetUser() is { } user) {
            if (user?.FindFirst(WellKnownClaims.Role)?.Value is ClaimValues.Roles.Client && user?.FindFirst(WellKnownClaims.Username)?.Value is string phoneNumber) {
                var hashedPhone = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(phoneNumber));
                var list = context.Users
                    .Include(u => u.Client)
                    .ThenInclude(c => c!.Subscribers)
                    .Where(c => c.PhoneNumberHash == hashedPhone)
                    .SelectMany(c => c.Client!.Subscribers);
                return list;

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

    protected override void Configure(IObjectTypeDescriptor<Admin> descriptor) {
        descriptor.Ignore(f => f.IdNavigation);
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
}

public sealed class QueryObjectType : ObjectType<Query> {
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor) {

        descriptor.Field(f => f.GetClient(default!, default!))
            .Type<ClientType>();

        descriptor.Field(f => f.GetClientByCustomerId(default!, default!, default!))
            .Type<ClientType>();

        descriptor.Field(f => f.GetClients(default!, default!))
            .Type<ClientType>();

        descriptor.Field(f => f.GetSubscribers(default!, default!, default!))
            .Type<SubscriberType>();

        descriptor.Field("viewer")
            .Type<UserType>()
            .Resolve(async context => {
                var user = context.GetUser();
                var hashService = context.Services.GetRequiredService<HashService>();

                if (user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() is not { } roles) {
                    return null;
                }

                using var dbContext = await context.Services.GetRequiredService<IDbContextFactory<UserDbContext>>().CreateDbContextAsync();

                if (roles.Any(r => r == ClaimValues.Roles.Admin) && user?.FindFirst(ClaimTypes.NameIdentifier)?.Value is string adminSub) {
                    var clientUser = await dbContext.Users.Include(u => u.Admin).SingleOrDefaultAsync(s => s.Subject == adminSub);
                    return clientUser?.Admin;
                }

                if (roles.Any(r => r == ClaimValues.Roles.Client) && user?.FindFirst(ClaimTypes.NameIdentifier)?.Value is string clientSub) {
                    var clientUser = await dbContext.Users.Include(u => u.Client).SingleOrDefaultAsync(s => s.Subject == clientSub);
                    return clientUser?.Client;
                }

                if (roles.Any(r => r == ClaimValues.Roles.Subscriber) && user?.FindFirst(WellKnownClaims.PhoneNumber)?.Value is string subPhone) {
                    var hashedSubPhone = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(subPhone));
                    var subUser = await dbContext.Users.Include(u => u.Subscriber).SingleOrDefaultAsync(s => s.PhoneNumberHash == hashedSubPhone);
                    return subUser?.Subscriber;
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
