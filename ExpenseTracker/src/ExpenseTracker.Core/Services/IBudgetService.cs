// ============================================================================
// IBudgetService Interface
// ============================================================================
// Defines the business logic contract for budget operations.
// Includes budget management and alert checking functionality.
// ============================================================================

using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Services;

/// <summary>
/// Service interface for budget business operations.
/// </summary>
public interface IBudgetService
{
    /// <summary>
    /// Sets or updates a budget for a user.
    /// If a budget already exists for the category, it will be updated.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="amount">The budget amount in EUR.</param>
    /// <param name="period">The budget period.</param>
    /// <param name="categoryId">Category ID, or null for global budget.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated budget.</returns>
    Task<Budget> SetBudgetAsync(
        string userId,
        decimal amount,
        BudgetPeriod period = BudgetPeriod.Monthly,
        string? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of all active budgets for a user.
    /// </summary>
    Task<IEnumerable<BudgetStatus>> GetBudgetStatusAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a specific budget.
    /// </summary>
    Task<BudgetStatus?> GetBudgetStatusAsync(
        string userId,
        string? categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all budgets for a user.
    /// </summary>
    Task<IEnumerable<Budget>> GetBudgetsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a budget.
    /// </summary>
    Task<bool> DeleteBudgetAsync(
        string budgetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a budget by category.
    /// </summary>
    Task<bool> DeleteBudgetByCategoryAsync(
        string userId,
        string? categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks all budgets and returns any that have alerts.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="warningThreshold">Percentage threshold for warning (default: 80%).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Budget statuses that exceed the warning threshold.</returns>
    Task<IEnumerable<BudgetAlert>> CheckBudgetAlertsAsync(
        string userId,
        decimal warningThreshold = 0.8m,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Status of a budget including current spending.
/// </summary>
public record BudgetStatus(
    string BudgetId,
    string? CategoryId,
    string? CategoryName,
    string? CategoryIcon,
    decimal BudgetAmount,
    BudgetPeriod Period,
    decimal SpentAmount,
    decimal RemainingAmount,
    decimal UsagePercentage,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool IsOverBudget,
    bool IsWarning);

/// <summary>
/// Alert for a budget that exceeds threshold.
/// </summary>
public record BudgetAlert(
    BudgetStatus Status,
    BudgetAlertLevel Level,
    string Message);

/// <summary>
/// Alert severity level.
/// </summary>
public enum BudgetAlertLevel
{
    /// <summary>Approaching budget limit (80-99%).</summary>
    Warning,

    /// <summary>Budget exceeded (100%+).</summary>
    Exceeded,

    /// <summary>Significantly over budget (120%+).</summary>
    Critical
}
