using Confluent.Kafka;

namespace UnAd.Kafka;

public static class ProductEventProducerExtensions {
    public static Task<DeliveryResult<PlanKey, PlanEvent>> ProducePlanCreated(
        this KafkaMessageProducer<PlanKey, PlanEvent> producer, string nodeId, CancellationToken cancellationToken = default) =>
            producer.Produce(PlanKey.PlanCreated(nodeId), PlanEvent.Empty, cancellationToken);
    public static Task<DeliveryResult<PlanKey, PlanEvent>> ProducePlanUpdated(
        this KafkaMessageProducer<PlanKey, PlanEvent> producer, string nodeId, CancellationToken cancellationToken = default) =>
            producer.Produce(PlanKey.PlanUpdated(nodeId), PlanEvent.Empty, cancellationToken);
    public static Task<DeliveryResult<PlanKey, PlanEvent>> ProducePlanDeleted(
        this KafkaMessageProducer<PlanKey, PlanEvent> producer, string foreignServiceId, CancellationToken cancellationToken = default) =>
            producer.Produce(PlanKey.PlanDeleted(foreignServiceId), PlanEvent.Empty, cancellationToken);
}

public static class PriceTierEventProducerExtensions {
    public static Task<DeliveryResult<PriceTierEventKey, PriceTierEvent>> ProducePriceTierCreated(
        this KafkaMessageProducer<PriceTierEventKey, PriceTierEvent> producer, string nodeId, CancellationToken cancellationToken = default) =>
            producer.Produce(PriceTierEventKey.PriceTierCreated(nodeId), PriceTierEvent.Empty, cancellationToken);
    public static Task<DeliveryResult<PriceTierEventKey, PriceTierEvent>> ProducePriceTierUpdated(
        this KafkaMessageProducer<PriceTierEventKey, PriceTierEvent> producer, string nodeId, CancellationToken cancellationToken = default) =>
            producer.Produce(PriceTierEventKey.PriceTierUpdated(nodeId), PriceTierEvent.Empty, cancellationToken);
    public static Task<DeliveryResult<PriceTierEventKey, PriceTierEvent>> ProducePriceTierDeleted(
        this KafkaMessageProducer<PriceTierEventKey, PriceTierEvent> producer, string nodeId, CancellationToken cancellationToken = default) =>
            producer.Produce(PriceTierEventKey.PriceTierDeleted(nodeId), PriceTierEvent.Empty, cancellationToken);
}

public partial record PlanKey {
    public static PlanKey PlanCreated(string nodeId) =>
        new(nameof(PlanCreated), nodeId);
    public static PlanKey PlanUpdated(string nodeId) =>
        new(nameof(PlanUpdated), nodeId);
    public static PlanKey PlanDeleted(string foreignServiceId) =>
        new(nameof(PlanDeleted), foreignServiceId);

}

public partial record PlanEvent {
    public static PlanEvent Empty => new();
}

public partial record PriceTierEventKey {
    public static PriceTierEventKey PriceTierCreated(string nodeId) =>
        new(nameof(PriceTierCreated), nodeId);
    public static PriceTierEventKey PriceTierUpdated(string nodeId) =>
        new(nameof(PriceTierUpdated), nodeId);
    public static PriceTierEventKey PriceTierDeleted(string nodeId) =>
        new(nameof(PriceTierDeleted), nodeId);

}

public partial record PriceTierEvent {
    public static PriceTierEvent Empty { get; } = new();
}
