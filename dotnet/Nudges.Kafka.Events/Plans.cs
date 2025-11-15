namespace Nudges.Kafka.Events;

public partial record PlanKey(string EventType, string EventKey) {
    public override string ToString() => $"PlanKey:{EventType}:{EventKey}";
}

[EventModel(typeof(PlanKey))]
public partial record PlanEvent { }


public partial record PriceTierEventKey(string EventType, string EventKey);

[EventModel(typeof(PriceTierEventKey))]
public partial record PriceTierEvent;
