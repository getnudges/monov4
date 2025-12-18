using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Stripe;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class PriceUpdatedCommand(
    ILogger<PriceUpdatedCommand> logger,
    KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> stripeWebhookProducer)
    : IEventCommand<StripeEventContext> {

    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Price price) {
            return new MissingDataException("Could not find price in event data");
        }

        try {
            await stripeWebhookProducer.ProducePriceUpdated(
                new StripePriceUpdatedEvent(
                    PriceId: price.Id,
                    Price: price.UnitAmountDecimal.GetValueOrDefault() / 100m),
                cancellationToken);
            logger.LogPriceUpdatedPublished(price.Id);
        } catch (Exception ex) {
            return ex;
        }
        return Maybe<Exception>.None;
    }
}

internal static partial class PriceUpdatedCommandLogs {
    [LoggerMessage(
        EventId = 1040,
        Level = LogLevel.Information,
        Message = "Published StripePriceUpdatedEvent for Price ID: {PriceId}")]
    public static partial void LogPriceUpdatedPublished(this ILogger logger, string priceId);
}
