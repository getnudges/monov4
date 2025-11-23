using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Nudges.Kafka.Events;


public partial record ForeignProductEventKey(string EventType, string EventKey) {
    public override string ToString() => $"{nameof(ForeignProductEventKey)}:{EventType}:{EventKey}";
}

[EventModel(typeof(ForeignProductEventKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ForeignProductCreatedEvent), ForeignProductEventKey.CreatedEventType)]
[JsonDerivedType(typeof(ForeignProductSynchronizedEvent), ForeignProductEventKey.SynchronizedEventType)]
public abstract record ForeignProductEvent;

public partial record ForeignProductCreatedEvent(
    string ForeignProductId,
    string Name,
    string? Description,
    string? IconUrl) : ForeignProductEvent;

public partial record ForeignProductSynchronizedEvent(
    int PlanId,
    string ForeignProductId,
    string Name,
    string? Description,
    string? IconUrl) : ForeignProductEvent;

public static class ForeignProductEventProducerExtensions {
    public static Task<DeliveryResult<ForeignProductEventKey, ForeignProductEvent>> ProduceForeignProductCreated(
        this KafkaMessageProducer<ForeignProductEventKey, ForeignProductEvent> producer, ForeignProductCreatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(ForeignProductEventKey.ForeignProductCreated(data.ForeignProductId), data, cancellationToken);
    public static Task<DeliveryResult<ForeignProductEventKey, ForeignProductEvent>> ProduceForeignProductSynchronized(
        this KafkaMessageProducer<ForeignProductEventKey, ForeignProductEvent> producer, ForeignProductSynchronizedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(ForeignProductEventKey.ForeignProductSynchronized(data.ForeignProductId), data, cancellationToken);
}

public partial record ForeignProductEventKey {
    public const string CreatedEventType = "foreignProduct.created";
    public const string SynchronizedEventType = "foreignProduct.synchronized";
    public static ForeignProductEventKey ForeignProductCreated(string foreignProductId) =>
        new(CreatedEventType, foreignProductId);
    public static ForeignProductEventKey ForeignProductSynchronized(string foreignProductId) =>
        new(SynchronizedEventType, foreignProductId);
}
