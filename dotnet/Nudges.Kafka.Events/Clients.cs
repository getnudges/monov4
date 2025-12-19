using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public partial record ClientKey(Guid ClientId) {
    public override string ToString() => $"ClientKey(ClientId={ClientId})";
}

[EventModel(typeof(ClientKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ClientCreatedEvent), "client.created")]
[JsonDerivedType(typeof(ClientUpdatedEvent), "client.updated")]
public abstract partial record ClientEvent;

public partial record ClientCreatedEvent(Guid UserId, string PhoneNumberEncrypted, string Name, string Locale) : ClientEvent;
public partial record ClientUpdatedEvent(Guid UserId, string ClientNodeId) : ClientEvent;


public static class ClientEventProducerExtensions {
    public static Task<DeliveryResult<ClientKey, ClientEvent>> ProduceClientCreatedEvent(
        this KafkaMessageProducer<ClientKey, ClientEvent> producer, ClientCreatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new ClientKey(data.UserId), data, cancellationToken);

    public static Task<DeliveryResult<ClientKey, ClientEvent>> ProduceClientUpdatedEvent(
        this KafkaMessageProducer<ClientKey, ClientEvent> producer, ClientUpdatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new ClientKey(data.UserId), data, cancellationToken);
}
