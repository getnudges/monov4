using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Stripe;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class CheckoutSessionCompletedCommand(
    ILogger<CheckoutSessionCompletedCommand> logger,
    IStripeClient stripeClient,
    KafkaMessageProducer<StripeWebhookKey, StripeWebhookEvent> stripeWebhookProducer)
    : IEventCommand<StripeEventContext> {

    private readonly InvoiceService _invoiceService = new(stripeClient);

    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not global::Stripe.Checkout.Session session) {
            return new MissingDataException("Could not find Session in event data");
        }

        var invoice = await _invoiceService.GetAsync(session.InvoiceId, cancellationToken: cancellationToken);

        try {
            await stripeWebhookProducer.ProduceCheckoutCompleted(
                new StripeCheckoutCompletedEvent(
                    SessionId: session.Id,
                    CustomerId: session.CustomerId,
                    InvoiceId: invoice.Id,
                    PriceLineItemId: invoice.Lines.First().Id,
                    MerchantServiceId: 1),
                cancellationToken);
            logger.LogCheckoutCompletedPublished(session.Id, session.CustomerId);
        } catch (Exception ex) {
            return ex;
        }
        return Maybe<Exception>.None;
    }
}

internal static partial class CheckoutSessionCompletedCommandLogs {
    [LoggerMessage(
        EventId = 1060,
        Level = LogLevel.Information,
        Message = "Published StripeCheckoutCompletedEvent for Session ID: {SessionId}, Customer ID: {CustomerId}")]
    public static partial void LogCheckoutCompletedPublished(this ILogger logger, string sessionId, string customerId);
}
