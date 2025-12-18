using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Stripe;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class ProductUpdatedCommand(
    ILogger<ProductUpdatedCommand> logger,
    KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> stripeWebhookProducer)
    : IEventCommand<StripeEventContext> {

    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }

        if (!product.Metadata.TryGetValue("planId", out var planIdStr) || string.IsNullOrEmpty(planIdStr)) {
            return new MissingDataException($"Could not find planId in product {product.Id} metadata");
        }

        try {
            await stripeWebhookProducer.ProduceProductUpdated(
                product.ToStripeProductUpdatedEvent(planIdStr),
                cancellationToken);
            logger.LogProductUpdatedPublished(product.Id, planIdStr);
        } catch (Exception ex) {
            return ex;
        }
        return Maybe<Exception>.None;
    }
}

public static class ProductUpdatedMappingExtensions {
    public static StripeProductUpdatedEvent ToStripeProductUpdatedEvent(this Product product, string planNodeId) =>
        new(
            ProductId: product.Id,
            PlanNodeId: planNodeId,
            Name: product.Name,
            Description: product.Description,
            IconUrl: product.Images?.FirstOrDefault(),
            Active: product.Active,
            ForeignServiceId: product.Id);
}

internal static partial class ProductUpdatedCommandLogs {
    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Information,
        Message = "Published StripeProductUpdatedEvent for Product ID: {ProductId}, Plan Node ID: {PlanNodeId}")]
    public static partial void LogProductUpdatedPublished(this ILogger logger, string productId, string planNodeId);
}
