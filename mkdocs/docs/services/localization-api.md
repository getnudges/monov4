# LocalizationApi

LocalizationApi is a gRPC-based string localization service for translating and formatting user-facing messages, primarily for SMS communications.

## Supported Locales

- `en-US` - English (default)
- `es-ES` - Spanish

## gRPC API

```protobuf
service LocalizationService {
    rpc GetLocalizedString(LocalizationRequest) returns (LocalizationResponse);
}

message LocalizationRequest {
    string resourceKey = 1;
    string locale = 2;
    map<string, string> parameters = 3;
}
```

## Usage

Request a localized string with parameter substitution:

```csharp
var response = await client.GetLocalizedStringAsync(new LocalizationRequest {
    ResourceKey = "AnnouncementSent",
    Locale = "es-ES",
    Parameters = { { "count", "5" } }
});
// Returns: "Anuncio enviado a 5 suscriptores"
```

## Resource Keys

Strings are stored in .NET resource files (`LocalizationService.resx`, `LocalizationService.es.resx`).

Example keys:
- `AnnouncementSent` - Confirmation after sending announcement
- `ClientHelpMessage` - Help text for SMS commands
- `NotSubscriber` - Error when user isn't subscribed
- `WelcomeMessage` - Welcome SMS for new subscribers

Parameters use `{paramName}` syntax for substitution.

## Configuration

```ini
Kestrel__Endpoints__gRPC__Url=http://*:8888
Otlp__Endpoint=http://otel-collector:4317
```

## Running

```powershell
cd dotnet/LocalizationApi
dotnet run
```

Default port: `8888` (gRPC/HTTP2)

## Notes

- Built with NativeAOT for fast startup
- Includes ICU libraries for proper locale support
