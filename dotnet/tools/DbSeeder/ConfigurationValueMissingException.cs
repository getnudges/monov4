namespace DbSeeder;
public class ConfigurationValueMissingException : Exception {
    public ConfigurationValueMissingException(string key) : base($"Key {key} has no value") => Data.Add("Key", key);
}
