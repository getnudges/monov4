using Confluent.Kafka;
using HotChocolate.Subscriptions;
using Nudges.Data.Products;
using Nudges.Data.Products.Models;
using Nudges.Kafka;

namespace ProductApi;

public partial class Mutation {

    public async Task<PlanSubscription> SubscribeToPlan(SubscribeToPlanInput input,
                                                        ProductDbContext context,
                                                        INodeIdSerializer idSerializer,
                                                        ITopicEventSender subscriptionSender,
                                                        KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                                                        CancellationToken cancellationToken) {

        var priceTier = await context.PriceTiers.FindAsync([input.PriceTierId], cancellationToken);

        if (priceTier is null) {
            throw new PriceTierNotFoundException(input.PriceTierId);
        }

        var newSub = context.PlanSubscriptions.Add(new PlanSubscription {
            PriceTierId = input.PriceTierId,
            ClientId = input.ClientId,
            PaymentConfirmationId = input.PaymentConfirmationId,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.Add(priceTier.Duration),
            Status = "ACTIVE",
        });
        await context.SaveChangesAsync(cancellationToken);

        try {
            var id = idSerializer.Format(nameof(PlanSubscription), newSub.Entity.Id)
                ?? throw new InvalidOperationException("Failed to serialize node ID");
            await notificationProducer.Produce(NotificationKey.StartSubscription(id), NotificationEvent.Empty, cancellationToken);
            //await subscriptionSender.SendAsync(SubscriptionType.SubscriptionStarted, newSub.Entity, cancellationToken);
        } catch (ProduceException<NotificationKey, NotificationEvent> e) {
            logger.LogException(e);
            throw new KafkaProduceException(e.Error, e.Message);
        } catch (InvalidOperationException e) {
            logger.LogException(e);
            throw;
        }
        return newSub.Entity;
    }

    public async Task<PlanSubscription> EndSubscription(EndSubscriptionInput input,
                                                        ProductDbContext context,
                                                        INodeIdSerializer idSerializer,
                                                        KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                                                        CancellationToken cancellationToken) {
        var planSubscription = await context.PlanSubscriptions.FindAsync([input.Id], cancellationToken);
        if (planSubscription is null) {
            throw new PlanSubscriptionNotFoundException(input.Id);
        }
        // TODO: additional validation?  Maybe check if it's already ended?
        planSubscription.EndDate = DateTime.UtcNow;
        planSubscription.Status = "ENDED";
        await context.SaveChangesAsync(cancellationToken);

        try {
            var id = idSerializer.Format(nameof(PlanSubscription), planSubscription.Id)
                ?? throw new InvalidOperationException("Failed to serialize node ID");
            await notificationProducer.Produce(NotificationKey.EndSubscription(id), NotificationEvent.Empty, cancellationToken);
        } catch (ProduceException<string, string> e) {
            logger.LogException(e);
            throw new KafkaProduceException(e.Error, e.Message);
        } catch (InvalidOperationException e) {
            logger.LogException(e);
            throw;
        }

        return planSubscription;
    }
}

public class PriceTierNotFoundException(int id) : Exception($"Price Tier with ID {id} not found");

public class KafkaProduceException(ErrorCode errorCode, string message) : Exception(message) {
    public ErrorCode ErrorCode => errorCode;
}

public record SubscribeToPlanInput(int PriceTierId, Guid ClientId, Guid PaymentConfirmationId);

public record EndSubscriptionInput(Guid Id);
public class EndSubscriptionInputType : InputObjectType<EndSubscriptionInput> {
    protected override void Configure(IInputObjectTypeDescriptor<EndSubscriptionInput> descriptor) =>
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>().ID(nameof(PlanSubscription));
}

public class SubscribeToPlanInputType : InputObjectType<SubscribeToPlanInput> {
    protected override void Configure(IInputObjectTypeDescriptor<SubscribeToPlanInput> descriptor) {
        descriptor.Field(f => f.PriceTierId).Type<NonNullType<IdType>>().ID(nameof(PriceTier));
        descriptor.Field(f => f.ClientId).Type<NonNullType<IdType>>().ID("Client");
    }
}

public class PlanSubscriptionNotFoundException(Guid id) : Exception($"Plan Subscription with ID {id} not found") {
    public Guid Id => id;
}
