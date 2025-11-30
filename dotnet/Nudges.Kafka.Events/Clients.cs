using System.Text.Json.Serialization;

namespace Nudges.Kafka.Events;

public partial record ClientKey(string EventType, string EventKey) {
    public static ClientKey ClientUpdated(string nodeId) =>
        new(nameof(ClientUpdated), nodeId);
    public static ClientKey ClientCreated(string nodeId) =>
        new(nameof(ClientCreated), nodeId);
}

[EventModel(typeof(ClientKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ClientCreatedEvent), "client.created")]
[JsonDerivedType(typeof(ClientUpdatedEvent), "client.updated")]
public abstract partial record ClientEvent;

public partial record ClientCreatedEvent(string ClientNodeId) : ClientEvent;
public partial record ClientUpdatedEvent(string ClientNodeId) : ClientEvent;
