using Confluent.Kafka;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class PaymentMessageMiddleware(ILogger<PaymentMessageMiddleware> logger,
                                        Func<INudgesClient> nudgesClientFactory,
                                        KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer) : IMessageMiddleware<PaymentKey, PaymentEvent> {

    public async Task<MessageContext<PaymentKey, PaymentEvent>> InvokeAsync(MessageContext<PaymentKey, PaymentEvent> context, MessageHandler<PaymentKey, PaymentEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully.");
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<PaymentKey, PaymentEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");

        switch (cr.Message.Value) {
            case PaymentCompletedEvent completed:
                await HandlePaymentComplete(completed, cancellationToken);
                break;
            default:
                throw new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.");
        }
    }

    private async Task HandlePaymentComplete(PaymentCompletedEvent paymentEvent, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        logger.LogAction($"Handling PaymentComplete for {paymentEvent}");

        var sub = await client.Instance.CreatePlanSubscription(new CreatePlanSubscriptionInput {
            PaymentConfirmationId = paymentEvent.PaymentConfirmationId.ToString(),
            ClientId = paymentEvent.ClientId.ToString(),
            PriceTierForeignServiceId = paymentEvent.PriceForeignServiceId,
        }, cancellationToken);

        // sub is a GraphQL response object representing created subscription, nothing to check here
    }
}
