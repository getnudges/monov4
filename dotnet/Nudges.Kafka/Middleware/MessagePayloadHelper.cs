using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Nudges.Kafka.Middleware;

public static class MessagePayloadHelper {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = false
    };

    public static string? SerializeToJson<T>(T value) {
        if (value is null) {
            return null;
        }

        try {
            return JsonSerializer.Serialize(value, JsonOptions);
        } catch (Exception) {
            // Serialization failed - return placeholder
            return $"<serialization_failed: {typeof(T).Name}>";
        }
    }

    public static Dictionary<string, string>? ExtractHeaders<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult) {
        ArgumentNullException.ThrowIfNull(consumeResult);

        if (consumeResult.Message?.Headers is null || consumeResult.Message.Headers.Count == 0) {
            return null;
        }

        var headers = new Dictionary<string, string>();

        foreach (var header in consumeResult.Message.Headers) {
            try {
                // Kafka headers are byte arrays - decode as UTF-8 strings
                var value = header.GetValueBytes() is { } bytes
                    ? Encoding.UTF8.GetString(bytes)
                    : string.Empty;

                headers[header.Key] = value;
            } catch (Exception) {
                // If decoding fails, store as base64
                headers[header.Key] = header.GetValueBytes() is { } bytes
                    ? Convert.ToBase64String(bytes)
                    : string.Empty;
            }
        }

        return headers.Count > 0 ? headers : null;
    }
}
