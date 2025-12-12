using Confluent.Kafka;
using KafkaConsumer.Services;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class StripeWebhookMessageMiddleware(
    ILogger<StripeWebhookMessageMiddleware> logger,
    Func<INudgesClient> nudgesClientFactory)
    : IMessageMiddleware<StripeWebhookKey, StripeWebhookEvent> {

    public async Task<MessageContext<StripeWebhookKey, StripeWebhookEvent>> InvokeAsync(
        MessageContext<StripeWebhookKey, StripeWebhookEvent> context,
        MessageHandler<StripeWebhookKey, StripeWebhookEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogMessageHandled(context.ConsumeResult.Message.Key);
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<StripeWebhookKey, StripeWebhookEvent> cr, CancellationToken cancellationToken) {
        logger.LogMessageReceived(cr.Message.Key);
        switch (cr.Message.Value) {
            case StripeProductCreatedEvent created:
                await HandleProductCreated(created, cancellationToken);
                break;
            case StripeProductUpdatedEvent updated:
                await HandleProductUpdated(updated, cancellationToken);
                break;
            case StripeProductDeletedEvent deleted:
                await HandleProductDeleted(deleted, cancellationToken);
                break;
            case StripePriceCreatedEvent priceCreated:
                await HandlePriceCreated(priceCreated, cancellationToken);
                break;
            case StripePriceUpdatedEvent priceUpdated:
                await HandlePriceUpdated(priceUpdated, cancellationToken);
                break;
            case StripePriceDeletedEvent priceDeleted:
                await HandlePriceDeleted(priceDeleted, cancellationToken);
                break;
            case StripeCheckoutCompletedEvent checkout:
                await HandleCheckoutCompleted(checkout, cancellationToken);
                break;
            default:
                throw new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.");
        }
    }

    private async Task HandleProductCreated(StripeProductCreatedEvent evt, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        try {
            var plan = await client.Instance.GetPlan(evt.PlanNodeId, cancellationToken);

            await client.Instance.PatchPlan(new PatchPlanInput {
                Id = evt.PlanNodeId,
                Name = evt.Name,
                Description = evt.Description,
                IconUrl = evt.IconUrl,
                IsActive = evt.Active,
                ForeignServiceId = evt.ProductId
            }, cancellationToken);


        } catch (PlanNotFoundException ex) {
            logger.LogNewPlanFromStripe(evt.ProductId, ex);
            throw;
        }
    }

    private async Task HandleProductUpdated(StripeProductUpdatedEvent evt, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        await client.Instance.PatchPlan(new PatchPlanInput {
            Id = evt.PlanNodeId,
            Name = evt.Name,
            Description = evt.Description,
            IconUrl = evt.IconUrl,
            IsActive = evt.Active,
            ForeignServiceId = evt.ForeignServiceId
        }, cancellationToken);
        logger.LogProductUpdated(evt.ProductId, evt.PlanNodeId);
    }

    private async Task HandleProductDeleted(StripeProductDeletedEvent evt, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        var plan = await client.Instance.GetPlanByForeignId(evt.ProductId, cancellationToken);

        await client.Instance.DeletePlan(new DeletePlanInput {
            Id = plan.Id
        }, cancellationToken);
        logger.LogProductDeleted(evt.ProductId);
    }

    private async Task HandlePriceCreated(StripePriceCreatedEvent evt, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        var plan = await client.Instance.GetPriceTierByForeignId(evt.ProductId, cancellationToken);

        await client.Instance.PatchPriceTier(new PatchPriceTierInput {
            ForeignServiceId = evt.PriceId,
            Name = evt.Nickname,
            Price = evt.Price,
            Description = evt.Description,
            IconUrl = evt.IconUrl
        }, cancellationToken);
        logger.LogPriceCreated(evt.PriceId, evt.ProductId);
    }

    private async Task HandlePriceUpdated(StripePriceUpdatedEvent evt, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        var tier = await client.Instance.GetPriceTierByForeignId(evt.PriceId, cancellationToken);

        await client.Instance.PatchPriceTier(new PatchPriceTierInput {
            Id = tier.Id,
            Price = evt.Price
        }, cancellationToken);
        logger.LogPriceUpdated(evt.PriceId);
    }

    private async Task HandlePriceDeleted(StripePriceDeletedEvent evt, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        var tier = await client.Instance.GetPriceTierByForeignId(evt.PriceId, cancellationToken);

        await client.Instance.DeletePriceTier(new DeletePriceTierInput {
            Id = tier.Id
        }, cancellationToken);
        logger.LogPriceDeleted(evt.PriceId);
    }

    private async Task HandleCheckoutCompleted(StripeCheckoutCompletedEvent evt, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        var nudgesClient = await client.Instance.GetClientByCustomerId(evt.CustomerId, cancellationToken);

        var paymentConfirmationId = await client.Instance.CreatePaymentConfirmation(new CreatePaymentConfirmationInput {
            ConfirmationId = evt.InvoiceId,
            MerchantServiceId = evt.MerchantServiceId
        }, cancellationToken);

        await client.Instance.CreatePlanSubscription(new CreatePlanSubscriptionInput {
            ClientId = nudgesClient.Id,
            PaymentConfirmationId = paymentConfirmationId,
            PriceTierForeignServiceId = evt.PriceLineItemId
        }, cancellationToken);

        logger.LogCheckoutCompleted(evt.SessionId, nudgesClient.Id);
    }
}
