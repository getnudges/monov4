using System.Diagnostics;
using Confluent.Kafka;

namespace Nudges.Kafka;

public abstract class KafkaMessageProducer<TKey, TValue> {
    protected static readonly ActivitySource ActivitySource = new($"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer");

    public abstract Task<DeliveryResult<TKey, TValue>> Produce(TKey key, TValue data, CancellationToken cancellationToken);
}
