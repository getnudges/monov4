using Monads;

namespace UnAd.Webhooks.Stripe;

public interface IEventCommand<TContext> {
    public Task<Maybe<Exception>> InvokeAsync(TContext context, CancellationToken cancellationToken);
}


