using System.Text.Json.Serialization;

namespace Nudges.Kafka.Events;

public partial record PlanSubscriptionKey(string EventType, Guid EventKey) {

    public static PlanSubscriptionKey PlanSubscriptionCreated(Guid planSubscriptionNodeId) =>
        new(nameof(PlanSubscriptionCreated), planSubscriptionNodeId);
}

[EventModel(typeof(PlanSubscriptionKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PlanSubscriptionCreatedEvent), "planSubscription.created")]
public abstract partial record PlanSubscriptionEvent;

public partial record PlanSubscriptionCreatedEvent(Guid PlanSubscriptionId) : PlanSubscriptionEvent;
