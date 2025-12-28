// ============================================================================
// 10. MCP CUSTOM SERVER
// FILE: StringTools.cs
// ============================================================================
//
// Tool MCP per manipolazione di stringhe.
// Dimostra come creare tool con logica più complessa.
// ============================================================================

using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Tools;

/// <summary>
/// Tool per operazioni su stringhe.
/// </summary>
[McpServerToolType]
public sealed class StringTools
{
    // ========================================================================
    // TRASFORMAZIONI
    // ========================================================================

    [McpServerTool(Name = "reverse_string")]
    [Description("Inverte una stringa carattere per carattere.")]
    public static string Reverse(
        [Description("Stringa da invertire")] string text)
    {
        var chars = text.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    [McpServerTool(Name = "to_uppercase")]
    [Description("Converte una stringa in maiuscolo.")]
    public static string ToUpperCase(
        [Description("Stringa da convertire")] string text)
    {
        return text.ToUpperInvariant();
    }

    [McpServerTool(Name = "to_lowercase")]
    [Description("Converte una stringa in minuscolo.")]
    public static string ToLowerCase(
        [Description("Stringa da convertire")] string text)
    {
        return text.ToLowerInvariant();
    }

    [McpServerTool(Name = "to_title_case")]
    [Description("Converte una stringa in Title Case (prima lettera di ogni parola maiuscola).")]
    public static string ToTitleCase(
        [Description("Stringa da convertire")] string text)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }

    // ========================================================================
    // ANALISI
    // ========================================================================

    [McpServerTool(Name = "count_chars")]
    [Description("Conta i caratteri in una stringa (con e senza spazi).")]
    public static string CountCharacters(
        [Description("Stringa da analizzare")] string text)
    {
        var total = text.Length;
        var withoutSpaces = text.Replace(" ", "").Length;
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return $"Caratteri totali: {total}, Senza spazi: {withoutSpaces}, Parole: {words}";
    }

    [McpServerTool(Name = "find_replace")]
    [Description("Cerca e sostituisce testo in una stringa.")]
    public static string FindAndReplace(
        [Description("Stringa originale")] string text,
        [Description("Testo da cercare")] string find,
        [Description("Testo sostitutivo")] string replace)
    {
        return text.Replace(find, replace);
    }

    [McpServerTool(Name = "extract_numbers")]
    [Description("Estrae tutti i numeri da una stringa.")]
    public static string ExtractNumbers(
        [Description("Stringa da cui estrarre i numeri")] string text)
    {
        var numbers = Regex.Matches(text, @"-?\d+\.?\d*")
            .Select(m => m.Value)
            .ToList();

        if (numbers.Count == 0)
            return "Nessun numero trovato nella stringa.";

        return string.Join(", ", numbers);
    }

    // ========================================================================
    // FORMATTAZIONE
    // ========================================================================

    [McpServerTool(Name = "slugify")]
    [Description("Converte una stringa in formato slug (URL-friendly).")]
    public static string Slugify(
        [Description("Stringa da convertire in slug")] string text)
    {
        // Rimuovi accenti
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

        // Converti in minuscolo e sostituisci spazi con trattini
        result = result.ToLowerInvariant();
        result = Regex.Replace(result, @"[^a-z0-9\s-]", "");
        result = Regex.Replace(result, @"\s+", "-");
        result = Regex.Replace(result, @"-+", "-");
        result = result.Trim('-');

        return result;
    }

    [McpServerTool(Name = "truncate")]
    [Description("Tronca una stringa a una lunghezza massima, aggiungendo '...' se necessario.")]
    public static string Truncate(
        [Description("Stringa da troncare")] string text,
        [Description("Lunghezza massima")] int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text[..(maxLength - 3)] + "...";
    }
}
