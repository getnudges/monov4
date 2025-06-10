using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace UnAd.Localization.Client;

/// <summary>
/// Extension methods for setting up localization client services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds the localization client to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="serverAddress">The localization service server address.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddLocalizationClient(this IServiceCollection services, string serverAddress) {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrEmpty(serverAddress)) {
            throw new ArgumentException("Server address cannot be null or empty.", nameof(serverAddress));
        }

        // Register the GrpcChannel
        services.AddSingleton(provider => GrpcChannel.ForAddress(serverAddress));

        // Register the LocalizationClient as a singleton
        services.AddSingleton<ILocalizationClient, LocalizationClient>();

        return services;
    }

    /// <summary>
    /// Adds the localization client to the specified <see cref="IServiceCollection"/> with advanced configuration options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureClient">A delegate to configure the <see cref="LocalizationClientOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddLocalizationClient(this IServiceCollection services, Action<LocalizationClientOptions> configureClient) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureClient);

        // Configure the options
        var options = new LocalizationClientOptions();
        configureClient(options);

        // Validate options
        if (string.IsNullOrEmpty(options.ServerAddress)) {
            throw new InvalidOperationException("Server address cannot be null or empty.");
        }

        // Register the GrpcChannel with configured options
        services.AddSingleton(provider => {
            var channel = GrpcChannel.ForAddress(options.ServerAddress, options.ChannelOptions);
            return channel;
        });

        // Register the LocalizationClient with proper lifetime
        switch (options.Lifetime) {
            case ServiceLifetime.Singleton:
                services.AddSingleton<ILocalizationClient, LocalizationClient>();
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<ILocalizationClient, LocalizationClient>();
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<ILocalizationClient, LocalizationClient>();
                break;
            default:
                services.AddSingleton<ILocalizationClient, LocalizationClient>();
                break;
        }

        return services;
    }

    /// <summary>
    /// Adds the localization client to the specified <see cref="IServiceCollection"/> with advanced configuration options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureClient">A delegate to configure the <see cref="LocalizationClientOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddLocalizationClient(this IServiceCollection services, Func<IServiceProvider, LocalizationClientOptions, LocalizationClientOptions> configureClient) =>
        services.AddLocalizationClient(options => configureClient(services.BuildServiceProvider(), options));
}

/// <summary>
/// Options for configuring the localization client.
/// </summary>
public class LocalizationClientOptions {
    /// <summary>
    /// The localization service server address.
    /// </summary>
    public string ServerAddress { get; set; } = "";

    /// <summary>
    /// Options for the gRPC channel.
    /// </summary>
    public GrpcChannelOptions ChannelOptions { get; set; } = new GrpcChannelOptions();

    /// <summary>
    /// The service lifetime for the client (defaults to Singleton).
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
}

