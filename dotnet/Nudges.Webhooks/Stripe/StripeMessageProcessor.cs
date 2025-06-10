using System.Collections.Immutable;
using Monads;

namespace Nudges.Webhooks.Stripe;

public sealed class StripeMessageProcessor(ImmutableDictionary<string, IEventCommand<StripeEventContext>> commands) {

    public async Task<Maybe<Exception>> ProcessEvent(StripeEventContext context, CancellationToken cancellationToken) {
        if (commands.TryGetValue(context.StripeEvent.Type, out var command)) {
            return await command.InvokeAsync(context, cancellationToken);
        }
        return Maybe<Exception>.None;
    }
}


