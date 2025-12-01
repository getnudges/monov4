using System.Text.Json.Serialization;

namespace GraphMonitor;

public record struct StringMessage(string Message);

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(string))]
public sealed partial class StringMessageSerializerContext : JsonSerializerContext;
