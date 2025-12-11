using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Stripe;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class ProductDeletedCommand(
    ILogger<ProductDeletedCommand> logger,
    KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> stripeWebhookProducer)
    : IEventCommand<StripeEventContext> {

    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }

        try {
            await stripeWebhookProducer.ProduceProductDeleted(
                new StripeProductDeletedEvent(product.Id),
                cancellationToken);
            logger.LogProductDeletedPublished(product.Id);
        } catch (Exception ex) {
            return ex;
        }
        return Maybe<Exception>.None;
    }
}

internal static partial class ProductDeletedCommandLogs {
    [LoggerMessage(
        EventId = 1020,
        Level = LogLevel.Information,
        Message = "Published StripeProductDeletedEvent for Product ID: {ProductId}")]
    public static partial void LogProductDeletedPublished(this ILogger logger, string productId);
}
