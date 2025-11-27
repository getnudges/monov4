using System.Buffers.Text;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Nudges.Auth;
using Nudges.Data.Users;
using Nudges.Data.Users.Models;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Security;
using StackExchange.Redis;

namespace UserApi;

public class Mutation(ILogger<Mutation> logger) {

    [Authorize(PolicyNames.Admin)]
    public async Task<Client> DeleteClient([ID<Client>] Guid id, UserDbContext context, CancellationToken cancellationToken) {
        var client = await context.Clients.FindAsync([id], cancellationToken)
            ?? throw new ClientNotFoundException(id);
        context.Clients.Remove(client);
        context.SaveChanges();
        return client;
    }

    public static string GenerateSlug(string input) {
        // Convert phone number to bytes
        var bytes = Encoding.UTF8.GetBytes(input);

        // Generate SHA-256 hash
        var hashBytes = SHA256.HashData(bytes);

        // we want them to be short
        var slug = Base64Url.EncodeToString(hashBytes)[..12];

        return slug;
    }

    [Authorize(Roles = [ClaimValues.Roles.Client])]
    [Error<ClientCreateException>]
    public async Task<Client> CreateClient(UserDbContext context,
                                           ClaimsPrincipal claimsPrincipal,
                                           CreateClientInput input,
                                           KafkaMessageProducer<ClientKey, ClientEvent> clientEventProducer,
                                           HashService hashService,
                                           HttpContext httpContext,
                                           CancellationToken cancellationToken) {

        var phoneNumber = claimsPrincipal.FindFirstValue(WellKnownClaims.PhoneNumber)
                          ?? throw new ClientCreateException("Phone number claim is missing from token.");

        var userSub = claimsPrincipal.FindFirstValue(WellKnownClaims.Sub)
                     ?? throw new ClientCreateException("Missing 'sub' claim.");

        var userLocale = claimsPrincipal.FindFirstValue(WellKnownClaims.Locale)
            ?? input.Locale
            ?? httpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name
            ?? "en-US";

        var phoneNumberHash = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(phoneNumber));
        var user = await context.Users
            .Include(u => u.Client)
            .SingleOrDefaultAsync(s => s.PhoneNumberHash == phoneNumberHash, cancellationToken);
        if (user?.Client is not null) {
            throw new ClientCreateException("Client already exists.");
        }

        var newUser = context.Users.Add(new User {
            PhoneNumber = phoneNumber,
            Locale = userLocale,
            Subject = userSub,
        });
        await context.SaveChangesAsync(cancellationToken);
        var newClient = context.Clients.Add(new Client {
            Id = newUser.Entity.Id,
            Name = input.Name,
            Slug = GenerateSlug(newUser.Entity.Id.ToString())
        });
        await context.SaveChangesAsync(cancellationToken);

        return newClient.Entity;
    }


    [Authorize(Roles = [ClaimValues.Roles.Admin, ClaimValues.Roles.Client])]
    [Error<ClientCreateException>]
    public async Task<Client> UpdateClient(UserDbContext context,
                                           KafkaMessageProducer<ClientKey, ClientEvent> clientEventProducer,
                                           INodeIdSerializer idSerializer,
                                           UpdateClientInput input,
                                           ITopicEventSender subscriptionSender,
                                           CancellationToken cancellationToken) {

        var client = await context.Clients.FindAsync([input.Id], cancellationToken) ?? throw new ClientNotFoundException(input.Id);
        client.Name = input.Name ?? client.Name;
        client.CustomerId = input.CustomerId ?? client.CustomerId;
        client.SubscriptionId = input.SubscriptionId ?? client.SubscriptionId;
        context.Attach(client).State = EntityState.Modified;

        try {
            await context.SaveChangesAsync(cancellationToken);
            var clientNodeId = idSerializer.Format(nameof(Client), client.Id);
            await clientEventProducer.Produce(ClientKey.ClientUpdated(clientNodeId), new ClientEvent(), cancellationToken);
            await subscriptionSender.SendAsync(nameof(Subscription.OnClientUpdated), client, cancellationToken);
            return client;
        } catch (Exception ex) {
            throw new ClientUpdateException(input.Id, ex.GetBaseException().Message);
        }

    }
    private static async Task<Subscriber> GetOrCreateSubscriber(string phoneNumber, string locale, string subject, UserDbContext context, HashService hashService, CancellationToken cancellationToken) {
        var phoneNumberHash = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(phoneNumber));
        var user = await context.Users
            .Include(u => u.Subscriber)
            .SingleOrDefaultAsync(s => s.PhoneNumberHash == phoneNumberHash, cancellationToken);
        if (user?.Client is not null) {
            throw new ClientCreateException("Client already exists.");
        }

        var newUser = context.Users.Add(new User {
            PhoneNumber = phoneNumber,
            Locale = locale,
            Subject = subject,
        });
        await context.SaveChangesAsync(cancellationToken);
        var newSub = context.Subscribers.Add(new Subscriber {
            Id = newUser.Entity.Id,
        });
        await context.SaveChangesAsync(cancellationToken);

        return newSub.Entity;
    }

    [Authorize(PolicyNames.Subscriber)]
    [Error<AlreadySubscribedException>]
    public async Task<Client> SubscribeToClient([ID<Client>] Guid clientId,
                                                UserDbContext dbContext,
                                                HashService hashService,
                                                HttpContext httpContext,
                                                KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                                                CancellationToken cancellationToken) {

        var phoneNumber = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.PhoneNumber)
            ?? throw new Exception("Phone number claim is missing");

        var phoneNumberHash = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(phoneNumber));
        var userLocale = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Locale);
        var locale = userLocale
            ?? httpContext.Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name
            ?? CultureInfo.CurrentCulture.Name;

        var sub = httpContext.User.FindFirstValue(WellKnownClaims.Sub)
                     ?? throw new ClientCreateException("Missing 'sub' claim.");
        var user = await dbContext.Users.Include(u => u.Client).SingleOrDefaultAsync(u => u.Id == clientId, cancellationToken)
            ?? throw new ClientNotFoundException(clientId);
        if (user?.PhoneNumberHash != phoneNumberHash || user?.Client is not { } client) {
            throw new AlreadySubscribedException(phoneNumber);
        }
        var subscriber = await GetOrCreateSubscriber(phoneNumber, locale, sub, dbContext, hashService, cancellationToken);

        if (subscriber.Clients.Any(c => c.IdNavigation.PhoneNumberHash == phoneNumberHash)) {
            throw new AlreadySubscribedException(subscriber.IdNavigation.PhoneNumberHash);
        }

        client.Subscribers.Add(subscriber);

        await dbContext.SaveChangesAsync(cancellationToken);

        //try {
        //    await notificationProducer.Produce(
        //        NotificationKey.SendSms(phoneNumber),
        //        NotificationEvent.SendSms("NewSubscriberWelcome", locale, new Dictionary<string, string> {
        //            { "name", user.Name }
        //        }), cancellationToken);
        //    await notificationProducer.Produce(
        //        NotificationKey.SendSms(user.PhoneNumber),
        //        NotificationEvent.SendSms("ClientNewSubscriber", user.Locale, new Dictionary<string, string> {
        //            { "count", user.Subscribers.Count.ToString(CultureInfo.GetCultureInfo(user.Locale)) }
        //        }), cancellationToken);
        //} catch (Exception ex) {
        //    logger.LogException(ex);
        //}

        return client;
    }

    [Authorize(PolicyNames.Subscriber)]
    [Error<NotSubscribedException>]
    [Error<ClientNotFoundException>]
    public async Task<Client> UnsubscribeFromClient([ID<Client>] Guid clientId, HttpContext httpContext, UserDbContext context, HashService hashService, ITopicEventSender sender, CancellationToken cancellationToken) {
        var phoneNumber = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.PhoneNumber)!;
        var phoneNumberHash = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(phoneNumber));
        var client = await context.Clients.FindAsync([clientId], cancellationToken)
            ?? throw new ClientNotFoundException(clientId);

        context.Entry(client).Collection(c => c.Subscribers).Load();
        var subscriber = client.Subscribers.FirstOrDefault(s => s.IdNavigation.PhoneNumberHash == phoneNumberHash)
            ?? throw new NotSubscribedException(phoneNumber);

        client.Subscribers.Remove(subscriber);
        await context.SaveChangesAsync(cancellationToken);

        await sender.SendAsync(Subscription.Events.SubscriberUnsubscribed, subscriber, cancellationToken);
        return client;
    }

    [Authorize(PolicyNames.Admin)]
    [Error<SubscriberNotFoundException>]
    public async Task<Subscriber> DeleteSubscriber([ID<Subscriber>] string id, UserDbContext context, CancellationToken cancellationToken) {
        var subscriber = await context.Subscribers.FindAsync([id], cancellationToken)
            ?? throw new SubscriberNotFoundException(id);
        context.Subscribers.Remove(subscriber);
        await context.SaveChangesAsync(cancellationToken);
        return subscriber;
    }
}

public record CreateClientInput(string Name, string? Locale);
public record UpdateClientInput(Guid Id, string? Name, string? CustomerId, string? SubscriptionId);

public class UpdateClientInputType : InputObjectType<UpdateClientInput> {
    protected override void Configure(IInputObjectTypeDescriptor<UpdateClientInput> descriptor) =>
        descriptor.Field(f => f.Id).ID(nameof(Client));
}
public record SendMessageInput(Audience Audience, string Message);
public record AddSubscriberInput(string PhoneNumber, string Locale);
public record SendMessagePayload(int Sent, int Failed);

[Flags]
public enum Audience {
    AllClients = 1 << 0,
    ActiveClients = 1 << 1,
    AllSubscribers = 1 << 2,
    ActiveClientsWithoutSubscribers = 1 << 3,
    Everyone = AllClients | AllSubscribers
}

public class ClientNotFoundException(Guid clientId) : Exception($"Client with ID {clientId} not found");

public class ClientUpdateException(Guid clientId, string message) : Exception($"Client with ID {clientId} failed to update: {message}");

public class SubscriberNotFoundException(string phoneNumber) : Exception($"Subscriber with phone number {phoneNumber} not found");

public class SubscriberExistsException(string phoneNumber) : Exception($"Subscriber with phone number {phoneNumber} already exists");

public class AlreadySubscribedException(string phoneNumber) : Exception($"Subscriber with phone number {phoneNumber} is already subscribed to client");

public class NotSubscribedException(string phoneNumber) : Exception($"Subscriber with phone number {phoneNumber} is not subscribed to client");

public class ClientCreateException(string errorMessage) : Exception($"Client Creation failed with error: {errorMessage}");

public class SendMessageException(string message) : Exception(message);

public class MutationObjectType : ObjectType<Mutation> { }
