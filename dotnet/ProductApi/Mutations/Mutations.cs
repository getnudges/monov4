using Nudges.Auth;
using Nudges.HotChocolate.Utils;

namespace ProductApi;

public partial class Mutation(ILogger<Mutation> logger) { }

public class MutationObjectType : ObjectType<Mutation> {
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor) {

        descriptor
            .Field(f => f.SubscribeToPlan(default!, default!, default!, default!, default!, default!))
            .Authorize(PolicyNames.Client)
            .Argument("input", a => a.Type<NonNullType<SubscribeToPlanInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.EndSubscription(default!, default!, default!, default!, default!))
            .Authorize(PolicyNames.Client)
            .Argument("input", a => a.Type<NonNullType<EndSubscriptionInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.CreatePlan(default!, default!, default!, default!, default!))
            .Use<TracingMiddleware>()
            .Argument("input", a => a.Type<NonNullType<CreatePlanInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.CreatePlanSubscription(default!, default!, default!, default!, default!, default!))
            .Use<TracingMiddleware>()
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<CreatePlanSubscriptionInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.UpdatePlan(default!, default!, default!, default!, default!, default!, default!))
            .Use<TracingMiddleware>()
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<UpdatePlanInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.PatchPlan(default!, default!, default!, default!, default!, default!, default!))
            .Use<TracingMiddleware>()
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<PatchPlanInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.PatchPriceTier(default!, default!, default!, default!, default!, default!))
            .Use<TracingMiddleware>()
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<PatchPriceTierInputType>>())
            .Error<PriceTierNotFoundException>()
            .Error<KafkaProduceException>()
            .UseMutationConvention();

        descriptor
            .Field(f => f.DeletePriceTier(default!, default!, default!, default!, default!))
            .Use<TracingMiddleware>()
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<DeletePriceTierInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.DeletePlan(default!, default!, default!, default!, default!))
            .Use<TracingMiddleware>()
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<DeletePlanInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.CreateDiscountCode(default!, default!, default!, default!, default!))
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<CreateDiscountCodeInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.DeleteDiscountCode(default!, default!, default!, default!, default!))
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<DeleteDiscountCodeInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.UpdateDiscountCode(default!, default!, default!, default!, default!, default!))
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<UpdateDiscountCodeInputType>>())
            .UseMutationConvention();

        descriptor
            .Field(f => f.PatchDiscountCode(default!, default!, default!, default!))
            .Authorize(PolicyNames.Admin)
            .Argument("input", a => a.Type<NonNullType<PatchDiscountCodeInputType>>())
            .UseMutationConvention();
        ;
    }
}
