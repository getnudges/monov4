using System.Globalization;
using ErrorOr;
using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Webhooks.GraphQL;
using Stripe;

namespace Nudges.Webhooks.Stripe.Commands;

internal sealed class ProductCreatedCommand(INudgesClient nudgesClient,
                                            ILogger<ProductCreatedCommand> logger,
                                            KafkaMessageProducer<ForeignProductEventKey, ForeignProductEvent> foreignProductProducer)
    : IEventCommand<StripeEventContext> {

    public async Task<Maybe<Exception>> InvokeAsync(StripeEventContext context, CancellationToken cancellationToken) {
        if (context.StripeEvent.Data.Object is not Product product) {
            return new MissingDataException("Could not find product in event data");
        }

        var result = await nudgesClient.GetPlanByForeignId(product.Id, cancellationToken);
        
        if (result.IsError) {
            var error = result.FirstError;
            if (error.Type == ErrorType.NotFound) {
                logger.LogNewPlanFromStripe(product.Id);
                return Maybe<Exception>.None;
            }
            return new GraphQLException(error.Description);
        }

        var plan = result.Value;
        logger.LogPlanAlreadyExists(plan.Id, product.Id);
        try {
            await foreignProductProducer.ProduceForeignProductSynchronized(
                    product.ToForeignProductSynchronizedEvent(),
                    cancellationToken);
        } catch (Exception ex) {
            return ex;
        }
        return Maybe<Exception>.None;
    }
}

public static class MappingExtensions {
    public static ForeignProductCreatedEvent ToForeignProductCreatedEvent(this Product product) =>
        new(
            ForeignProductId: product.Id,
            Name: product.Name,
            Description: product.Description,
            IconUrl: product.Images?.FirstOrDefault());

    public static ForeignProductSynchronizedEvent ToForeignProductSynchronizedEvent(this Product product) =>
        new(
            PlanId: Convert.ToInt32(product.Metadata["planId"], CultureInfo.InvariantCulture),
            ForeignProductId: product.Id,
            Name: product.Name,
            Description: product.Description,
            IconUrl: product.Images?.FirstOrDefault());
}

internal static partial class ProductCreatedCommandLogs {
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Processed ProductCreated event for Product ID: {ProductId}")]
    public static partial void LogProductCreatedProcessed(this ILogger logger, string productId);
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Plan already exists for Foreign Product ID: {ForeignProductId}, producing SYNC event")]
    public static partial void LogPlanAlreadyExists(this ILogger logger, string planId, string foreignProductId);
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "New plan detected from Stripe for Foreign Product ID: {ForeignProductId}, ignoring.")]
    public static partial void LogNewPlanFromStripe(this ILogger logger, string foreignProductId);
}
