using Grpc.Net.Client;
using LocalizationApi;

namespace UnAd.Localization.Client;

/// <summary>
/// Client for the Localization gRPC service.
/// Provides a simple interface for retrieving localized strings.
/// </summary>
public class LocalizationClient : ILocalizationClient, IDisposable {
    private readonly GrpcChannel _channel;
    private readonly LocalizationGrpcService.LocalizationGrpcServiceClient _client;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationClient"/> class with the specified address.
    /// </summary>
    /// <param name="address">The address of the gRPC service.</param>
    public LocalizationClient(string address) {
        ArgumentException.ThrowIfNullOrEmpty(address);

        _channel = GrpcChannel.ForAddress(address);
        _client = new LocalizationGrpcService.LocalizationGrpcServiceClient(_channel);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationClient"/> class with the specified channel.
    /// </summary>
    /// <param name="channel">The gRPC channel to use.</param>
    public LocalizationClient(GrpcChannel channel) {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _client = new LocalizationGrpcService.LocalizationGrpcServiceClient(_channel);
    }

    /// <summary>
    /// Gets a localized string for the specified resource key.
    /// </summary>
    /// <param name="resourceKey">The resource key to look up.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The localized string.</returns>
    public async Task<string> GetLocalizedStringAsync(string resourceKey, string locale, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(resourceKey);

        var request = new LocalizationRequest {
            ResourceKey = resourceKey,
            Locale = locale,
        };

        var response = await _client.GetLocalizedStringAsync(request, cancellationToken: cancellationToken);
        return response.LocalizedString;
    }

    /// <summary>
    /// Gets a localized string for the specified resource key with parameter replacements.
    /// </summary>
    /// <param name="resourceKey">The resource key to look up.</param>
    /// <param name="parameters">Dictionary of parameter names and values to replace in the localized string.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The localized string with parameters replaced.</returns>
    public async Task<string> GetLocalizedStringAsync(string resourceKey, string locale, IDictionary<string, string> parameters, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(resourceKey);

        var request = new LocalizationRequest {
            ResourceKey = resourceKey,
            Locale = locale,
        };

        if (parameters is not null) {
            foreach (var param in parameters) {
                request.Parameters.Add(param.Key, param.Value);
            }
        }

        var response = await _client.GetLocalizedStringAsync(request, cancellationToken: cancellationToken);
        return response.LocalizedString;
    }

    /// <summary>
    /// Disposes the resources used by the client.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by the client.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
    protected virtual void Dispose(bool disposing) {
        if (_disposed) {
            return;
        }

        if (disposing) {
            _channel?.Dispose();
        }

        _disposed = true;
    }
}

