using System;
using System.Collections.Generic;
using System.Text;
using Nudges.Configuration;

namespace KafkaConsumer;

internal class Settings {
    public OidcSettings Oidc { get; set; } = new();
    public WarpCacheSettings WarpCache { get; set; } = new();
    public OtlpSettings Otlp { get; set; } = new();
    public KafkaSettings Kafka { get; set; } = new();
}
