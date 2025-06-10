using System.Diagnostics;
using System.Text;
using Confluent.Kafka;

namespace UnAd.Kafka.Middleware;

public static class MessageExtensions {
    public static ActivityContext? GetActivityContext<TKey, TValue>(this Message<TKey, TValue> message) {
        var traceparent = message.Headers.FirstOrDefault(h => h.Key == "traceparent")?.GetValueBytes();
        var tracestate = message.Headers.FirstOrDefault(h => h.Key == "tracestate")?.GetValueBytes();
        if (traceparent is byte[] parent && parent.Length > 0) {
            if (traceparent is byte[] tParent && tracestate is byte[] state && state.Length > 0) {
                return ActivityContext.Parse(Encoding.UTF8.GetString(tParent), Encoding.UTF8.GetString(state));
            }
            return ActivityContext.Parse(Encoding.UTF8.GetString(parent), default);
        }

        return Activity.Current?.Context;
    }
}
