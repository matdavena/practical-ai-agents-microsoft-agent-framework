// ============================================================================
// Expense Entity
// ============================================================================
// Represents a single expense record.
// Core entity of the expense tracker containing amount, category, date,
// and metadata about how the expense was recorded.
// ============================================================================

namespace ExpenseTracker.Core.Domain.Entities;

/// <summary>
/// Represents a single expense record.
/// </summary>
public class Expense
{
    /// <summary>
    /// Unique identifier for the expense (GUID).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The user who owns this expense.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The amount spent in EUR.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Description of the expense (e.g., "Spesa al supermercato").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category ID for grouping expenses.
    /// </summary>
    public string CategoryId { get; set; } = "other";

    /// <summary>
    /// When the expense occurred.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Where the expense occurred (store name, location).
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Additional notes or details.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// How the expense was recorded.
    /// </summary>
    public ExpenseSource Source { get; set; } = ExpenseSource.Manual;

    /// <summary>
    /// When the expense record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the expense record was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Creates a new expense instance.
    /// </summary>
    public Expense() { }

    /// <summary>
    /// Creates a new expense with the specified values.
    /// </summary>
    public static Expense Create(
        string userId,
        decimal amount,
        string description,
        string categoryId = "other",
        DateTime? date = null,
        string? location = null,
        ExpenseSource source = ExpenseSource.Manual)
    {
        return new Expense
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Amount = amount,
            Description = description,
            CategoryId = categoryId,
            Date = date ?? DateTime.Today,
            Location = location,
            Source = source,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Returns a formatted string representation of the expense.
    /// </summary>
    public override string ToString() =>
        $"{Amount:C} - {Description} ({Date:d})";

    /// <summary>
    /// Returns a detailed string representation.
    /// </summary>
    public string ToDetailedString() =>
        $"""
        {Amount:N2} EUR - {Description}
        Data: {Date:d}
        Categoria: {CategoryId}
        {(Location != null ? $"Luogo: {Location}" : "")}
        Fonte: {Source}
        """;
}
