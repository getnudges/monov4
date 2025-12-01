using System.Diagnostics;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Nudges.Auth;
using Nudges.Data.Products;
using Nudges.Data.Products.Models;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Models;
using Nudges.Telemetry;
using ProductApi.Models;
using ProductApi.Mutations;

namespace ProductApi;

public partial class Mutation {
    public static readonly ActivitySource ActivitySource = new($"{typeof(Mutation).FullName}");

    [Error<PlanNotFoundException>]
    [Error<PlanDeleteException>]
    public async Task<Plan> DeletePlan(ProductDbContext context,
                                       KafkaMessageProducer<PlanEventKey, PlanChangeEvent> productProducer,
                                       DeletePlanInput input,
                                       CancellationToken cancellationToken) {

        var found = await context.Plans.FindAsync([input.Id], cancellationToken);
        if (found is not { } plan) {
            throw new PlanNotFoundException(input.Id);
        }

        try {
            context.Plans.Remove(plan);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogPlanDeleted(plan.Id, plan.Name);
        } catch (Exception ex) {
            throw new PlanDeleteException(ex.InnerException?.Message ?? ex.Message);
        }

        if (string.IsNullOrEmpty(plan.ForeignServiceId)) {
            return plan;
        }
        await productProducer.ProducePlanDeleted(plan.ToPlanDeletedEvent(DateTimeOffset.UtcNow), cancellationToken);
        return plan;

    }

    [Error<PriceTierNotFoundException>]
    [Error<PriceTierDeleteException>]
    public async Task<Plan> DeletePriceTier(ProductDbContext context,
                                            KafkaMessageProducer<PriceTierEventKey, PriceTierChangeEvent> eventProducer, DeletePriceTierInput input,
                                            CancellationToken cancellationToken) {

        var found = await context.PriceTiers.FindAsync([input.Id], cancellationToken);
        if (found is not { } priceTier) {
            throw new PriceTierNotFoundException(input.Id);
        }

        priceTier.Status = BasicStatus.Inactive.ToStatusString();
        try {
            await context.SaveChangesAsync(cancellationToken);
        } catch (Exception ex) {
            throw new PriceTierDeleteException(priceTier.Id, ex.Message);
        }
        logger.LogPriceTierDeleted(priceTier.Id, priceTier.Name);

        if (string.IsNullOrEmpty(priceTier.ForeignServiceId)) {
            return priceTier.Plan!;
        }

        await eventProducer.ProducePriceTierDeleted(priceTier.ToPriceTierDeletedEvent(DateTimeOffset.UtcNow), cancellationToken);
        return priceTier.Plan!;

    }

    [Authorize(Roles = [ClaimValues.Roles.Admin])]
    [Error<PlanCreationException>]
    public async Task<Plan> CreatePlan(ProductDbContext context,
                                       KafkaMessageProducer<PlanEventKey, PlanChangeEvent> productProducer,
                                       CreatePlanInput input,
                                       CancellationToken cancellationToken) {

        var newPlan = context.Plans.Add(input.ToPlan());

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogPlanCreated(newPlan.Entity.Id, newPlan.Entity.Name);

            await productProducer.ProducePlanCreated(newPlan.Entity.ToPlanCreatedEvent(), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return newPlan.Entity;
        } catch (Exception ex) {
            await transaction.RollbackAsync(cancellationToken);
            throw new PlanCreationException(ex.GetBaseException().Message);
        }
    }

    [Error<PlanCreationException>]
    public async Task<PlanSubscription> CreatePlanSubscription(ProductDbContext context,
                                                                KafkaMessageProducer<PlanSubscriptionKey, PlanSubscriptionEvent> kafkaProducer,
                                                                CreatePlanSubscriptionInput input,
                                                                HttpContext httpContext,
                                                                CancellationToken cancellationToken) {

        using var activity = ActivitySource.StartActivity(nameof(CreatePlanSubscription), ActivityKind.Server, httpContext.Request.GetActivityContext());
        activity?.Start();

        var priceTier = await context.PriceTiers.Include(t => t.Plan).FirstOrDefaultAsync(pt => pt.ForeignServiceId == input.PriceTierForeignServiceId, cancellationToken)
            ?? throw new PlanCreationException($"Price tier with PriceTierForeignServiceId {input.PriceTierForeignServiceId} not found");

        // TODO: it would be nice to have the node IDs deserialized properly here
        var newSub = context.PlanSubscriptions.Add(new PlanSubscription {
            ClientId = Guid.Parse(input.ClientId),
            PaymentConfirmationId = Guid.Parse(input.PaymentConfirmationId),
            EndDate = DateTime.UtcNow.Add(priceTier.Duration),
            PriceTierId = priceTier.Id,
            StartDate = DateTime.UtcNow,
            Status = BasicStatus.Active.ToStatusString(),
            //Trials = [],
            //Discounts = [],
        });

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogPlanSubscriptionCreated(newSub.Entity.Id);

            await kafkaProducer.Produce(
                PlanSubscriptionKey.PlanSubscriptionCreated(newSub.Entity.Id),
                new PlanSubscriptionCreatedEvent(newSub.Entity.ClientId, newSub.Entity.Id),
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return newSub.Entity;
        } catch (Exception ex) {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetDeepestInnerException().Message);
            await transaction.RollbackAsync(cancellationToken);
            throw new PlanCreationException(ex.GetDeepestInnerException().Message);
        }
    }

    [Error<PlanNotFoundException>]
    [Error<PlanUpdateException>]
    public async Task<Plan> UpdatePlan(ProductDbContext context,
                                       ITopicEventSender subscriptionSender,
                                       UpdatePlanInput input,
                                       KafkaMessageProducer<PlanEventKey, PlanChangeEvent> productProducer,
                                       KafkaMessageProducer<PriceTierEventKey, PriceTierChangeEvent> priceTierEventProducer,
                                       CancellationToken cancellationToken) {

        var plan = await context.Plans.FindAsync([input.Id], cancellationToken)
            ?? throw new PlanNotFoundException(input.Id);

        plan.Name = input.Name;
        plan.Description = input.Description;
        plan.IsActive = input.IsActive;
        plan.IconUrl = input.IconUrl;
        // so it doesn't get overwritten if you save again too quickly
        //plan.ForeignServiceId = input.ForeignServiceId;

        await context.Entry(plan).Reference(p => p.PlanFeature).LoadAsync(cancellationToken);
        plan.PlanFeature!.AiSupport = input.Features.AiSupport;
        plan.PlanFeature!.MaxMessages = input.Features.MaxMessages;
        plan.PlanFeature!.SupportTier = input.Features.SupportTier;

        await context.Entry(plan).Collection(p => p.PriceTiers).LoadAsync(cancellationToken);
        var inputs = input.PriceTiers?.UnionBy(plan.PriceTiers.Select(t => t.ToUpdatePlanPriceTierInput()), t => t.Id) ?? [];
        var tiersToUpdate = UpdatePriceTierCollection(inputs, plan);
        foreach (var tierUpdate in tiersToUpdate) {
            if (tierUpdate.Id is > 0) {
                context.PriceTiers.Update(tierUpdate);
            } else {
                plan.PriceTiers.Add(tierUpdate);
            }
        }

        try {
            await context.SaveChangesAsync(cancellationToken);
        } catch (Exception ex) {
            logger.LogException(ex);
            throw new PlanUpdateException(plan.Id, ex.InnerException?.Message ?? ex.Message);
        }

        foreach (var tierToReport in tiersToUpdate) {
            if (tierToReport.ForeignServiceId is null) {
                await priceTierEventProducer.ProducePriceTierCreated(tierToReport.ToPriceTierCreatedEvent(), cancellationToken);
            }
        }

        await productProducer.ProducePlanUpdated(plan.ToPlanUpdatedEvent(), cancellationToken);

        plan.PlanFeature.Plan = default!;
        foreach (var tier in plan.PriceTiers) {
            tier.Plan = null;
        }

        if (input.Features is not null) {
            // TODO: send event
        }
        await subscriptionSender.SendAsync(nameof(Subscription.OnPlanUpdated), plan, cancellationToken);
        return plan;
    }

    [Error<PlanUpdateException>]
    [Error<PlanNotFoundException>]
    public async Task<Plan> PatchPlan(ProductDbContext context,
                                      ITopicEventSender subscriptionSender,
                                      PatchPlanInput input,
                                      HttpContext httpContext,
                                      KafkaMessageProducer<PriceTierEventKey, PriceTierChangeEvent> priceTierEventProducer,
                                      CancellationToken cancellationToken) {

        using var activity = ActivitySource.StartActivity(nameof(PatchPlan), ActivityKind.Server, httpContext.Request.GetActivityContext());
        activity?.Start();

        var plan = await context.Plans.FindAsync([input.Id], cancellationToken)
            ?? throw new PlanNotFoundException(input.Id);
        await context.Entry(plan).Reference(p => p.PlanFeature).LoadAsync(cancellationToken);

        plan.Name = input.Name ?? plan.Name;
        plan.Description = input.Description ?? plan.Description;
        plan.IsActive = input.IsActive;
        plan.IconUrl = input.IconUrl ?? plan.IconUrl;
        plan.ForeignServiceId = input.ForeignServiceId ?? plan.ForeignServiceId;

        if (input.Features is { } features) {
            plan.PlanFeature = input.Features is { } feature ? new PlanFeature {
                SupportTier = feature.SupportTier ?? plan.PlanFeature?.SupportTier,
                AiSupport = feature.AiSupport ?? plan.PlanFeature?.AiSupport,
                MaxMessages = feature.MaxMessages ?? plan.PlanFeature?.MaxMessages,
            } : plan.PlanFeature;
        }

        await context.Entry(plan).Collection(p => p.PriceTiers).LoadAsync(cancellationToken);
        var inputs = input.PriceTiers?.UnionBy(plan.PriceTiers.Select(t => t.ToPatchPlanPriceTierInput()), t => t.Id) ?? [];
        var tiersToUpdate = PatchPriceTierCollection(inputs, plan);
        foreach (var tierUpdate in tiersToUpdate) {
            if (tierUpdate.Id is > 0) {
                context.Entry(tierUpdate).State = EntityState.Modified;
            } else {
                plan.PriceTiers.Add(tierUpdate);
            }
        }

        try {
            await context.SaveChangesAsync(cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
        } catch (Exception ex) {
            logger.LogException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw new PlanUpdateException(plan.Id, ex.InnerException?.Message ?? ex.Message);
        }
        if (plan.PlanFeature?.Plan is not null) {
            plan.PlanFeature.Plan = default!;
        }
        foreach (var tier in plan.PriceTiers) {
            if (tier.Plan is not null) {
                tier.Plan = null;
            }
        }
        if (input.Features is not null) {
            // TODO: send event
        }
        foreach (var tierToReport in tiersToUpdate) {
            if (tierToReport.ForeignServiceId is null) {
                await priceTierEventProducer.ProducePriceTierCreated(tierToReport.ToPriceTierCreatedEvent(), cancellationToken);
            } else {
                await priceTierEventProducer.ProducePriceTierUpdated(tierToReport.ToPriceTierUpdatedEvent(), cancellationToken);
            }
        }

        // Inject current Activity into headers and send traced event
        var headers = tracePropagator.Inject();
        await subscriptionSender.SendAsync(nameof(Subscription.OnPlanUpdated), new TracedMessage<Plan>(plan, headers), cancellationToken);
        return plan;
    }

    private static IEnumerable<PriceTier> PatchPriceTierCollection(IEnumerable<PatchPlanPriceTierInput> input, Plan plan) {
        foreach (var inputTier in input) {
            var existingTier = plan.PriceTiers.FirstOrDefault(t => t.Id == inputTier.Id)
                ?? plan.PriceTiers.FirstOrDefault(t => t.ForeignServiceId == inputTier.ForeignServiceId);
            if (existingTier is not null) {
                existingTier.Name = inputTier.Name ?? existingTier.Name;
                existingTier.Price = inputTier.Price ?? existingTier.Price;
                existingTier.Duration = inputTier.Duration ?? existingTier.Duration;
                existingTier.Description = inputTier.Description ?? existingTier.Description;
                existingTier.IconUrl = inputTier.IconUrl ?? existingTier.IconUrl;
                existingTier.Status = inputTier.Status ?? existingTier.Status;
                existingTier.ForeignServiceId = inputTier.ForeignServiceId ?? existingTier.ForeignServiceId;

                yield return existingTier;
            } else {
                yield return new PriceTier {
                    ForeignServiceId = inputTier.ForeignServiceId,
                    Name = inputTier.Name!,
                    Price = inputTier.Price!.Value,
                    Duration = inputTier.Duration ?? TimeSpan.Zero, // NOTE: This fallback should never happen
                    Status = inputTier.Status ?? BasicStatus.Active.ToStatusString(),
                    Description = inputTier.Description,
                    IconUrl = inputTier.IconUrl,
                };
            }
        }
    }

    private static IEnumerable<PriceTier> UpdatePriceTierCollection(IEnumerable<UpdatePlanPriceTierInput> input, Plan plan) {
        foreach (var inputTier in input) {
            var existingTier = plan.PriceTiers.FirstOrDefault(t => t.Id == inputTier.Id);
            if (existingTier is not null) {
                existingTier.Price = inputTier.Price;
                existingTier.Duration = inputTier.Duration;
                existingTier.Description = inputTier.Description ?? existingTier.Description;
                existingTier.IconUrl = inputTier.IconUrl ?? existingTier.IconUrl;
                existingTier.Status = inputTier.Status ?? existingTier.Status;

                yield return existingTier;
            } else {
                yield return new PriceTier {
                    Name = inputTier.Name,
                    Price = inputTier.Price,
                    Duration = inputTier.Duration, // NOTE: This should never happen
                    Status = inputTier.Status ?? BasicStatus.Active.ToStatusString(),
                    Description = inputTier.Description,
                    IconUrl = inputTier.IconUrl,
                };
            }
        }
    }

    [Error<PriceTierUpdateException>]
    public async Task<Plan> PatchPriceTier(ProductDbContext context,
                                            ITopicEventSender subscriptionSender,
                                            PatchPriceTierInput input,
                                            INodeIdSerializer idSerializer,
                                            HttpContext httpContext,
                                            CancellationToken cancellationToken) {

        using var activity = ActivitySource.StartActivity(nameof(PatchPriceTier), ActivityKind.Server, httpContext.Request.GetActivityContext());
        activity?.Start();
        var tier = await context.PriceTiers.FindAsync([input.Id], cancellationToken) ?? throw new PriceTierNotFoundException(input.Id);

        tier.Name = input.Name ?? tier.Name;
        tier.Description = input.Description ?? tier.Description;
        tier.Duration = input.Duration ?? tier.Duration;
        tier.IconUrl = input.IconUrl ?? tier.IconUrl;
        tier.Price = input.Price ?? tier.Price;
        tier.ForeignServiceId = input.ForeignServiceId ?? tier.ForeignServiceId;
        tier.Status = input.Status ?? tier.Status;

        try {
            await context.SaveChangesAsync(cancellationToken);
            await context.Entry(tier).Reference(p => p.Plan).LoadAsync(cancellationToken);
            tier.Plan!.PriceTiers = null!;
        } catch (Exception ex) {
            logger.LogException(ex);
            throw new PriceTierUpdateException(tier.Id, ex.GetDeepestInnerException().Message);
        }
        if (tier.Plan.PriceTiers is not null) {
            tier.Plan.PriceTiers = default!;
        }
        var id = idSerializer.Format(nameof(Plan), tier.PlanId);

        await subscriptionSender.SendAsync(nameof(Subscription.OnPriceTierUpdated), tier, cancellationToken);
        return tier.Plan!;
    }
}

public static class PriceTierTypeExtensions {
    public static PatchPlanPriceTierInput ToPatchPlanPriceTierInput(this PriceTier tier) =>
        new(
            tier.Id,
            tier.Price,
            tier.Duration,
            tier.Status,
            tier.Name,
            tier.Description,
            tier.ForeignServiceId,
            tier.IconUrl
        );
    public static UpdatePlanPriceTierInput ToUpdatePlanPriceTierInput(this PriceTier tier) =>
        new(
            tier.PlanId,
            tier.Id,
            tier.Price,
            tier.Duration,
            tier.Status,
            tier.Name,
            tier.Description,
            tier.ForeignServiceId,
            tier.IconUrl
        );
}

public class PlanCreationException(string message) : Exception(message);

public class PlanCreateMessageException(string message) : Exception(message);


public class PlanDeleteException(string message) : Exception(message);
public class PlanUpdateException(int planId, string message) : Exception(message) {
    public int PlanId => planId;
}


public class PriceTierDeleteException(int priceTierId, string message) : Exception(message) {
    public int PriceTierId => priceTierId;
}



public record class DeletePlanInput(int Id);
public class DeletePlanInputType : InputObjectType<DeletePlanInput> {
    protected override void Configure(IInputObjectTypeDescriptor<DeletePlanInput> descriptor) =>
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>().ID(nameof(Plan));
}
public record class DeletePriceTierInput(int Id);
public class DeletePriceTierInputType : InputObjectType<DeletePriceTierInput> {
    protected override void Configure(IInputObjectTypeDescriptor<DeletePriceTierInput> descriptor) =>
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>().ID(nameof(PriceTier));
}

public record class CreatePlanInput(string Name,
                              string? Description,
                              string? IconUrl,
                              IReadOnlyCollection<CreatePlanPriceTierInput>? PriceTiers,
                              CreatePlanFeatureInput? Features,
                              bool? ActivateOnCreate = false);
public class CreatePlanInputType : InputObjectType<CreatePlanInput> {
    protected override void Configure(IInputObjectTypeDescriptor<CreatePlanInput> descriptor) {
        descriptor.Field(f => f.PriceTiers).Type<ListType<NonNullType<CreatePlanPriceTierInputType>>>();
        descriptor.Field(f => f.Features).Type<CreatePlanFeatureInputType>();
        descriptor.Field(f => f.ActivateOnCreate).Type<BooleanType>().DefaultValue(false);
    }
}

public record class CreatePlanSubscriptionInput(string ClientId, string PriceTierForeignServiceId, string PaymentConfirmationId);
public class CreatePlanSubscriptionInputType : InputObjectType<CreatePlanSubscriptionInput> {
    protected override void Configure(IInputObjectTypeDescriptor<CreatePlanSubscriptionInput> descriptor) {
        descriptor.Field(f => f.ClientId).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.PriceTierForeignServiceId).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.PaymentConfirmationId).Type<NonNullType<StringType>>();
    }
}

public record class CreatePlanPriceTierInput(decimal Price, TimeSpan Duration, string Name, string? Description, string? IconUrl);
public class CreatePlanPriceTierInputType : InputObjectType<CreatePlanPriceTierInput> { }

public record class CreatePlanFeatureInput(string SupportTier, bool AiSupport, int MaxMessages);
public class CreatePlanFeatureInputType : InputObjectType<CreatePlanFeatureInput> { }


public record class CreatePriceTierInput(int PlanId, decimal Price, TimeSpan Duration, string Name, string? Description, string? IconUrl);
public class CreatePriceTierInputType : InputObjectType<CreatePriceTierInput> {
    protected override void Configure(IInputObjectTypeDescriptor<CreatePriceTierInput> descriptor) =>
        descriptor.Field(f => f.PlanId).Type<NonNullType<IdType>>().ID(nameof(Plan));
}

public record class UpdatePlanInput(int Id,
                                    string Name,
                                    string? Description,
                                    string? IconUrl,
                                    string? ForeignServiceId,
                                    IReadOnlyCollection<UpdatePlanPriceTierInput>? PriceTiers,
                                    UpdatePlanFeatureInput Features,
                                    bool? IsActive);
public class UpdatePlanInputType : InputObjectType<UpdatePlanInput> {
    protected override void Configure(IInputObjectTypeDescriptor<UpdatePlanInput> descriptor) {
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>().ID(nameof(Plan));
        descriptor.Field(f => f.PriceTiers).Type<ListType<NonNullType<UpdatePlanPriceTierInputType>>>();
        descriptor.Field(f => f.Features).Type<UpdatePlanFeatureInputType>();
    }
}

public record class UpdatePlanPriceTierInput(int PlanId,
                                             int Id,
                                             decimal Price,
                                             TimeSpan Duration,
                                             string Status,
                                             string Name,
                                             string? Description,
                                             string? ForeignServiceId,
                                             string? IconUrl);
public class UpdatePlanPriceTierInputType : InputObjectType<UpdatePlanPriceTierInput> {
    protected override void Configure(IInputObjectTypeDescriptor<UpdatePlanPriceTierInput> descriptor) {
        descriptor.Field(f => f.Id).Type<IdType>().ID(nameof(PriceTier));
        descriptor.Field(f => f.PlanId).Type<NonNullType<IdType>>().ID(nameof(Plan));
    }
}

public record class UpdatePlanFeatureInput(int PlanId, string? SupportTier, bool? AiSupport, int? MaxMessages);
public class UpdatePlanFeatureInputType : InputObjectType<UpdatePlanFeatureInput> {
    protected override void Configure(IInputObjectTypeDescriptor<UpdatePlanFeatureInput> descriptor) =>
        descriptor.Field(f => f.PlanId).Type<NonNullType<IdType>>().ID(nameof(Plan));
}

public record class PatchPlanInput(int Id,
                                   string? Name,
                                   string? Description,
                                   string? IconUrl,
                                   string? ForeignServiceId,
                                   IReadOnlyCollection<PatchPlanPriceTierInput>? PriceTiers,
                                   PatchPlanFeatureInput? Features,
                                   bool? IsActive);
public class PatchPlanInputType : InputObjectType<PatchPlanInput> {
    protected override void Configure(IInputObjectTypeDescriptor<PatchPlanInput> descriptor) {
        descriptor.Field(f => f.Id).Type<NonNullType<IntType>>();
        descriptor.Field(f => f.PriceTiers).Type<ListType<NonNullType<PatchPlanPriceTierInputType>>>();
        descriptor.Field(f => f.Features).Type<PatchPlanFeatureInputType>();
    }
}

public record class PatchPriceTierInput(int Id,
                                        decimal? Price,
                                        TimeSpan? Duration,
                                        string? Name,
                                        string? Description,
                                        string? ForeignServiceId,
                                        string? IconUrl,
                                        string? Status);
public class PatchPriceTierInputType : InputObjectType<PatchPriceTierInput> {
    protected override void Configure(IInputObjectTypeDescriptor<PatchPriceTierInput> descriptor) =>
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>().ID(nameof(PriceTier));
}

public record class PatchPlanPriceTierInput(int? Id,
                                            decimal? Price,
                                            TimeSpan? Duration,
                                            string? Status,
                                            string? Name,
                                            string? Description,
                                            string? ForeignServiceId,
                                            string? IconUrl);
public class PatchPlanPriceTierInputType : InputObjectType<PatchPlanPriceTierInput> {
    protected override void Configure(IInputObjectTypeDescriptor<PatchPlanPriceTierInput> descriptor) {
        descriptor.Field(f => f.Id).Type<IdType>().ID(nameof(PriceTier));
        descriptor.Field(f => f.Duration).Type<BasicDurationType>();
        descriptor.Field(f => f.Status).Type<BasicStatusType>();
    }
}

public record class PatchPlanFeatureInput(int PlanId, string? SupportTier, bool? AiSupport, int? MaxMessages);
public class PatchPlanFeatureInputType : InputObjectType<PatchPlanFeatureInput> {
    protected override void Configure(IInputObjectTypeDescriptor<PatchPlanFeatureInput> descriptor) =>
        descriptor.Field(f => f.PlanId).Type<NonNullType<IdType>>().ID(nameof(Plan));
}

public class PlanNotFoundException(int planId) : Exception($"Plan with ID {planId} not found") {
    public int PlanId => planId;
}

public class PriceTierUpdateException(int priceTierId, string message) : Exception($"Price Tier with ID {priceTierId} not found: {message}") {
    public int PriceTierId => priceTierId;
}
