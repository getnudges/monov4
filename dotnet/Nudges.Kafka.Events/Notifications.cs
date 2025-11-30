using System.Globalization;
using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public record NotificationKey(string EventType, string EventKey) {
    public static NotificationKey EndSubscription(string planSubscriptionNodeId) =>
        new(nameof(EndSubscription), planSubscriptionNodeId);
    public static NotificationKey StartSubscription(string planSubscriptionNodeId) =>
        new(nameof(StartSubscription), planSubscriptionNodeId);
    public static NotificationKey ClientCreated(string clientNodeId) =>
        new(nameof(ClientCreated), clientNodeId);
    public static NotificationKey ClientUpdated(string nodeId) =>
        new(nameof(ClientUpdated), nodeId);
    public static NotificationKey SendSms(string phoneNumber) =>
        new(nameof(SendSms), phoneNumber);

    public override string ToString() => $"{EventType}:{EventKey}";
}

[EventModel(typeof(NotificationKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SendSmsNotificationEvent), "notification.sendSms")]
[JsonDerivedType(typeof(ClientCreatedNotificationEvent), "notification.clientCreated")]
[JsonDerivedType(typeof(ClientUpdatedNotificationEvent), "notification.clientUpdated")]
[JsonDerivedType(typeof(EndSubscriptionNotificationEvent), "notification.endSubscription")]
[JsonDerivedType(typeof(StartSubscriptionNotificationEvent), "notification.startSubscription")]
public abstract record NotificationEvent;

public record SendSmsNotificationEvent(string ResourceKey, string Locale, Dictionary<string, string> Replacements) : NotificationEvent {
    public static SendSmsNotificationEvent Create(string resourceKey, string locale, Dictionary<string, string> replacements) =>
        new(resourceKey, locale, replacements);
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
    public static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceOtpRequested(
        this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string phoneNumber, string locale, string otp, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);
        ArgumentException.ThrowIfNullOrEmpty(locale);
        ArgumentException.ThrowIfNullOrEmpty(otp);

        return producer.Produce(NotificationKey.SendSms(phoneNumber), SendSmsNotificationEvent.Create("SendOtp", locale, new Dictionary<string, string> { { "otp", otp } }), cancellationToken);
    }

    public static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceSendSms(
        this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string phoneNumber, string resourceKey, string locale, Dictionary<string, string> replacements, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);
        ArgumentException.ThrowIfNullOrEmpty(locale);

        return producer.Produce(NotificationKey.SendSms(phoneNumber), SendSmsNotificationEvent.Create(resourceKey, locale, replacements), cancellationToken);
    }
}

