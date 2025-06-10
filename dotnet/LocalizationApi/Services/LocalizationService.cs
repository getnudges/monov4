using System.Globalization;
using System.Text.RegularExpressions;
using Grpc.Core;
using Microsoft.Extensions.Localization;

namespace LocalizationApi.Services;

public partial class LocalizationService(IStringLocalizer<LocalizationService> localizer,
                                         ILogger<LocalizationService> logger) : LocalizationGrpcService.LocalizationGrpcServiceBase {

    private static readonly Regex ReplaceByNameRegex = ReplaceByName();

    private readonly IStringLocalizer<LocalizationService> _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    private readonly ILogger<LocalizationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public override Task<LocalizationResponse> GetLocalizedString(LocalizationRequest request, ServerCallContext context) {
        _logger.RequestingLocalizedString(request.ResourceKey);

        SetCulture(string.IsNullOrEmpty(request.Locale) ? CultureInfo.CurrentCulture.Name : request.Locale);

        var localizedString = _localizer[request.ResourceKey];

        if (localizedString.ResourceNotFound) {
            _logger.ResourceNotFound(request.ResourceKey);
            return Task.FromResult(new LocalizationResponse {
                LocalizedString = request.ResourceKey,
                Success = false,
                ErrorMessage = $"Resource key '{request.ResourceKey}' not found."
            });
        }


        var result = FormatWithReplacements(localizedString.Value, request.Parameters.ToDictionary());

        return Task.FromResult(new LocalizationResponse {
            LocalizedString = result,
            Success = true,
            ErrorMessage = string.Empty
        });
    }

    private static string FormatWithReplacements(string localizedString, Dictionary<string, string> replacements) {
        var matches = ReplaceByNameRegex.Matches(localizedString);
        return matches.Aggregate(localizedString.Replace("\\n", "\n"),
            (current, match) => current.Replace(match.Value,
                replacements[match.Groups[1].Value] ?? match.Value));
    }
    private static void SetCulture(string culture) {
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex ReplaceByName();
}

