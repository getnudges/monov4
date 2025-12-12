using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Nudges.Kafka.Events;

public partial record StripeWebhookKey(string EventType, string EntityId) {
    public override string ToString() => $"{EventType}:{EntityId}";

    public static StripeWebhookKey ProductCreated(string productId) => new("product.created", productId);
    public static StripeWebhookKey ProductUpdated(string productId) => new("product.updated", productId);
    public static StripeWebhookKey ProductDeleted(string productId) => new("product.deleted", productId);
    public static StripeWebhookKey PriceCreated(string priceId) => new("price.created", priceId);
    public static StripeWebhookKey PriceUpdated(string priceId) => new("price.updated", priceId);
    public static StripeWebhookKey PriceDeleted(string priceId) => new("price.deleted", priceId);
    public static StripeWebhookKey CheckoutCompleted(string sessionId) => new("checkout.completed", sessionId);
}

[EventModel(typeof(StripeWebhookKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(StripeProductCreatedEvent), "stripe.product.created")]
[JsonDerivedType(typeof(StripeProductUpdatedEvent), "stripe.product.updated")]
[JsonDerivedType(typeof(StripeProductDeletedEvent), "stripe.product.deleted")]
[JsonDerivedType(typeof(StripePriceCreatedEvent), "stripe.price.created")]
[JsonDerivedType(typeof(StripePriceUpdatedEvent), "stripe.price.updated")]
[JsonDerivedType(typeof(StripePriceDeletedEvent), "stripe.price.deleted")]
[JsonDerivedType(typeof(StripeCheckoutCompletedEvent), "stripe.checkout.completed")]
public abstract record StripeWebhookEvent;

/// <summary>
/// Fired when a Stripe product is created.
/// Consumer should check if a Plan with matching ForeignServiceId exists.
/// If yes, produce ForeignProductSynchronizedEvent.
/// If no, log and ignore (plans must be created in Nudges first).
/// </summary>
public record StripeProductCreatedEvent(
    string ProductId,
    string PlanNodeId,
    string Name,
    string? Description,
    string? IconUrl,
    bool Active,
    Dictionary<string, string> Metadata
) : StripeWebhookEvent;

/// <summary>
/// Fired when a Stripe product is updated.
/// Consumer should look up Plan by PlanId and update it.
/// </summary>
public record StripeProductUpdatedEvent(
    string ProductId,
    string PlanNodeId,
    string Name,
    string? Description,
    string? IconUrl,
    bool Active,
    string? ForeignServiceId
) : StripeWebhookEvent;

/// <summary>
/// Fired when a Stripe product is deleted.
/// Consumer should look up Plan by ForeignServiceId and delete it.
/// </summary>
public record StripeProductDeletedEvent(
    string ProductId
) : StripeWebhookEvent;

/// <summary>
/// Fired when a Stripe price is created.
/// Consumer should look up Plan by ProductId's ForeignServiceId,
/// then create/update a PriceTier with the given details.
/// </summary>
public record StripePriceCreatedEvent(
    string PriceId,
    string ProductId,
    string? Nickname,
    decimal Price,
    string? Description,
    string? IconUrl
) : StripeWebhookEvent;

/// <summary>
/// Fired when a Stripe price is updated.
/// Consumer should look up PriceTier by ForeignServiceId (= PriceId) and update price.
/// </summary>
public record StripePriceUpdatedEvent(
    string PriceId,
    decimal Price
) : StripeWebhookEvent;

/// <summary>
/// Fired when a Stripe price is deleted.
/// Consumer should look up PriceTier by ForeignServiceId and delete it.
/// </summary>
public record StripePriceDeletedEvent(
    string PriceId
) : StripeWebhookEvent;

/// <summary>
/// Fired when a Stripe checkout session completes successfully.
/// Consumer should:
/// 1. Look up Client by StripeCustomerId
/// 2. Create PaymentConfirmation with InvoiceId
/// 3. Create PlanSubscription linking Client to PriceTier
/// </summary>
public record StripeCheckoutCompletedEvent(
    string SessionId,
    string CustomerId,
    string InvoiceId,
    string PriceLineItemId,
    int MerchantServiceId
) : StripeWebhookEvent;

public static class StripeWebhookProducerExtensions {
    public static Task<DeliveryResult<StripeWebhookKey, StripeWebhookEvent>> ProduceProductCreated(
        this KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> producer,
        StripeProductCreatedEvent data,
        CancellationToken ct) =>
            producer.Produce(StripeWebhookKey.ProductCreated(data.ProductId), data, ct);

    public static Task<DeliveryResult<StripeWebhookKey, StripeWebhookEvent>> ProduceProductUpdated(
        this KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> producer,
        StripeProductUpdatedEvent data,
        CancellationToken ct) =>
            producer.Produce(StripeWebhookKey.ProductUpdated(data.ProductId), data, ct);

    public static Task<DeliveryResult<StripeWebhookKey, StripeWebhookEvent>> ProduceProductDeleted(
        this KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> producer,
        StripeProductDeletedEvent data,
        CancellationToken ct) =>
            producer.Produce(StripeWebhookKey.ProductDeleted(data.ProductId), data, ct);

    public static Task<DeliveryResult<StripeWebhookKey, StripeWebhookEvent>> ProducePriceCreated(
        this KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> producer,
        StripePriceCreatedEvent data,
        CancellationToken ct) =>
            producer.Produce(StripeWebhookKey.PriceCreated(data.PriceId), data, ct);

    public static Task<DeliveryResult<StripeWebhookKey, StripeWebhookEvent>> ProducePriceUpdated(
        this KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> producer,
        StripePriceUpdatedEvent data,
        CancellationToken ct) =>
            producer.Produce(StripeWebhookKey.PriceUpdated(data.PriceId), data, ct);

    public static Task<DeliveryResult<StripeWebhookKey, StripeWebhookEvent>> ProducePriceDeleted(
        this KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> producer,
        StripePriceDeletedEvent data,
        CancellationToken ct) =>
            producer.Produce(StripeWebhookKey.PriceDeleted(data.PriceId), data, ct);

    public static Task<DeliveryResult<StripeWebhookKey, StripeWebhookEvent>> ProduceCheckoutCompleted(
        this KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> producer,
        StripeCheckoutCompletedEvent data,
        CancellationToken ct) =>
            producer.Produce(StripeWebhookKey.CheckoutCompleted(data.SessionId), data, ct);
}
