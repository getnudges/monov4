using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace ProductApi;

public static class TelemetryPropagation {
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static IReadOnlyDictionary<string, string> InjectCurrent() {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var propagationContext = new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current);
        Propagator.Inject(propagationContext, headers, static (d, k, v) => d[k] = v);
        return headers;
    }

    public static ActivityContext Extract(IReadOnlyDictionary<string, string> headers) {
        var parent = Propagator.Extract(default, headers, static (d, k) =>
            d.TryGetValue(k, out var v) ? [v] : Array.Empty<string>());

        return parent.ActivityContext;
    }
}

// Simple envelope to carry trace headers with any payload.
public sealed record TracedEvent<T>(T Payload, IReadOnlyDictionary<string, string> Headers);
