namespace Nudges.Kafka;

public record EventKey {
    public string Type { get; }
    public Guid Id { get; }

    public EventKey(string type) : this(type, Guid.CreateVersion7()) { }

    private EventKey(string type, Guid id) {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        Type = type;
        Id = id;
    }

    public override string ToString() => $"{Type}:{Id}";

    public static EventKey Parse(string str) {
        ArgumentException.ThrowIfNullOrWhiteSpace(str);

        var parts = str.Split(':', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out var id)) {
            throw new FormatException($"Invalid EventKey format: '{str}'. Expected format: 'EventType:Guid'");
        }

        return new EventKey(parts[0], id);
    }

    public static bool TryParse(string str, out EventKey? key) {
        try {
            key = Parse(str);
            return true;
        } catch {
            key = null;
            return false;
        }
    }

    public static EventKey Empty => new("Empty", Guid.Empty);
}
