// ============================================================================
// IExpenseRepository Interface
// ============================================================================
// Defines the contract for expense data access operations.
// Implemented by infrastructure layer using Dapper + SQLite.
// ============================================================================

using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Abstractions;

/// <summary>
/// Repository interface for expense data operations.
/// </summary>
public interface IExpenseRepository
{
    /// <summary>
    /// Creates a new expense record.
    /// </summary>
    Task<Expense> CreateAsync(Expense expense, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an expense by its ID.
    /// </summary>
    Task<Expense?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expenses for a user.
    /// </summary>
    Task<IEnumerable<Expense>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expenses for a user within a date range.
    /// </summary>
    Task<IEnumerable<Expense>> GetByDateRangeAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expenses for a user filtered by category.
    /// </summary>
    Task<IEnumerable<Expense>> GetByCategoryAsync(
        string userId,
        string categoryId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing expense.
    /// </summary>
    Task<Expense> UpdateAsync(Expense expense, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an expense by its ID.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total amount spent by a user in a date range.
    /// </summary>
    Task<decimal> GetTotalAmountAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        string? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expense count for a user.
    /// </summary>
    Task<int> GetCountAsync(string userId, CancellationToken cancellationToken = default);
}
