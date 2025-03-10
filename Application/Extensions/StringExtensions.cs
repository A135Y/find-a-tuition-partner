using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Application.Constants;

namespace Application.Extensions;

public static class StringExtensions
{
    [return: NotNullIfNotNull("value")]
    public static string? ToSeoUrl(this string? value)
    {
        var seo = value?
            .RegexReplace(StringConstants.CamelCaseBoundaries, " $1")
            .Trim()
            .RegexReplace(StringConstants.SpacesAndUnderscore, "-")
            .RegexReplace(StringConstants.UrlUnsafeCharacters, "")
            .RegexReplace(StringConstants.MultipleUnderscores, "-")
            .ToLower();

        return seo;
    }

    public static string ToYesNoString(this bool value)
            => value ? "Yes" : "No";

    public static string[] SplitByLineBreaks(this string value)
        => value.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

    public static bool TryParse<TEnum>(this string displayName, out TEnum resultInputType)
        where TEnum : struct, Enum
    {
        foreach (TEnum enumLoop in Enum.GetValues(typeof(TEnum)))
        {
            var displayNameLoop = enumLoop.DisplayName();
            if (displayNameLoop.Equals(displayName, StringComparison.InvariantCultureIgnoreCase))
            {
                resultInputType = enumLoop;
                return true;
            }
        }
        resultInputType = default;
        return false;
    }
    private static string RegexReplace(this string value, string pattern, string replacement)
        => Regex.Replace(
            value, pattern, replacement,
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static string DisplayList(this IEnumerable<string>? strings)
    {
        if (strings == null) return string.Empty;

        var commaSeparated = string.Join(", ", strings);

        var lastCommaIndex = commaSeparated.LastIndexOf(",", StringComparison.Ordinal);

        if (lastCommaIndex == -1) return commaSeparated;

        return commaSeparated.Remove(lastCommaIndex, 1).Insert(lastCommaIndex, " and");
    }
}