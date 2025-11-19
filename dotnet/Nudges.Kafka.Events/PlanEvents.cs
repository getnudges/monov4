using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public partial record PlanEventKey(int PlanId) {
    public override string ToString() => $"PlanEventKey(PlanId={PlanId})";
}

public partial record PriceTierCreatedData(int PriceTierId, decimal Price, TimeSpan Duration, string Name, string? Description, string? IconUrl);
public partial record PlanFeatureCreatedData(string? SupportTier, bool? AiSupport, int? MaxMessages);


[EventModel(typeof(PlanEventKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PlanCreatedEvent), "plan.created")]
[JsonDerivedType(typeof(PlanUpdatedEvent), "plan.updated")]
[JsonDerivedType(typeof(PlanDeletedEvent), "plan.deleted")]
public abstract partial record PlanChangeEvent;

public partial record PlanCreatedEvent(
    int PlanId,
    string Name,
    string? Description,
    string? IconUrl,
    PlanFeatureCreatedData? Features,
    List<PriceTierCreatedData> PriceTiers) : PlanChangeEvent;
public partial record PlanUpdatedEvent(
    int PlanId,
    string Name,
    string? Description,
    string? IconUrl,
    PlanFeatureCreatedData? Features,
    List<PriceTierCreatedData> PriceTiers) : PlanChangeEvent;
public partial record PlanDeletedEvent(int PlanId, DateTimeOffset DeletedAt) : PlanChangeEvent;

public static class PlanChangedEventProducerExtensions {
    public static Task<DeliveryResult<PlanEventKey, PlanChangeEvent>> ProducePlanCreated(
        this KafkaMessageProducer<PlanEventKey, PlanChangeEvent> producer, PlanCreatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PlanEventKey(data.PlanId), data, cancellationToken);
    public static Task<DeliveryResult<PlanEventKey, PlanChangeEvent>> ProducePlanUpdated(
        this KafkaMessageProducer<PlanEventKey, PlanChangeEvent> producer, PlanUpdatedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PlanEventKey(data.PlanId), data, cancellationToken);
    public static Task<DeliveryResult<PlanEventKey, PlanChangeEvent>> ProducePlanDeleted(
        this KafkaMessageProducer<PlanEventKey, PlanChangeEvent> producer, PlanDeletedEvent data, CancellationToken cancellationToken) =>
            producer.Produce(new PlanEventKey(data.PlanId), data, cancellationToken);
}

public partial record PlanEventKey {
    public static PlanEventKey PlanCreated(int planId) =>
        new(planId);
    public static PlanEventKey PlanUpdated(int planId) =>
        new(planId);
    public static PlanEventKey PlanDeleted(int planId) =>
        new(planId);

}
