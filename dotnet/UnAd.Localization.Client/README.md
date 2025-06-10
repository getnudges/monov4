# UnAd.Localization.Client

A lightweight .NET client library for interacting with the UnAd Localization gRPC service. This library simplifies retrieving localized strings from the localization service with minimal configuration.

## Features

- Simple, fluent API for retrieving localized strings
- Built-in support for parameter substitution
- Integration with .NET Dependency Injection
- Async-first design with cancellation token support
- Configurable connection settings

## Installation

### Package Manager Console
```
Install-Package UnAd.Localization.Client
```

### .NET CLI
```
dotnet add package UnAd.Localization.Client
```

## Basic Usage

### Register with Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using UnAd.Localization.Client;

// In your Startup.cs or Program.cs
services.AddLocalizationClient(options => {
    options.ServiceUrl = "https://your-localization-service-url";
});
```

### Inject and Use the Client

```csharp
using UnAd.Localization.Client;

public class MyService
{
    private readonly ILocalizationClient _localizationClient;

    public MyService(ILocalizationClient localizationClient)
    {
        _localizationClient = localizationClient;
    }

    public async Task DisplayWelcomeMessage(string username)
    {
        // Get a simple localized string
        string welcome = await _localizationClient.GetStringAsync("Welcome");

        // Get a localized string with parameter substitution
        string personalWelcome = await _localizationClient.GetStringAsync(
            "WelcomeUser", 
            new Dictionary<string, string> { { "username", username } }
        );

        Console.WriteLine(welcome);
        Console.WriteLine(personalWelcome);
    }
}
```

## Advanced Usage

### Manual Initialization

If you're not using dependency injection, you can manually create the client:

```csharp
var client = new LocalizationClient("https://your-localization-service-url");

// Use the client
string message = await client.GetStringAsync("ErrorMessage");
```

### With Cancellation Token

```csharp
public async Task<string> GetLocalizedStringWithTimeout(CancellationToken cancellationToken)
{
    return await _localizationClient.GetStringAsync(
        "LongOperationMessage", 
        null, 
        cancellationToken
    );
}
```

## Configuration Options

When registering the client, you can configure several options:

```csharp
services.AddLocalizationClient(options => {
    // Required: The URL of the localization gRPC service
    options.ServiceUrl = "https://localization.example.com";
    
    // Optional: Configure default timeout for requests (default: 30 seconds)
    options.Timeout = TimeSpan.FromSeconds(10);
    
    // Optional: Configure gRPC channel options
    options.ConfigureChannel = channel => {
        channel.HttpHandler = new HttpClientHandler 
        {
            ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    };
});
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

