using HotChocolate.Authorization;
using HotChocolate.Subscriptions;
using Nudges.Auth;
using Nudges.Data.Users.Models;
using UserApi.Models;

namespace UserApi;

public class Subscription {
    public static class Events {
        public const string ClientCreated = nameof(ClientCreated);
        public const string ClientUpdated = nameof(ClientUpdated);
        public const string SubscriberSubscribed = nameof(SubscriberSubscribed);
        public const string SubscriberUnsubscribed = nameof(SubscriberUnsubscribed);
    }

    [Subscribe(MessageType = typeof(Client))]
    [Authorize(PolicyNames.Client)]
    public Client? OnClientUpdated([ID<Client>] Guid id, [EventMessage] Client client) {
        if (client.Id != id) {
            return default!;
        }
        return client;
    }
}

/*
 * TODO: I need a subscription for Client updates so the PlanSubscription being created
 *       when a payment goes through can update in the success page without a refresh.
 */

public class SubscriptionObjectType : ObjectType<Subscription> {

    protected override void Configure(IObjectTypeDescriptor<Subscription> descriptor) {
        descriptor
            .Field(Subscription.Events.ClientCreated)
            .Type<ClientType>()
            .Resolve(context => context.GetEventMessage<Client>())
            .Subscribe(async context => {
                var receiver = context.Service<ITopicEventReceiver>();

                var stream =
                    await receiver.SubscribeAsync<Client>(Subscription.Events.ClientCreated);

                return stream;
            });
        descriptor
            .Field(Subscription.Events.SubscriberUnsubscribed)
            .Type<SubscriberType>()
            .Resolve(context => context.GetEventMessage<Subscriber>())
            .Subscribe(async context => {
                var receiver = context.Service<ITopicEventReceiver>();

                var stream =
                    await receiver.SubscribeAsync<Subscriber>(Subscription.Events.SubscriberUnsubscribed);

                return stream;
            });

        descriptor
            .Field(d => d.OnClientUpdated(default!, default!))
            .Type<ClientType>();


    }
}

