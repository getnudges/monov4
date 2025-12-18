using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Stripe;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class PriceDeletedCommand(
    ILogger<PriceDeletedCommand> logger,
    KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> stripeWebhookProducer)
    : IEventCommand<StripeEventContext> {

    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Price price) {
            return new MissingDataException("Could not find price in event data");
        }

        try {
            await stripeWebhookProducer.ProducePriceDeleted(
                new StripePriceDeletedEvent(price.Id),
                cancellationToken);
            logger.LogPriceDeletedPublished(price.Id);
        } catch (Exception ex) {
            return ex;
        }
        return Maybe<Exception>.None;
    }
}

internal static partial class PriceDeletedCommandLogs {
    [LoggerMessage(
        EventId = 1050,
        Level = LogLevel.Information,
        Message = "Published StripePriceDeletedEvent for Price ID: {PriceId}")]
    public static partial void LogPriceDeletedPublished(this ILogger logger, string priceId);
}
