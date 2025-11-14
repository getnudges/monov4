using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public sealed record DeadLetterEventKey {
    public EventKey EventKey { get; set; } = EventKey.Empty;
    public EventKey FailedKey { get; set; } = EventKey.Empty;

    public DeadLetterEventKey() { }

    private DeadLetterEventKey(EventKey eventKey, EventKey failedKey) {
        EventKey = eventKey;
        FailedKey = failedKey;
    }

    public override string ToString() => EventKey.ToString();

    public static DeadLetterEventKey MessageFailed(EventKey failedKey) =>
        new(new EventKey(nameof(MessageFailed)), failedKey);
}

[EventModel(typeof(DeadLetterEventKey))]
public sealed class DeadLetterEvent {
    public required Exception Exception { get; init; }
    public required string Topic { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required EventKey EventKey { get; init; }

    public static (DeadLetterEventKey, DeadLetterEvent) MessageFailed(string topic, EventKey failedKey, Exception exception) {
        var key = DeadLetterEventKey.MessageFailed(failedKey);
        var evt = new DeadLetterEvent {
            EventKey = key.EventKey,
            Topic = topic,
            Exception = exception
        };
        return (key, evt);
    }
}

public static class DeadLetterEventProducerExtensions {
    public static Task<DeliveryResult<DeadLetterEventKey, DeadLetterEvent>> ProduceMessageFailed(
        this KafkaMessageProducer<DeadLetterEventKey, DeadLetterEvent> producer,
        string topic,
        EventKey failedKey,
        Exception exception,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(failedKey);

        var (key, evt) = DeadLetterEvent.MessageFailed(topic, failedKey, exception);

        return producer.Produce(key, evt, cancellationToken);
    }
}
