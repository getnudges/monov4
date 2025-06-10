using System.Text.Json.Serialization.Metadata;
using Grpc.Net.Client;
using Precision.WarpCache.Grpc.Client;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up cache client services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds the cache client to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="serverAddress">The cache service server address.</param>
    /// <param name="jsonTypeInfo">The JSON type information for the cache value type.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddWarpCacheClient<TValue>(this IServiceCollection services, string serverAddress, JsonTypeInfo<TValue> jsonTypeInfo) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        if (string.IsNullOrEmpty(serverAddress)) {
            throw new ArgumentException("Server address cannot be null or empty.", nameof(serverAddress));
        }

        services.AddKeyedSingleton(serverAddress, (s, _) => GrpcChannel.ForAddress(serverAddress));

        services.AddSingleton<ICacheClient<TValue>>(s =>
            new CacheClient<TValue>(
                s.GetKeyedService<GrpcChannel>(serverAddress)!,
                jsonTypeInfo));

        return services;
    }
}
