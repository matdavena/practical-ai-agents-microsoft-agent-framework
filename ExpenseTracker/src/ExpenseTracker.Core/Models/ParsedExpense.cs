// ============================================================================
// ParsedExpense Model
// ============================================================================
// Structured output model for AI expense parsing.
// Used with RunAsync<ParsedExpense>() for guaranteed typed responses.
//
// BOOK CHAPTER NOTE:
// This model demonstrates the Structured Output pattern in Microsoft Agent
// Framework. The [Description] attributes help the LLM understand what each
// field represents, improving parsing accuracy.
// ============================================================================

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ExpenseTracker.Core.Models;

/// <summary>
/// Represents an expense parsed from natural language or receipt image.
/// Used as structured output from the AI agent.
/// </summary>
[Description("An expense parsed from user input, containing amount, description, category, date, and optional location")]
public class ParsedExpense
{
    /// <summary>
    /// The amount spent in EUR.
    /// </summary>
    [JsonPropertyName("amount")]
    [Description("The amount spent in EUR (numeric value, e.g., 45.50)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Brief description of the expense.
    /// </summary>
    [JsonPropertyName("description")]
    [Description("A brief description of the expense (e.g., 'Spesa al supermercato', 'Cena al ristorante')")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category ID that best matches this expense.
    /// Must be one of: food, restaurant, transport, fuel, health, entertainment, shopping, bills, home, other
    /// </summary>
    [JsonPropertyName("category")]
    [Description("The category ID that best matches this expense. Must be one of: food, restaurant, transport, fuel, health, entertainment, shopping, bills, home, other")]
    public string Category { get; set; } = "other";

    /// <summary>
    /// The date of the expense in ISO format (yyyy-MM-dd).
    /// </summary>
    [JsonPropertyName("date")]
    [Description("The date of the expense in ISO format (yyyy-MM-dd). Use today's date if not specified.")]
    public string Date { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

    /// <summary>
    /// Where the expense occurred (store name, location).
    /// </summary>
    [JsonPropertyName("location")]
    [Description("Where the expense occurred (store name, city, or location). Can be null if not mentioned.")]
    public string? Location { get; set; }

    /// <summary>
    /// Confidence score from 0 to 1 indicating how confident the AI is about the parsing.
    /// </summary>
    [JsonPropertyName("confidence")]
    [Description("Confidence score from 0.0 to 1.0 indicating how certain the parsing is. Use lower values if information is ambiguous or incomplete.")]
    public float Confidence { get; set; } = 1.0f;

    /// <summary>
    /// Any notes or clarifications about the parsing.
    /// </summary>
    [JsonPropertyName("notes")]
    [Description("Optional notes about the parsing, especially if some information was inferred or is uncertain")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets the parsed date as DateTime.
    /// </summary>
    [JsonIgnore]
    public DateTime ParsedDate => DateTime.TryParse(Date, out var date) ? date : DateTime.Today;

    /// <summary>
    /// Returns a formatted string representation.
    /// </summary>
    public override string ToString() =>
        $"{Amount:N2} EUR - {Description} [{Category}] ({Date})";

    /// <summary>
    /// Returns a detailed multi-line representation.
    /// </summary>
    public string ToDetailedString() =>
        $"""
        Importo: {Amount:N2} EUR
        Descrizione: {Description}
        Categoria: {Category}
        Data: {Date}
        {(Location != null ? $"Luogo: {Location}" : "")}
        Confidence: {Confidence:P0}
        {(Notes != null ? $"Note: {Notes}" : "")}
        """;
}

/// <summary>
/// Result of parsing an expense, including the parsed data and metadata.
/// </summary>
public class ExpenseParseResult
{
    /// <summary>
    /// Whether the parsing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The parsed expense data.
    /// </summary>
    public ParsedExpense? Expense { get; set; }

    /// <summary>
    /// Error message if parsing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The original input text.
    /// </summary>
    public string OriginalInput { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ExpenseParseResult Ok(ParsedExpense expense, string originalInput) =>
        new() { Success = true, Expense = expense, OriginalInput = originalInput };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ExpenseParseResult Fail(string error, string originalInput) =>
        new() { Success = false, ErrorMessage = error, OriginalInput = originalInput };
}
