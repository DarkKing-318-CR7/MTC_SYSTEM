using System.Globalization;
using System.Text;

namespace MTC_System.Services.Implements;

/// <summary>Creates stable, filesystem-safe readable labels for Media storage directories.</summary>
public static class MediaDirectoryNameSanitizer
{
    public const int MaximumSlugLength = 64;

    public static string Sanitize(string? name, string fallbackLabel)
    {
        var normalized = (name ?? string.Empty).Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder(Math.Min(normalized.Length, MaximumSlugLength));
        var pendingSeparator = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
                continue;

            if (character is 'đ' or 'Đ')
            {
                AppendAscii('d');
            }
            else if (character is >= 'A' and <= 'Z')
            {
                AppendAscii(character is >= 'A' and <= 'Z' ? (char)(character + ('a' - 'A')) : character);
            }
            else if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                AppendAscii(character);
            }
            else
            {
                pendingSeparator = slug.Length > 0;
            }
        }

        if (pendingSeparator && slug.Length < MaximumSlugLength) slug.Append('-');
        var result = slug.ToString().Trim('-', '.');
        if (string.IsNullOrEmpty(result))
            result = SanitizeFallback(fallbackLabel);
        if (IsWindowsReservedDeviceName(result))
            result = "safe-" + result;
        return result.Length <= MaximumSlugLength ? result : result[..MaximumSlugLength].TrimEnd('-', '.');

        void AppendAscii(char value)
        {
            if (pendingSeparator && slug.Length > 0 && slug.Length < MaximumSlugLength)
                slug.Append('-');
            pendingSeparator = false;
            if (slug.Length < MaximumSlugLength)
                slug.Append(value);
        }
    }

    private static string SanitizeFallback(string? fallbackLabel)
    {
        if (!string.IsNullOrWhiteSpace(fallbackLabel))
        {
            var normalized = fallbackLabel.Normalize(NormalizationForm.FormD);
            var fallback = new StringBuilder();
            var separator = false;
            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
                    continue;
                var lowered = character is 'đ' or 'Đ' ? 'd' :
                    character is >= 'A' and <= 'Z' ? (char)(character + ('a' - 'A')) : character;
                if (lowered is >= 'a' and <= 'z' or >= '0' and <= '9')
                {
                    if (separator && fallback.Length > 0) fallback.Append('-');
                    separator = false;
                    if (fallback.Length < MaximumSlugLength) fallback.Append(lowered);
                }
                else separator = fallback.Length > 0;
            }
            var result = fallback.ToString().Trim('-', '.');
            if (!string.IsNullOrEmpty(result)) return result;
        }
        return "unknown";
    }

    private static bool IsWindowsReservedDeviceName(string name)
    {
        var stem = name.Split('.')[0];
        return stem is "con" or "prn" or "aux" or "nul" ||
            (stem.Length == 4 && (stem.StartsWith("com", StringComparison.Ordinal) || stem.StartsWith("lpt", StringComparison.Ordinal)) && stem[3] is >= '1' and <= '9');
    }
}
