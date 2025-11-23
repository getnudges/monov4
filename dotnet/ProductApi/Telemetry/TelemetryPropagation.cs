using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace ProductApi.Telemetry;

public readonly record struct TraceHeaders(
    IReadOnlyDictionary<string, string> Values);

public interface ITracePropagator {
    public TraceHeaders Inject();
    public PropagationContext Extract(TraceHeaders headers);
}

public sealed class TracePropagator : ITracePropagator {
    private readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

    public TraceHeaders Inject() {
        var carrier = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var context = new PropagationContext(
            Activity.Current?.Context ?? default,
            Baggage.Current);

        _propagator.Inject(context, carrier, (d, k, v) => d[k] = v);
        return new TraceHeaders(carrier);
    }

    public PropagationContext Extract(TraceHeaders headers) =>
        _propagator.Extract(default, headers.Values,
            (d, k) => d.TryGetValue(k, out var v) ? [v] : Array.Empty<string>());
}

public readonly record struct TracedMessage<T>(
    T Payload,
    TraceHeaders Trace);

public static class ActivityExtensions {
    public static Activity? StartConsumerActivity(
        this ActivitySource source,
        string name,
        PropagationContext parent) =>
        source.StartActivity(
            name,
            ActivityKind.Consumer,
            parent.ActivityContext);
}
