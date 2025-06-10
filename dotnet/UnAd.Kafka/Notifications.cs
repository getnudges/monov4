using System.Globalization;
using Confluent.Kafka;

namespace UnAd.Kafka;

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
public record NotificationEvent(string ResourceKey, string Locale, Dictionary<string, string> Replacements) {
    // TODO: this is where it falls apart. I need to be able to handle messages of different types
    // Hopefully without having a million different classes
    public static readonly NotificationEvent Empty = new(string.Empty, CultureInfo.CurrentCulture.Name, []);
    public static NotificationEvent ClientCreated() => new(nameof(ClientCreated), CultureInfo.CurrentCulture.Name, []);
    public static NotificationEvent ClientUpdated() => new(nameof(ClientUpdated), CultureInfo.CurrentCulture.Name, []);
    public static NotificationEvent SendSms(string resourceKey, string locale, Dictionary<string, string> replacements) => new(resourceKey, locale, replacements);
}

public static class NotificationProducerExtensions {
    public static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceSendOtp(
        this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string phoneNumber, string locale, string otp, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);
        ArgumentException.ThrowIfNullOrEmpty(locale);
        ArgumentException.ThrowIfNullOrEmpty(otp);

        return producer.Produce(NotificationKey.SendSms(phoneNumber), NotificationEvent.SendSms("SendOtp", locale, new Dictionary<string, string> { { "otp", otp } }), cancellationToken);
    }

    public static Task<DeliveryResult<NotificationKey, NotificationEvent>> ProduceSendSms(
        this KafkaMessageProducer<NotificationKey, NotificationEvent> producer, string phoneNumber, string resourceKey, string locale, Dictionary<string, string> replacements, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);
        ArgumentException.ThrowIfNullOrEmpty(locale);

        return producer.Produce(NotificationKey.SendSms(phoneNumber), NotificationEvent.SendSms(resourceKey, locale, replacements), cancellationToken);
    }
}

