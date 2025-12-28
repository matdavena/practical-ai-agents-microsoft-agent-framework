// ============================================================================
// Budget Entity
// ============================================================================
// Represents a budget limit for a category or overall spending.
// Used to track spending against limits and trigger alerts.
// ============================================================================

namespace ExpenseTracker.Core.Domain.Entities;

/// <summary>
/// Period for budget tracking.
/// </summary>
public enum BudgetPeriod
{
    /// <summary>Weekly budget.</summary>
    Weekly = 0,

    /// <summary>Monthly budget.</summary>
    Monthly = 1,

    /// <summary>Yearly budget.</summary>
    Yearly = 2
}

/// <summary>
/// Represents a budget limit for tracking spending.
/// </summary>
public class Budget
{
    /// <summary>
    /// Unique identifier for the budget.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The user who owns this budget.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Category ID this budget applies to.
    /// Null means this is a global budget for all expenses.
    /// </summary>
    public string? CategoryId { get; set; }

    /// <summary>
    /// Budget amount limit in EUR.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The period this budget covers.
    /// </summary>
    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;

    /// <summary>
    /// Whether this budget is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the budget was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a new budget instance.
    /// </summary>
    public Budget() { }

    /// <summary>
    /// Creates a new budget with the specified values.
    /// </summary>
    public static Budget Create(
        string userId,
        decimal amount,
        BudgetPeriod period = BudgetPeriod.Monthly,
        string? categoryId = null)
    {
        return new Budget
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Amount = amount,
            Period = period,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets whether this is a global budget (applies to all categories).
    /// </summary>
    public bool IsGlobal => CategoryId == null;

    /// <summary>
    /// Returns a string representation of the budget.
    /// </summary>
    public override string ToString()
    {
        var scope = IsGlobal ? "Globale" : $"Categoria: {CategoryId}";
        return $"Budget {Period}: {Amount:N2} EUR ({scope})";
    }
}
