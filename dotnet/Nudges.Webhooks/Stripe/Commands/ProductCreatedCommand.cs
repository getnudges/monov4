using System.Globalization;
using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Stripe;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class ProductCreatedCommand(
    ILogger<ProductCreatedCommand> logger,
    KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> stripeWebhookProducer)
    : IEventCommand<StripeEventContext> {

    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }

        try {
            var data = product.ToStripeProductCreatedEvent();
            await stripeWebhookProducer.ProduceProductCreated(data, cancellationToken);
            logger.LogProductCreatedPublished(product.Id);
        } catch (Exception ex) {
            return ex;
        }
        return Maybe<Exception>.None;
    }
}

public static class ProductMappingExtensions {
    public static StripeProductCreatedEvent ToStripeProductCreatedEvent(this Product product) =>
        new(
            ProductId: product.Id,
            PlanId: int.Parse(product.Metadata.GetValueOrDefault("planId") ?? "0", CultureInfo.InvariantCulture),
            Name: product.Name,
            Description: product.Description,
            IconUrl: product.Images?.FirstOrDefault(),
            Active: product.Active,
            Metadata: product.Metadata.ToDictionary(k => k.Key, v => v.Value));
}

internal static partial class ProductCreatedCommandLogs {
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Published StripeProductCreatedEvent for Product ID: {ProductId}")]
    public static partial void LogProductCreatedPublished(this ILogger logger, string productId);
}
