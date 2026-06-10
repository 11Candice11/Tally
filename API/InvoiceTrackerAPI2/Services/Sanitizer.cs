using System.Text.RegularExpressions;

namespace InvoiceTrackerAPI2.Services;

/// <summary>
/// Strips HTML tags and normalises whitespace on string inputs before they
/// are persisted. This is a defence-in-depth measure — the DTOs already
/// enforce length/format constraints via Data Annotations.
/// </summary>
public static partial class Sanitizer
{
    // Matches any HTML / XML tag
    [GeneratedRegex("<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    // Collapses two or more whitespace chars (including newlines) into one space
    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceRegex();

    /// <summary>Strip HTML tags and trim.</summary>
    public static string Text(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var stripped = HtmlTagRegex().Replace(value, string.Empty);
        return stripped.Trim();
    }

    /// <summary>Strip HTML, collapse internal whitespace, trim.</summary>
    public static string Name(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var stripped = HtmlTagRegex().Replace(value, string.Empty);
        return MultiSpaceRegex().Replace(stripped, " ").Trim();
    }

    /// <summary>Lowercase, strip HTML, trim.</summary>
    public static string Email(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var stripped = HtmlTagRegex().Replace(value, string.Empty);
        return stripped.Trim().ToLowerInvariant();
    }

    /// <summary>Strip HTML and trim — null-safe (returns null if input is null/empty).</summary>
    public static string? NullableText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var stripped = HtmlTagRegex().Replace(value, string.Empty);
        return stripped.Trim();
    }
}
