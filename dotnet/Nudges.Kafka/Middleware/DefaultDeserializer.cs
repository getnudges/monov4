using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Nudges.Kafka.Middleware;

public sealed class DefaultDeserializer<TValue> : IDeserializer<TValue> {
    public TValue? Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        isNull ? default : JsonSerializer.Deserialize<TValue>(data);
}

public sealed class DefaultSerializer<TValue> : ISerializer<TValue> {
    public byte[] Serialize(TValue data, SerializationContext context) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
}
