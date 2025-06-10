namespace UnAd.Kafka;

public partial record DiscountCodeKey(string EventType, string EventKey) {
    public override string ToString() => $"{EventType}:{EventKey}";
}

[EventModel(typeof(DiscountCodeKey))]
public partial record DiscountCodeEvent() { }
