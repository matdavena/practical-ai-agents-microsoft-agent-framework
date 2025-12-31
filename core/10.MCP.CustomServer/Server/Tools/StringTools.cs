// ============================================================================
// 10. MCP CUSTOM SERVER
// FILE: StringTools.cs
// ============================================================================
//
// MCP tools for string manipulation.
// Demonstrates how to create tools with more complex logic.
// ============================================================================

using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Tools;

/// <summary>
/// Tools for string operations.
/// </summary>
[McpServerToolType]
public sealed class StringTools
{
    // ========================================================================
    // TRANSFORMATIONS
    // ========================================================================

    [McpServerTool(Name = "reverse_string")]
    [Description("Reverses a string character by character.")]
    public static string Reverse(
        [Description("String to reverse")] string text)
    {
        var chars = text.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    [McpServerTool(Name = "to_uppercase")]
    [Description("Converts a string to uppercase.")]
    public static string ToUpperCase(
        [Description("String to convert")] string text)
    {
        return text.ToUpperInvariant();
    }

    [McpServerTool(Name = "to_lowercase")]
    [Description("Converts a string to lowercase.")]
    public static string ToLowerCase(
        [Description("String to convert")] string text)
    {
        return text.ToLowerInvariant();
    }

    [McpServerTool(Name = "to_title_case")]
    [Description("Converts a string to Title Case (first letter of each word capitalized).")]
    public static string ToTitleCase(
        [Description("String to convert")] string text)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }

    // ========================================================================
    // ANALYSIS
    // ========================================================================

    [McpServerTool(Name = "count_chars")]
    [Description("Counts characters in a string (with and without spaces).")]
    public static string CountCharacters(
        [Description("String to analyze")] string text)
    {
        var total = text.Length;
        var withoutSpaces = text.Replace(" ", "").Length;
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return $"Total characters: {total}, Without spaces: {withoutSpaces}, Words: {words}";
    }

    [McpServerTool(Name = "find_replace")]
    [Description("Searches and replaces text in a string.")]
    public static string FindAndReplace(
        [Description("Original string")] string text,
        [Description("Text to search for")] string find,
        [Description("Replacement text")] string replace)
    {
        return text.Replace(find, replace);
    }

    [McpServerTool(Name = "extract_numbers")]
    [Description("Extracts all numbers from a string.")]
    public static string ExtractNumbers(
        [Description("String to extract numbers from")] string text)
    {
        var numbers = Regex.Matches(text, @"-?\d+\.?\d*")
            .Select(m => m.Value)
            .ToList();

        if (numbers.Count == 0)
            return "No numbers found in the string.";

        return string.Join(", ", numbers);
    }

    // ========================================================================
    // FORMATTING
    // ========================================================================

    [McpServerTool(Name = "slugify")]
    [Description("Converts a string to slug format (URL-friendly).")]
    public static string Slugify(
        [Description("String to convert to slug")] string text)
    {
        // Remove accents
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString().Normalize(NormalizationForm.FormC);

        // Convert to lowercase and replace spaces with hyphens
        result = result.ToLowerInvariant();
        result = Regex.Replace(result, @"[^a-z0-9\s-]", "");
        result = Regex.Replace(result, @"\s+", "-");
        result = Regex.Replace(result, @"-+", "-");
        result = result.Trim('-');

        return result;
    }

    [McpServerTool(Name = "truncate")]
    [Description("Truncates a string to a maximum length, adding '...' if necessary.")]
    public static string Truncate(
        [Description("String to truncate")] string text,
        [Description("Maximum length")] int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text[..(maxLength - 3)] + "...";
    }
}
