using Nudges.Configuration;

namespace AuthApi;

public class Settings
{
    public OidcSettings Oidc { get; set; } = new OidcSettings();
    public KafkaSettings Kafka { get; set; } = new KafkaSettings();
    public WarpCacheSettings WarpCache { get; set; } = new WarpCacheSettings();
    public OtlpSettings Otlp { get; set; } = new();
}
