using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public sealed record UserAuthenticationEventKey {
    public EventKey EventKey { get; }

    private UserAuthenticationEventKey(EventKey eventKey) => EventKey = eventKey;

    public static UserAuthenticationEventKey UserLoggedIn() =>
        new(new EventKey(nameof(UserLoggedIn)));

    public static UserAuthenticationEventKey UserLoggedOut() =>
        new(new EventKey(nameof(UserLoggedOut)));
}

[EventModel(typeof(UserAuthenticationEventKey))]
public sealed class UserAuthenticationEvent {
    public required string PhoneNumber { get; init; }
    public required string Locale { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required EventKey EventKey { get; init; }

    public static (UserAuthenticationEventKey, UserAuthenticationEvent) UserLoggedIn(string phoneNumber, string locale) {
        var key = UserAuthenticationEventKey.UserLoggedIn();
        var evt = new UserAuthenticationEvent {
            EventKey = key.EventKey,
            PhoneNumber = phoneNumber,
            Locale = locale
        };
        return (key, evt);
    }
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

        var (key, evt) = UserAuthenticationEvent.UserLoggedIn(phoneNumber, locale);

        return producer.Produce(key, evt, cancellationToken);
    }
}
