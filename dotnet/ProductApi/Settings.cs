using Nudges.Configuration;

namespace ProductApi;

internal sealed class Settings {
    public OidcSettings Oidc { get; set; } = new();
    public OtlpSettings Otlp { get; set; } = new();
    public KafkaSettings Kafka { get; set; } = new();

}
