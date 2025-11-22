using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public partial record PlanEventKey(int PlanId) {
    public override string ToString() => $"PlanEventKey(PlanId={PlanId})";
}

[EventModel(typeof(PlanEventKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PlanCreatedEvent), "plan.created")]
[JsonDerivedType(typeof(PlanUpdatedEvent), "plan.updated")]
[JsonDerivedType(typeof(PlanDeletedEvent), "plan.deleted")]
public abstract partial record PlanChangeEvent;

public partial record PlanCreatedEvent(Contracts.Products.Plan Plan) : PlanChangeEvent;
public partial record PlanUpdatedEvent(Contracts.Products.Plan Plan) : PlanChangeEvent;
public partial record PlanDeletedEvent(Contracts.Products.Plan Plan, DateTimeOffset DeletedAt) : PlanChangeEvent;

public partial record PlanEventKey {
    public static PlanEventKey PlanCreated(int planId) => new(planId);
    public static PlanEventKey PlanUpdated(int planId) => new(planId);
    public static PlanEventKey PlanDeleted(int planId) => new(planId);

}

public partial record PriceTierEventKey(int PriceTierId) {
    public override string ToString() => $"PriceTierEventKey(PriceTierId={PriceTierId})";
}

[EventModel(typeof(PriceTierEventKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PriceTierCreatedEvent), "pricetier.created")]
[JsonDerivedType(typeof(PriceTierUpdatedEvent), "pricetier.updated")]
[JsonDerivedType(typeof(PriceTierDeletedEvent), "pricetier.deleted")]
public abstract partial record PriceTierChangeEvent;

public partial record PriceTierCreatedEvent(Contracts.Products.PriceTier PriceTier) : PriceTierChangeEvent;
public partial record PriceTierUpdatedEvent(Contracts.Products.PriceTier PriceTier) : PriceTierChangeEvent;
public partial record PriceTierDeletedEvent(Contracts.Products.PriceTier PriceTier, DateTimeOffset DeletedAt) : PriceTierChangeEvent;

public static class PlanChangedEventProducerExtensions {
    public static Task<DeliveryResult<PlanEventKey, PlanChangeEvent>> ProducePlanCreated(
        this KafkaMessageProducer<PlanEventKey, PlanChangeEvent> producer, PlanCreatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PlanEventKey(data.Plan.Id), data, cancellationToken);
    public static Task<DeliveryResult<PlanEventKey, PlanChangeEvent>> ProducePlanUpdated(
        this KafkaMessageProducer<PlanEventKey, PlanChangeEvent> producer, PlanUpdatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PlanEventKey(data.Plan.Id), data, cancellationToken);
    public static Task<DeliveryResult<PlanEventKey, PlanChangeEvent>> ProducePlanDeleted(
        this KafkaMessageProducer<PlanEventKey, PlanChangeEvent> producer, PlanDeletedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PlanEventKey(data.Plan.Id), data, cancellationToken);
}

public static class PriceTierChangedEventProducerExtensions {
    public static Task<DeliveryResult<PriceTierEventKey, PriceTierChangeEvent>> ProducePriceTierCreated(
        this KafkaMessageProducer<PriceTierEventKey, PriceTierChangeEvent> producer, PriceTierCreatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PriceTierEventKey(data.PriceTier.Id), data, cancellationToken);
    public static Task<DeliveryResult<PriceTierEventKey, PriceTierChangeEvent>> ProducePriceTierUpdated(
        this KafkaMessageProducer<PriceTierEventKey, PriceTierChangeEvent> producer, PriceTierUpdatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PriceTierEventKey(data.PriceTier.Id), data, cancellationToken);
    public static Task<DeliveryResult<PriceTierEventKey, PriceTierChangeEvent>> ProducePriceTierDeleted(
        this KafkaMessageProducer<PriceTierEventKey, PriceTierChangeEvent> producer, PriceTierDeletedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PriceTierEventKey(data.PriceTier.Id), data, cancellationToken);
}
