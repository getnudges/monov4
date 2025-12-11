using Confluent.Kafka;
using Nudges.Kafka.Middleware;

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
    // Exception details (structured)
    public required string ExceptionType { get; init; }
    public required string ExceptionMessage { get; init; }
    public string? StackTrace { get; init; }
    public string? InnerExceptionType { get; init; }
    public string? InnerExceptionMessage { get; init; }
    public string? InnerExceptionStackTrace { get; init; }

    // Original message context
    public required string Topic { get; init; }
    public required EventKey EventKey { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    // Original message payload (for replay)
    public string? OriginalKeyJson { get; init; }
    public string? OriginalValueJson { get; init; }

    // Headers (including trace context)
    public Dictionary<string, string>? OriginalHeaders { get; init; }

    // Additional metadata
    public int AttemptCount { get; init; }
    public FailureType FailureType { get; init; }

    public static (DeadLetterEventKey, DeadLetterEvent) MessageFailed(string topic, EventKey failedKey, Exception exception) {
        var key = DeadLetterEventKey.MessageFailed(failedKey);
        var evt = new DeadLetterEvent {
            EventKey = key.EventKey,
            Topic = topic,
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            ExceptionMessage = exception.Message,
            StackTrace = exception.StackTrace,
            InnerExceptionType = exception.InnerException?.GetType().FullName,
            InnerExceptionMessage = exception.InnerException?.Message,
            InnerExceptionStackTrace = exception.InnerException?.StackTrace,
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
