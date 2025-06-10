using Nudges.Configuration;

namespace Microsoft.Extensions.Configuration;

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
        Console.WriteLine("FileMap: {0}", fileMap);

        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in fileMap.Split(Environment.NewLine)) {
            // Trim whitespace and ignore comments or empty lines.
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#')) {
                continue;
            }

            // Split only on the first '=' character.
            var parts = trimmedLine.Split(['='], 2);
            if (parts.Length != 2) {
                // Optionally log a warning here about the malformed line.
                continue;
            }

            var key = parts[0].Trim().Replace("__", ":");
            var value = parts[1].Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value)) {
                mappings[key] = value;
                Console.WriteLine("Mapping: {0} -> {1}", key, value);
            }
        }

        return builder.AddFileSecrets(source => {
            foreach (var mapping in mappings) {
                source.FileMappings[mapping.Key] = mapping.Value;
            }
        });
    }
}
