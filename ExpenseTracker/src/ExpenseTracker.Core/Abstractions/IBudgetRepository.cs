// ============================================================================
// IBudgetRepository Interface
// ============================================================================
// Defines the contract for budget data access operations.
// ============================================================================

using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Abstractions;

/// <summary>
/// Repository interface for budget data operations.
/// </summary>
public interface IBudgetRepository
{
    /// <summary>
    /// Gets all budgets for a user.
    /// </summary>
    Task<IEnumerable<Budget>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a budget by its ID.
    /// </summary>
    Task<Budget?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the budget for a specific category (or global budget if categoryId is null).
    /// </summary>
    Task<Budget?> GetByCategoryAsync(string userId, string? categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new budget.
    /// </summary>
    Task<Budget> CreateAsync(Budget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing budget.
    /// </summary>
    Task<Budget> UpdateAsync(Budget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a budget by its ID.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active budgets for a user.
    /// </summary>
    Task<IEnumerable<Budget>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
