using Confluent.Kafka;
using HotChocolate.Subscriptions;
using ProductApi.Models;
using Nudges.Data.Products;
using Nudges.Data.Products.Models;
using Nudges.Kafka;
using Nudges.Models;

namespace ProductApi;

public partial class Mutation {

    public async Task<DiscountCode> CreateDiscountCode(ProductDbContext context,
                                                                          KafkaMessageProducer<DiscountCodeKey, DiscountCodeEvent> productProducer,
                                                                          INodeIdSerializer idSerializer,
                                                                          CreateDiscountCodeInput input,
                                                                          CancellationToken cancellationToken) {

        var newDiscountCode = context.DiscountCodes.Add(new DiscountCode {
            Name = input.Name,
            Description = input.Description,
            Code = input.Code,
            Duration = input.Duration.ToTimeSpan(),
            ExpiryDate = input.ExpiryDate,
        });
        try {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogDiscountCodeCreated(newDiscountCode.Entity.Id, newDiscountCode.Entity.Name);

            var id = idSerializer.Format(nameof(DiscountCode), newDiscountCode.Entity.Id)
                ?? throw new InvalidOperationException("Failed to serialize node ID");
            //await productProducer.ProduceDiscountCodeCreated(id, cancellationToken);
            return newDiscountCode.Entity;
        } catch (KafkaException ex) {
            throw new KafkaProduceException(ex.Error, ex.GetDeepestInnerException().Message);
        } catch (Exception ex) {
            throw new DiscountCodeCreationException(ex.GetDeepestInnerException().Message);
        }
    }

    public async Task<DiscountCode> UpdateDiscountCode(ProductDbContext context,
                                                                                           ITopicEventSender subscriptionSender,
                                                                                           UpdateDiscountCodeInput input,
                                                                                           INodeIdSerializer idSerializer,
                                                                                           KafkaMessageProducer<DiscountCodeKey, DiscountCodeEvent> productProducer,
                                                                                           CancellationToken cancellationToken) {
        var discountcode = await context.DiscountCodes.FindAsync([input.Id], cancellationToken)
            ?? throw new DiscountCodeNotFoundException(input.Id, "Not Found");

        discountcode.Name = input.Name;
        discountcode.Description = input.Description;
        discountcode.Code = input.Code;
        discountcode.Description = input.Description;
        discountcode.Discount = input.Discount;

        try {
            await context.SaveChangesAsync(cancellationToken);
        } catch (Exception ex) {
            logger.LogException(ex);
            throw new DiscountCodeUpdateException(discountcode.Id, ex.InnerException?.Message ?? ex.Message);
        }

        //var id = idSerializer.Serialize(null, nameof(DiscountCode), discountcode.Id)
        //    ?? throw new InvalidOperationException("Failed to serialize node ID");
        //await productProducer.ProduceDiscountCodeUpdated(id, cancellationToken);
        //await subscriptionSender.SendAsync(nameof(Subscriptions.OnDiscountCodeUpdated), discountcode, cancellationToken);
        //await subscriptionSender.SendAsync(SubscriptionType.PriceTiersUpdated(id), discountcode, cancellationToken);
        return discountcode;
    }
    public async Task<DiscountCode> DeleteDiscountCode(ProductDbContext context,
                                                                                           KafkaMessageProducer<DiscountCodeKey, DiscountCodeEvent> productProducer,
                                                                                           INodeIdSerializer idSerializer,
                                                                                           DeleteDiscountCodeInput input,
                                                                                           CancellationToken cancellationToken) {

        var found = await context.DiscountCodes.FindAsync([input.Id], cancellationToken);
        if (found is not { } discountcode) {
            throw new DiscountCodeNotFoundException(input.Id, "Not Found");
        }

        try {
            context.DiscountCodes.Remove(discountcode);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogDiscountCodeDeleted(discountcode.Id);
        } catch (Exception ex) {
            throw new DiscountCodeDeleteException(ex.InnerException?.Message ?? ex.Message);
        }

        //if (string.IsNullOrEmpty(discountcode.ForeignServiceId)) {
        //    return discountcode;
        //}
        //var id = idSerializer.Serialize(null, nameof(DiscountCode), discountcode.Id)
        //    ?? throw new InvalidOperationException("Failed to serialize node ID");
        //await productProducer.ProduceDiscountCodeDeleted(discountcode.ForeignServiceId, cancellationToken);
        return discountcode;

    }

    public async Task<DiscountCode> PatchDiscountCode(ProductDbContext context,
                                                                                          ITopicEventSender subscriptionSender,
                                                                                          PatchDiscountCodeInput input,
                                                                                          CancellationToken cancellationToken) {
        var discountcode = await context.DiscountCodes.FindAsync([input.Id], cancellationToken)
            ?? throw new DiscountCodeNotFoundException(input.Id, "Not Found");

        discountcode.Name = input.Name ?? discountcode.Name;
        discountcode.Description = input.Description ?? discountcode.Description;
        discountcode.Code = input.Code ?? discountcode.Code;
        discountcode.Discount = input.Discount ?? discountcode.Discount;
        discountcode.Duration = input.Duration?.Duration ?? discountcode.Duration;


        try {
            await context.SaveChangesAsync(cancellationToken);
        } catch (Exception ex) {
            logger.LogException(ex);
            throw new DiscountCodeUpdateException(discountcode.Id, ex.InnerException?.Message ?? ex.Message);
        }

        //await subscriptionSender.SendAsync(nameof(Subscriptions.OnDiscountCodeUpdated), discountcode, cancellationToken);
        return discountcode;
    }

}

public class DiscountCodeCreationException(string message) : Exception(message);

public class DiscountCodeNotFoundException(int id, string message) : Exception(message) {
    public int Id => id;
}

public class DiscountCodeCreateMessageException(string message) : Exception(message);

public class DiscountCodeDeleteException(string message) : Exception(message);

public class DiscountCodeUpdateException(int discountCodeId, string message) : Exception(message) {
    public int DiscountCodeId => discountCodeId;
}

public record class DeleteDiscountCodeInput(int Id);

public class DeleteDiscountCodeInputType : InputObjectType<DeleteDiscountCodeInput> {
    protected override void Configure(IInputObjectTypeDescriptor<DeleteDiscountCodeInput> descriptor) =>
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>().ID(nameof(DiscountCode));
}

public record class CreateDiscountCodeInput(string Name,
                                            int PriceTierId,
                                            string Code,
                                            decimal Discount,
                                            BasicDuration Duration,
                                            string? Description,
                                            DateTime? ExpiryDate);
public class CreateDiscountCodeInputType : InputObjectType<CreateDiscountCodeInput> {
    protected override void Configure(IInputObjectTypeDescriptor<CreateDiscountCodeInput> descriptor) {
        descriptor.Field(f => f.PriceTierId).Type<NonNullType<IdType>>().ID(nameof(PriceTier));
        descriptor.Field(f => f.Duration).Type<NonNullType<BasicDurationType>>();
    }
}

public record class UpdateDiscountCodeInput(int Id,
                                            string Name,
                                            int PriceTierId,
                                            string Code,
                                            decimal Discount,
                                            BasicDuration Duration,
                                            string? Description,
                                            DateTime? ExpiryDate);
public class UpdateDiscountCodeInputType : InputObjectType<UpdateDiscountCodeInput> {
    protected override void Configure(IInputObjectTypeDescriptor<UpdateDiscountCodeInput> descriptor) {
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>().ID(nameof(DiscountCode));
        descriptor.Field(f => f.PriceTierId).Type<IdType>().ID(nameof(PriceTier));
        descriptor.Field(f => f.Duration).Type<BasicDurationType>();
    }
}

public record class PatchDiscountCodeInput(int Id,
                                           int? PriceTierId,
                                           string? Name,
                                           string? Code,
                                           decimal? Discount,
                                           BasicDuration? Duration,
                                           string? Description,
                                           DateTime? ExpiryDate);
public class PatchDiscountCodeInputType : InputObjectType<PatchDiscountCodeInput> {
    protected override void Configure(IInputObjectTypeDescriptor<PatchDiscountCodeInput> descriptor) {
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>().ID(nameof(DiscountCode));
        descriptor.Field(f => f.PriceTierId).ID(nameof(PriceTier));
        descriptor.Field(f => f.Duration).Type<BasicDurationType>();
    }
}
