// ============================================================================
// IExpenseService Interface
// ============================================================================
// Defines the business logic contract for expense operations.
// Orchestrates between repositories and adds business rules.
// ============================================================================

using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Services;

/// <summary>
/// Service interface for expense business operations.
/// </summary>
public interface IExpenseService
{
    /// <summary>
    /// Adds a new expense for a user.
    /// </summary>
    Task<Expense> AddExpenseAsync(
        string userId,
        decimal amount,
        string description,
        string categoryId = "other",
        DateTime? date = null,
        string? location = null,
        ExpenseSource source = ExpenseSource.Manual,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an expense by ID.
    /// </summary>
    Task<Expense?> GetExpenseAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expenses for a user.
    /// </summary>
    Task<IEnumerable<Expense>> GetExpensesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expenses for a user in a date range.
    /// </summary>
    Task<IEnumerable<Expense>> GetExpensesAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        string? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent expenses for a user (last N days).
    /// </summary>
    Task<IEnumerable<Expense>> GetRecentExpensesAsync(
        string userId,
        int days = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing expense.
    /// </summary>
    Task<Expense> UpdateExpenseAsync(Expense expense, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an expense.
    /// </summary>
    Task<bool> DeleteExpenseAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total amount spent by a user in a period.
    /// </summary>
    Task<decimal> GetTotalSpentAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        string? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expense summary grouped by category for a period.
    /// </summary>
    Task<IEnumerable<CategorySummary>> GetCategorySummaryAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches expenses using semantic similarity.
    /// Returns expenses similar to the query text.
    /// </summary>
    /// <param name="userId">The user ID to search within.</param>
    /// <param name="query">Natural language query.</param>
    /// <param name="limit">Maximum results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching expenses ordered by relevance.</returns>
    Task<IEnumerable<SemanticSearchResult>> SemanticSearchAsync(
        string userId,
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary of expenses for a category.
/// </summary>
public record CategorySummary(
    string CategoryId,
    string CategoryName,
    string CategoryIcon,
    decimal TotalAmount,
    int ExpenseCount,
    decimal Percentage);

/// <summary>
/// Result from semantic search.
/// </summary>
public record SemanticSearchResult(
    Domain.Entities.Expense Expense,
    float Score,
    string CategoryName,
    string CategoryIcon);
