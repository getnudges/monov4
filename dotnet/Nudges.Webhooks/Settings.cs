using Nudges.Configuration;

namespace Nudges.Webhooks;

internal sealed class Settings {
    public KafkaSettings Kafka { get; set; } = new();
    public OtlpSettings Otlp { get; set; } = new();
    public OidcSettings Oidc { get; set; } = new();
    public WarpCacheSettings WarpCache { get; set; } = new();
}
