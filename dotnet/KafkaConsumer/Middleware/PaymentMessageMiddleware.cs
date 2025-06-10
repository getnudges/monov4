using Confluent.Kafka;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using UnAd.Kafka;
using UnAd.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class PaymentMessageMiddleware(ILogger<PaymentMessageMiddleware> logger,
                                        Func<IUnAdClient> unAdClientFactory,
                                        KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer) : IMessageMiddleware<PaymentKey, PaymentEvent> {

    public async Task<MessageContext<PaymentKey, PaymentEvent>> InvokeAsync(MessageContext<PaymentKey, PaymentEvent> context, MessageHandler<PaymentKey, PaymentEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err)); // TODO: handle errors better (maybe retry?)
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<PaymentKey, PaymentEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        return await (cr switch {
            { Message.Key.EventType: nameof(PaymentKey.PaymentCompleted), Message.Value: var paymentEvent } =>
                HandlePaymentComplete(paymentEvent, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandlePaymentComplete(PaymentEvent paymentEvent, CancellationToken cancellationToken) {
        /*
         * NOTE: this is only fired and used in local debugging.
         * The CreatePlanSubscription mutation is called by the Stripe webhook handler in production.
         */
        using var client = new DisposableWrapper<IUnAdClient>(unAdClientFactory);
        logger.LogAction($"Handling PaymentComplete for {paymentEvent}");

        return await client.Instance.GetPriceTierByForeignId(paymentEvent.PriceForeignServiceId, cancellationToken).Match(async tier =>
            await client.Instance.CreatePlanSubscription(new CreatePlanSubscriptionInput {
                PaymentConfirmationId = paymentEvent.PaymentConfirmationId.ToString(),
                ClientId = paymentEvent.ClientId.ToString(),
                PriceTierForeignServiceId = paymentEvent.PriceForeignServiceId,
            }, cancellationToken).Map<ICreatePlanSubscription_CreatePlanSubscription, bool, Exception>(e => true),
            () => new MissingDataException("Could not find PriceTier"));
    }
}
