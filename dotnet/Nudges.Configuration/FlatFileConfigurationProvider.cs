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

public class FlatFileConfigurationSource : IConfigurationSource {
    public IDictionary<string, string> FileMappings { get; set; } = new Dictionary<string, string>();

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new FlatFileConfigurationProvider(FileMappings);
}
