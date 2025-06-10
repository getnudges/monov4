namespace UnAd.Kafka;

public partial record PaymentKey(string EventType, string EventKey) {

    public static PaymentKey PaymentCompleted(string paymentConfirmationNodeId) =>
        new(nameof(PaymentCompleted), paymentConfirmationNodeId);
}

[EventModel(typeof(PaymentKey))]
public partial record PaymentEvent(Guid ClientId, string PriceForeignServiceId, Guid PaymentConfirmationId);
