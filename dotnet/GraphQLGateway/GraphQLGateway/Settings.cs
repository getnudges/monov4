using Nudges.Configuration;

namespace GraphQLGateway;

internal sealed class Settings {
    public OtlpSettings Otlp { get; set; } = new();
    public WarpCacheSettings WarpCache { get; set; } = new();
}
