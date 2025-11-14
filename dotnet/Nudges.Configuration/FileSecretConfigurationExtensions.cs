using Nudges.Configuration;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Provides extension methods for adding file-based secret providers to an IConfigurationBuilder instance.
/// </summary>
/// <remarks>These extension methods enable the configuration system to load secrets from files using either a
/// hard-coded mapping or a .env-style mapping file. They are intended to simplify the integration of file-based secrets
/// into application configuration pipelines.</remarks>
public static class FileSecretConfigurationExtensions {
    /// <summary>
    /// Adds the file secrets provider using a hard-coded mapping.
    /// </summary>
    public static IConfigurationBuilder AddFileSecrets(this IConfigurationBuilder builder, Action<FlatFileConfigurationSource> configureSource) {
        var source = new FlatFileConfigurationSource();
        configureSource(source);
        builder.Add(source);
        return builder;
    }

    /// <summary>
    /// Adds the file secrets provider by reading a .env-style map where each line is in the format: KEY=filepath.
    /// </summary>
    public static IConfigurationBuilder AddFlatFilesFromMap(this IConfigurationBuilder builder, string fileMap, bool optional = true) {
        if (optional || string.IsNullOrEmpty(fileMap)) {
            return builder;
        }

        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in fileMap.Split(Environment.NewLine)) {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#')) {
                continue;
            }

            var parts = trimmedLine.Split(['='], 2);
            if (parts.Length != 2) {
                continue;
            }

            var key = parts[0].Trim().Replace("__", ":");
            var value = parts[1].Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value)) {
                mappings[key] = value;
            }
        }

        return builder.AddFileSecrets(source => {
            foreach (var mapping in mappings) {
                source.FileMappings[mapping.Key] = mapping.Value;
            }
        });
    }
}
