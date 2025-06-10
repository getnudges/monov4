namespace UnAd.Kafka;

public partial record PlanSubscriptionKey(string EventType, Guid EventKey) {

    public static PlanSubscriptionKey PlanSubscriptionCreated(Guid planSubscriptionNodeId) =>
        new(nameof(PlanSubscriptionCreated), planSubscriptionNodeId);
}

[EventModel(typeof(PlanSubscriptionKey))]
public partial record PlanSubscriptionEvent;
