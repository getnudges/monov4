using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace Nudges.Localization;
public static partial class LocalizationExtensions {
    private static readonly Regex ReplaceByNameRegex = ReplaceByName();

    public static void SetCulture(string culture) {
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;
    }

    public static string FormatWithReplacements(string localizedString, Dictionary<string, string> replacements) {
        var matches = ReplaceByNameRegex.Matches(localizedString);
        return matches.Aggregate(localizedString.Replace("\\n", "\n"),
            (current, match) => current.Replace(match.Value,
                replacements[match.Groups[1].Value] ?? match.Value));
    }

    public static string GetStringWithReplacements(this IStringLocalizer localizer, string name, Dictionary<string, string> replacements) {
        var resource = localizer.GetString(name) ?? throw new InvalidOperationException($"Resource not found: {name}");
        if (resource.ResourceNotFound) {
            throw new InvalidOperationException($"Resource not found: {name}");
        }
        return FormatWithReplacements(resource, replacements);
    }

    public static string GetStringWithReplacements<T>(this IStringLocalizer<T> localizer, string name, Dictionary<string, string> replacements) {
        var resource = localizer.GetString(name) ?? throw new InvalidOperationException($"Resource not found: {name}");
        if (resource.ResourceNotFound) {
            throw new InvalidOperationException($"Resource not found: {name}");
        }
        return FormatWithReplacements(resource, replacements);
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex ReplaceByName();
}
