using Confluent.Kafka;

namespace UnAd.Kafka.Abstractions;
public interface IEventProducer<TKey, TEvent> : IDisposable {
    Task<DeliveryResult<TKey, TEvent>> Produce(TKey key, TEvent data, CancellationToken cancellationToken = default);
}

public interface IEventConsumer<TKey, TEvent> : IDisposable {
    void Subscribe();
    ConsumeResult<TKey, TEvent> Consume(CancellationToken cancellationToken = default);
    void Commit(ConsumeResult<TKey, TEvent> consumeResult);
    void Close();
}

public interface IEventSerializer<T> {
    byte[] Serialize(T data);
}

public interface IEventDeserializer<T> {
    T Deserialize(ReadOnlySpan<byte> data);
}

public sealed class KafkaProducer<TKey, TEvent>(string topic,
                                                ProducerConfig producerConfig,
                                                IEventSerializer<TKey> keySerializer,
                                                IEventSerializer<TEvent> valueSerializer) : IEventProducer<TKey, TEvent> {

    private readonly IProducer<TKey, TEvent> _producer = new ProducerBuilder<TKey, TEvent>(producerConfig)
            .SetKeySerializer(new KafkaSerializerAdapter<TKey>(keySerializer))
            .SetValueSerializer(new KafkaSerializerAdapter<TEvent>(valueSerializer))
            .Build();

    public Task<DeliveryResult<TKey, TEvent>> Produce(TKey key, TEvent data, CancellationToken cancellationToken = default) {
        return _producer.ProduceAsync(topic, new Message<TKey, TEvent> { Key = key, Value = data }, cancellationToken);
    }

    public void Dispose() => _producer.Dispose();
}

public class KafkaSerializerAdapter<T> : ISerializer<T> {
    private readonly IEventSerializer<T> _serializer;

    public KafkaSerializerAdapter(IEventSerializer<T> serializer) {
        _serializer = serializer;
    }

    public byte[] Serialize(T data, SerializationContext context) => _serializer.Serialize(data);
}

