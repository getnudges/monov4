using Confluent.Kafka;

namespace UnAd.Kafka.Abstractions;
public class KafkaMessageBuilder {
    private readonly IProducer<PlanKey, PlanEvent> _producer;
    private readonly string _topic;
    private readonly Message<PlanKey, PlanEvent> _message;

    public KafkaMessageBuilder(IProducer<PlanKey, PlanEvent> producer, string topic, PlanKey key, PlanEvent value) {
        _producer = producer;
        _topic = topic;
        _message = new Message<PlanKey, PlanEvent> {
            Key = key,
            Value = value,
            Headers = []
        };
    }

    public KafkaMessageBuilder WithHeader(string key, string value) {
        _message.Headers.Add(key, System.Text.Encoding.UTF8.GetBytes(value));
        return this;
    }

    public KafkaMessageBuilder WithTraceContext() {
        var currentActivity = System.Diagnostics.Activity.Current;
        if (currentActivity?.Id is string activity) {
            _message.Headers.Add("traceparent", System.Text.Encoding.UTF8.GetBytes(activity));
            if (currentActivity.TraceStateString is string state) {
                _message.Headers.Add("tracestate", System.Text.Encoding.UTF8.GetBytes(state));
            }
        }
        return this;
    }

    public Task<DeliveryResult<PlanKey, PlanEvent>> SendAsync(CancellationToken cancellationToken = default) =>
        _producer.ProduceAsync(_topic, _message, cancellationToken);
}
