using System.ComponentModel.DataAnnotations;
using Nudges.Configuration;

namespace AuthApi;

public class Settings {
    [Required]
    public OidcSettings Oidc { get; set; } = new OidcSettings();
    [Required]
    public KafkaSettings Kafka { get; set; } = new KafkaSettings();
    [Required]
    public WarpCacheSettings WarpCache { get; set; } = new WarpCacheSettings();
    public OtlpSettings Otlp { get; set; } = new();
}
