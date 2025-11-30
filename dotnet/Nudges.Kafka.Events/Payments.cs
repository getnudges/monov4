using System.Text.Json.Serialization;

namespace Nudges.Kafka.Events;

public partial record PaymentKey(string EventType, string EventKey) {

    public static PaymentKey PaymentCompleted(string paymentConfirmationNodeId) =>
        new(nameof(PaymentCompleted), paymentConfirmationNodeId);
}

[EventModel(typeof(PaymentKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PaymentCompletedEvent), "payment.completed")]
public abstract partial record PaymentEvent;

public partial record PaymentCompletedEvent(Guid ClientId, string PriceForeignServiceId, Guid PaymentConfirmationId) : PaymentEvent;
