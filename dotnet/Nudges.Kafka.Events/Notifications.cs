using System.Globalization;
using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public record NotificationKey(string EventType) {
    public override string ToString() => $"NotificationKey(EventType={EventType})";
}

[EventModel(typeof(NotificationKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SendSmsNotificationEvent), "notification.sendSms")]
[JsonDerivedType(typeof(ClientCreatedNotificationEvent), "notification.clientCreated")]
[JsonDerivedType(typeof(ClientUpdatedNotificationEvent), "notification.clientUpdated")]
[JsonDerivedType(typeof(EndSubscriptionNotificationEvent), "notification.endSubscription")]
[JsonDerivedType(typeof(StartSubscriptionNotificationEvent), "notification.startSubscription")]
public abstract record NotificationEvent;

public record SendSmsNotificationEvent(string PhoneNumber, string ResourceKey, string Locale, Dictionary<string, string> Replacements) : NotificationEvent {
    public static SendSmsNotificationEvent Create(string phoneNumber, string resourceKey, string locale, Dictionary<string, string> replacements) =>
        new(phoneNumber, resourceKey, locale, replacements);
}

public record ClientCreatedNotificationEvent(string ResourceKey, string Locale, Dictionary<string, string> Replacements) : NotificationEvent {
    public static ClientCreatedNotificationEvent Create() =>
        new(nameof(ClientCreatedNotificationEvent), CultureInfo.CurrentCulture.Name, []);
}

public record ClientUpdatedNotificationEvent(string ResourceKey, string Locale, Dictionary<string, string> Replacements) : NotificationEvent {
    public static ClientUpdatedNotificationEvent Create() =>
        new(nameof(ClientUpdatedNotificationEvent), CultureInfo.CurrentCulture.Name, []);
}

public record EndSubscriptionNotificationEvent(string ResourceKey, string Locale, Dictionary<string, string> Replacements) : NotificationEvent;

public record StartSubscriptionNotificationEvent(string ResourceKey, string Locale, Dictionary<string, string> Replacements) : NotificationEvent;

public static class NotificationProducerExtensions {

    private static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceSendSms(
        this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string encryptedPhone, string resourceKey, string locale, Dictionary<string, string> replacements, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrEmpty(encryptedPhone);
        ArgumentException.ThrowIfNullOrEmpty(locale);

        return producer.Produce(new NotificationKey("SendSms"), SendSmsNotificationEvent.Create(encryptedPhone, resourceKey, locale, replacements), cancellationToken);
    }

    public static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceSendOtp(
        this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string encryptedPhone, string locale, string code, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrEmpty(encryptedPhone);
        ArgumentException.ThrowIfNullOrEmpty(locale);

        return ProduceSendSms(producer, encryptedPhone, "SendOtp", locale, new Dictionary<string, string>(
            [new KeyValuePair<string, string>("otp", code)]
        ), cancellationToken);
    }

    public static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceSendUserLoggedIn(
        this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string encryptedPhone, string locale, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrEmpty(encryptedPhone);
        ArgumentException.ThrowIfNullOrEmpty(locale);

        return ProduceSendSms(producer, encryptedPhone, "UserLoggedIn", locale, [], cancellationToken);
    }

    public static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceSendClientCreated(
        this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string encryptedPhone, string locale, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrEmpty(encryptedPhone);
        ArgumentException.ThrowIfNullOrEmpty(locale);

        return ProduceSendSms(producer, encryptedPhone, "ClientCreated", locale, [], cancellationToken);
    }


    //public static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceStartSubscription(
    //    this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string phoneNumber, string resourceKey, string locale, Dictionary<string, string> replacements, CancellationToken cancellationToken) {
    //    ArgumentNullException.ThrowIfNull(producer);
    //    return producer.Produce(new NotificationKey("StartSubscription"),
    //        new StartSubscriptionNotificationEvent("StartSubscription", locale, []), cancellationToken);
    //}
}

