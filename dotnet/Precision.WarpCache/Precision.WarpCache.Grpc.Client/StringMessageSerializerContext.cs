using System.Text.Json.Serialization;

namespace Precision.WarpCache.Grpc.Client;

public record struct StringMessage(string Message);

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(string))]
public sealed partial class StringMessageSerializerContext : JsonSerializerContext;
