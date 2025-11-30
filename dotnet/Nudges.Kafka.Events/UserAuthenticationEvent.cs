using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public sealed record UserAuthenticationEventKey {
    public EventKey EventKey { get; set; } = EventKey.Empty;

    public UserAuthenticationEventKey() { }
    private UserAuthenticationEventKey(EventKey eventKey) => EventKey = eventKey;

    public static UserAuthenticationEventKey UserLoggedIn() =>
        new(new EventKey(nameof(UserLoggedIn)));

    public static UserAuthenticationEventKey UserLoggedOut() =>
        new(new EventKey(nameof(UserLoggedOut)));
}

[EventModel(typeof(UserAuthenticationEventKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(UserLoggedInEvent), "userAuth.loggedIn")]
[JsonDerivedType(typeof(UserLoggedOutEvent), "userAuth.loggedOut")]
public abstract record UserAuthenticationEvent {
    public required string PhoneNumber { get; init; }
    public required string Locale { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record UserLoggedInEvent : UserAuthenticationEvent {
    public static UserLoggedInEvent Create(string phoneNumber, string locale) =>
        new() {
            PhoneNumber = phoneNumber,
            Locale = locale,
            Timestamp = DateTimeOffset.UtcNow
        };
}

public record UserLoggedOutEvent : UserAuthenticationEvent {
    public static UserLoggedOutEvent Create(string phoneNumber, string locale) =>
        new() {
            PhoneNumber = phoneNumber,
            Locale = locale,
            Timestamp = DateTimeOffset.UtcNow
        };
}

public static class UserAuthenticationEventProducerExtensions {
    public static Task<DeliveryResult<UserAuthenticationEventKey, UserAuthenticationEvent>> ProduceUserLoggedIn(
        this KafkaMessageProducer<UserAuthenticationEventKey, UserAuthenticationEvent> producer,
        string phoneNumber,
        string locale,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        var key = UserAuthenticationEventKey.UserLoggedIn();
        var evt = UserLoggedInEvent.Create(phoneNumber, locale);

        return producer.Produce(key, evt, cancellationToken);
    }
}
