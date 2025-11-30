using Nudges.Configuration;

namespace LocalizationApi;


public class Settings {
    public OtlpSettings Otlp { get; set; } = new OtlpSettings();
}
