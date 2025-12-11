using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Stripe;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class PriceCreatedCommand(
    ILogger<PriceCreatedCommand> logger,
    KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> stripeWebhookProducer)
    : IEventCommand<StripeEventContext> {

    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Price price) {
            return new MissingDataException("Could not find price in event data");
        }

        try {
            await stripeWebhookProducer.ProducePriceCreated(
                price.ToStripePriceCreatedEvent(),
                cancellationToken);
            logger.LogPriceCreatedPublished(price.Id, price.ProductId);
        } catch (Exception ex) {
            return ex;
        }
        return Maybe<Exception>.None;
    }
}

public static class PriceCreatedMappingExtensions {
    public static StripePriceCreatedEvent ToStripePriceCreatedEvent(this Price price) =>
        new(
            PriceId: price.Id,
            ProductId: price.ProductId,
            Nickname: price.Nickname,
            Price: price.UnitAmountDecimal.GetValueOrDefault() / 100m,
            Description: null,
            IconUrl: null);
}

internal static partial class PriceCreatedCommandLogs {
    [LoggerMessage(
        EventId = 1030,
        Level = LogLevel.Information,
        Message = "Published StripePriceCreatedEvent for Price ID: {PriceId}, Product ID: {ProductId}")]
    public static partial void LogPriceCreatedPublished(this ILogger logger, string priceId, string productId);
}
