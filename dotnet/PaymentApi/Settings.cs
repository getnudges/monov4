using Nudges.Configuration;

namespace PaymentApi;

internal sealed class Settings {
    public KafkaSettings Kafka { get; set; } = new();
    public OtlpSettings Otlp { get; set; } = new();
}
