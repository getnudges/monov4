using System.Text.RegularExpressions;

namespace Nudges.Security;

public static partial class ValidationHelpers {
    [GeneratedRegex(@"\D", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NonDigit();

    /// <summary>
    /// Normalizes a phone number string to a standardized E.164-like format suitable for storage or comparison.
    /// </summary>
    /// <remarks>If the input does not include a country code, the method assumes a default country code of
    /// "+1" (United States/Canada). All non-digit characters are removed before normalization.</remarks>
    /// <param name="input">The phone number to normalize. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A normalized phone number string in E.164-like format, starting with a plus sign and country code. For example,
    /// "+18005551234".</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input"/> is null, empty, or consists only of white-space characters.</exception>
    public static string NormalizePhoneNumber(string input) {
        ArgumentException.ThrowIfNullOrEmpty(input);

        // Strip non-digits: "1 (800) 555 1234" -> "18005551234"
        var digits = NonDigit().Replace(input, "");

        // Enforce E.164ish shape; you can adjust rules if needed.
        if (digits.StartsWith("1", StringComparison.OrdinalIgnoreCase) && digits.Length == 11) {
            // US numbers beginning with 1
            return $"+{digits}";
        }

        if (!digits.StartsWith("+", StringComparison.OrdinalIgnoreCase)) {
            // If user entered "8005551234" assume US country code
            digits = $"+1{digits}";
        }

        return digits;
    }
}
