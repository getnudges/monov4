using Nudges.Configuration;

namespace UserApi;

public class Settings {
    public KafkaSettings Kafka { get; set; } = new();
    public OtlpSettings Otlp { get; set; } = new();

}
