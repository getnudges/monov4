using Microsoft.Extensions.Configuration;

namespace Nudges.Configuration;

public class FlatFileConfigurationProvider(IDictionary<string, string> fileMappings) : ConfigurationProvider {
    private readonly IDictionary<string, string> _fileMappings = fileMappings;

    public override void Load() {
        var data = new Dictionary<string, string?>();
        foreach (var mapping in _fileMappings) {
            var key = mapping.Key;
            var filePath = mapping.Value;

            if (File.Exists(filePath)) {
                var fileContent = File.ReadAllText(filePath)?.Trim();
                Console.WriteLine("File: {0} -> {1}", key, fileContent);
                data[key] = fileContent;
            }
        }

        Data = data;
    }
}

/// <summary>
/// Represents a configuration source that loads key-value pairs from one or more flat files using specified file
/// mappings.
/// </summary>
/// <remarks>Use this class to add configuration data from custom flat files to an application's configuration
/// system. Each entry in the file mappings dictionary specifies a configuration key and the corresponding file path
/// from which to load its value. This source is typically added to an IConfigurationBuilder to integrate flat file
/// configuration into the application's configuration pipeline.</remarks>
public class FlatFileConfigurationSource : IConfigurationSource {

    /// <summary>
    /// Gets or sets the collection of file path mappings used by the application.
    /// </summary>
    public IDictionary<string, string> FileMappings { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Builds a new configuration provider for reading configuration values from flat files.
    /// </summary>
    /// <remarks>The returned provider uses the file mappings defined in this instance. The supplied builder
    /// is not used to further modify the provider after creation.</remarks>
    /// <param name="builder">The configuration builder that specifies the sources and settings for the configuration provider.</param>
    /// <returns>A configuration provider that loads configuration data from the mapped flat files.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new FlatFileConfigurationProvider(FileMappings);
}
