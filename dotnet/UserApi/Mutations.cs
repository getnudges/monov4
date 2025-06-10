using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using UnAd.Auth;
using UnAd.Data.Users;
using UnAd.Data.Users.Models;
using UnAd.Kafka;

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

    public static string GenerateSlug(string phoneNumber) {
        // Convert phone number to bytes
        var phoneBytes = Encoding.UTF8.GetBytes(phoneNumber);

        // Generate SHA-256 hash
        var hashBytes = SHA256.HashData(phoneBytes);

        // Take the first 12 characters of the hashs
        var hashHex = Convert.ToHexString(hashBytes)[..12];

        // Encode the hash to base64
        var slugBytes = new byte[hashHex.Length / 2];
        for (var i = 0; i < slugBytes.Length; i++) {
            slugBytes[i] = Convert.ToByte(hashHex.Substring(i * 2, 2), 16);
        }
        var slug = Convert.ToBase64String(slugBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return slug;
    }

    [Authorize(Roles = [ClaimValues.Roles.Client])]
    [Error<ClientCreateException>]
    public async Task<Client> CreateClient(UserDbContext context,
                                           ClaimsPrincipal claimsPrincipal,
                                           CreateClientInput input,
                                           KafkaMessageProducer<ClientKey, ClientEvent> clientEventProducer,
                                           INodeIdSerializer idSerializer,
                                           HttpContext httpContext,
                                           CancellationToken cancellationToken) {

        var phoneNumber = claimsPrincipal.FindFirstValue(WellKnownClaims.PhoneNumber)!;
        var userSub = httpContext.User.FindFirstValue(WellKnownClaims.Sub);
        var userLocale = httpContext.User.FindFirstValue(WellKnownClaims.Locale);
        var existingClient = await context.Clients.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);
        if (existingClient is not null) {
            throw new ClientCreateException("Client already exists");
        }
        var requestLocale = httpContext.Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name;
        var newRecord = context.Add(new Client {
            PhoneNumber = phoneNumber,
            Name = input.Name,
            Locale = input.Locale ?? requestLocale ?? userLocale ?? CultureInfo.CurrentCulture.Name,
            Slug = GenerateSlug(phoneNumber),
            Subject = userSub,
        });

        try {
            await context.SaveChangesAsync(cancellationToken);
            var clientNodeId = idSerializer.Format(nameof(Client), newRecord.Entity.Id);
            await clientEventProducer.Produce(ClientKey.ClientCreated(clientNodeId), new ClientEvent(), cancellationToken);
            return newRecord.Entity;
        } catch (Exception ex) {
            throw new ClientCreateException(ex.GetBaseException().Message);
        }
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
        client.Locale = input.Locale ?? client.Locale;
        client.CustomerId = input.CustomerId ?? client.CustomerId;
        client.SubscriptionId = input.SubscriptionId.ToString() ?? client.SubscriptionId;
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

    private static async Task<Subscriber> GetOrCreateSubscriber(UserDbContext context, string phoneNumber, string locale, CancellationToken cancellationToken) {
        var subscriber = await context.Subscribers.FirstOrDefaultAsync(s => s.PhoneNumber == phoneNumber, cancellationToken);
        if (subscriber is null) {
            var newSub = context.Subscribers.Add(new Subscriber {
                PhoneNumber = phoneNumber,
                Locale = locale
            });
            await context.SaveChangesAsync(cancellationToken);
            return newSub.Entity;
        }
        return subscriber;
    }

    [Authorize(PolicyNames.Subscriber)]
    [Error<AlreadySubscribedException>]
    public async Task<Client> SubscribeToClient([ID<Client>] Guid clientId,
                                                UserDbContext context,
                                                HttpContext httpContext,
                                                KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                                                CancellationToken cancellationToken) {

        var phoneNumber = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.PhoneNumber)!;
        var userLocale = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Locale);
        var locale = userLocale
            ?? httpContext.Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name
            ?? CultureInfo.CurrentCulture.Name;
        var client = context.Clients.Find(clientId) ?? throw new ClientNotFoundException(clientId);
        if (client.PhoneNumber == phoneNumber) {
            throw new AlreadySubscribedException(phoneNumber); // TODO: should be custom
        }
        var subscriber = await GetOrCreateSubscriber(context, phoneNumber, locale, cancellationToken);

        if (subscriber.Clients.Contains(client)) {
            throw new AlreadySubscribedException(subscriber.PhoneNumber);
        }

        client.SubscriberPhoneNumbers.Add(subscriber);

        await context.SaveChangesAsync(cancellationToken);

        try {
            await notificationProducer.Produce(
                NotificationKey.SendSms(phoneNumber),
                NotificationEvent.SendSms("NewSubscriberWelcome", locale, new Dictionary<string, string> {
                    { "name", client.Name }
                }), cancellationToken);
            await notificationProducer.Produce(
                NotificationKey.SendSms(client.PhoneNumber),
                NotificationEvent.SendSms("ClientNewSubscriber", client.Locale, new Dictionary<string, string> {
                    { "count", client.SubscriberPhoneNumbers.Count.ToString(CultureInfo.GetCultureInfo(client.Locale)) }
                }), cancellationToken);
        } catch (Exception ex) {
            logger.LogException(ex);
        }

        return client;
    }

    [Authorize(PolicyNames.Subscriber)]
    [Error<NotSubscribedException>]
    [Error<ClientNotFoundException>]
    public async Task<Client> UnsubscribeFromClient([ID<Client>] Guid clientId, HttpContext httpContext, UserDbContext context, ITopicEventSender sender, CancellationToken cancellationToken) {
        var phoneNumber = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.PhoneNumber)!;
        var client = await context.Clients.FindAsync([clientId], cancellationToken)
            ?? throw new ClientNotFoundException(clientId);
        context.Entry(client).Collection(c => c.SubscriberPhoneNumbers).Load();
        var subscriber = client.SubscriberPhoneNumbers.FirstOrDefault(s => s.PhoneNumber == phoneNumber)
            ?? throw new NotSubscribedException(phoneNumber);

        client.SubscriberPhoneNumbers.Remove(subscriber);
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

    [Authorize(PolicyNames.Client)]
    [Error<SubscriberExistsException>]
    public async Task<Subscriber> AddSubscriber(UserDbContext context, AddSubscriberInput input, ITopicEventSender sender, CancellationToken cancellationToken) {
        var existing = await context.Subscribers.FindAsync([input.PhoneNumber], cancellationToken);
        if (existing is not null) {
            throw new SubscriberExistsException(input.PhoneNumber);
        }

        var newRecord = context.Subscribers.Add(new Subscriber {
            PhoneNumber = input.PhoneNumber,
            Locale = input.Locale
        });
        await context.SaveChangesAsync(cancellationToken);
        await sender.SendAsync(Subscription.Events.SubscriberSubscribed, newRecord.Entity, cancellationToken);
        return newRecord.Entity;
    }
}

public record CreateClientInput(string Name, string? Locale);
public record UpdateClientInput(Guid Id, string? Name, string? Locale, string? CustomerId, Guid? SubscriptionId);

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
