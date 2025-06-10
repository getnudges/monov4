using System.Collections.Immutable;

namespace UnAd.Webhooks.Stripe;

public sealed class StripeEventCommandProcessorBuilder() {

    private readonly ImmutableDictionary<string, IEventCommand<StripeEventContext>>.Builder _commands =
        ImmutableDictionary<string, IEventCommand<StripeEventContext>>.Empty.ToBuilder();

    private StripeEventCommandProcessorBuilder(ImmutableDictionary<string, IEventCommand<StripeEventContext>>.Builder commands) : this() =>
        _commands = commands;

    public StripeEventCommandProcessorBuilder AddHandler(string type, IEventCommand<StripeEventContext> commands) {
        _commands.Add(type, commands);
        return new StripeEventCommandProcessorBuilder(_commands);
    }

    public StripeMessageProcessor Build() => new(_commands.ToImmutableDictionary());
}


