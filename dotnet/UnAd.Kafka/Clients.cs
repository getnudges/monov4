namespace UnAd.Kafka;

public partial record ClientKey(string EventType, string EventKey) {
    public static ClientKey ClientUpdated(string nodeId) =>
        new(nameof(ClientUpdated), nodeId);
    public static ClientKey ClientCreated(string nodeId) =>
        new(nameof(ClientCreated), nodeId);
}

[EventModel(typeof(ClientKey))]
public partial record ClientEvent;
