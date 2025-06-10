namespace UnAd.Localization.Client;

/// <summary>
/// Interface for the Localization Client that provides access to localized strings
/// </summary>
public interface ILocalizationClient {
    /// <summary>
    /// Gets a localized string for the specified resource key
    /// </summary>
    /// <param name="resourceKey">The resource key to retrieve</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The localized string</returns>
    public Task<string> GetLocalizedStringAsync(string resourceKey, string locale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a localized string for the specified resource key with parameter substitution
    /// </summary>
    /// <param name="resourceKey">The resource key to retrieve</param>
    /// <param name="parameters">Dictionary of parameters to substitute in the localized string</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The localized string with parameters substituted</returns>
    public Task<string> GetLocalizedStringAsync(string resourceKey, string locale, IDictionary<string, string> parameters, CancellationToken cancellationToken = default);
}

