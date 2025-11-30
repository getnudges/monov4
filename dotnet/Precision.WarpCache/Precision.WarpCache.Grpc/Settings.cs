using Nudges.Configuration;

namespace Precision.WarpCache.Grpc;

internal class Settings {
    public OtlpSettings Otlp { get; set; } = new();
}
