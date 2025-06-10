namespace UnAd.Kafka;

public partial record PlanKey(string EventType, string EventKey);

[EventModel(typeof(PlanKey))]
public partial record PlanEvent { }


public partial record PriceTierEventKey(string EventType, string EventKey);

[EventModel(typeof(PriceTierEventKey))]
public partial record PriceTierEvent;
