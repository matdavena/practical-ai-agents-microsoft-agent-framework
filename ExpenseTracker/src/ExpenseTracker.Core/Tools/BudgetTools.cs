// ============================================================================
// BudgetTools
// ============================================================================
// AI Tools for budget management operations.
// These tools allow the AI agent to manage budgets and check spending status.
//
// BOOK CHAPTER NOTE:
// This demonstrates:
// 1. Budget management via AI function calling
// 2. Real-time budget status checks
// 3. Proactive alert generation
// ============================================================================

using System.ComponentModel;
using System.Text;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Core.Services;

namespace ExpenseTracker.Core.Tools;

/// <summary>
/// Tools for budget management operations.
/// These methods are exposed to the AI agent via Function Calling.
/// </summary>
public class BudgetTools
{
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;
    private readonly string _userId;

    /// <summary>
    /// Creates a new BudgetTools instance.
    /// </summary>
    public BudgetTools(
        IBudgetService budgetService,
        ICategoryService categoryService,
        string userId)
    {
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _userId = userId ?? throw new ArgumentNullException(nameof(userId));
    }

    // =========================================================================
    // TOOL: SetBudget
    // =========================================================================

    [Description("Sets or updates a budget limit for a specific category or globally. Use this when the user wants to create or modify a budget.")]
    public async Task<string> SetBudget(
        [Description("The budget amount in EUR (e.g., 500.00)")]
        decimal amount,
        [Description("The budget period: weekly, monthly, or yearly (default: monthly)")]
        string period = "monthly",
        [Description("The category ID to set budget for. Use null or empty for a global budget across all categories.")]
        string? categoryId = null)
    {
        try
        {
            if (amount <= 0)
            {
                return "Error: Budget amount must be greater than zero.";
            }

            var budgetPeriod = period.ToLower() switch
            {
                "weekly" => BudgetPeriod.Weekly,
                "yearly" => BudgetPeriod.Yearly,
                _ => BudgetPeriod.Monthly
            };

            // Validate category if specified
            if (!string.IsNullOrEmpty(categoryId))
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                if (!categories.Any(c => c.Id == categoryId))
                {
                    return $"Error: Category '{categoryId}' not found. Use GetCategories to see valid options.";
                }
            }

            var budget = await _budgetService.SetBudgetAsync(
                _userId,
                amount,
                budgetPeriod,
                string.IsNullOrEmpty(categoryId) ? null : categoryId);

            var scopeText = budget.IsGlobal ? "global (all categories)" : $"category '{categoryId}'";
            return $"Budget set successfully! {budgetPeriod} budget of {amount:N2} EUR for {scopeText}.";
        }
        catch (Exception ex)
        {
            return $"Error setting budget: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: GetBudgetStatus
    // =========================================================================

    [Description("Gets the current status of all budgets including spending progress. Use this when the user asks about their budget status, remaining budget, or spending limits.")]
    public async Task<string> GetBudgetStatus()
    {
        try
        {
            var statuses = await _budgetService.GetBudgetStatusAsync(_userId);
            var statusList = statuses.ToList();

            if (statusList.Count == 0)
            {
                return "No budgets configured. Use SetBudget to create one.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Budget Status:");
            sb.AppendLine();

            foreach (var status in statusList)
            {
                var statusIcon = status.IsOverBudget ? "🔴" : status.IsWarning ? "🟡" : "🟢";
                var periodText = status.Period.ToString().ToLower();

                sb.AppendLine($"{statusIcon} {status.CategoryIcon} {status.CategoryName} ({periodText}):");
                sb.AppendLine($"   Budget: {status.BudgetAmount:N2} EUR");
                sb.AppendLine($"   Spent: {status.SpentAmount:N2} EUR ({status.UsagePercentage:P0})");
                sb.AppendLine($"   Remaining: {status.RemainingAmount:N2} EUR");
                sb.AppendLine($"   Period: {status.PeriodStart:dd/MM} - {status.PeriodEnd:dd/MM}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error retrieving budget status: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: GetBudgetAlerts
    // =========================================================================

    [Description("Checks for any budget alerts or warnings. Use this to proactively inform the user about budget issues or when they ask if they're within budget.")]
    public async Task<string> GetBudgetAlerts()
    {
        try
        {
            var alerts = await _budgetService.CheckBudgetAlertsAsync(_userId);
            var alertList = alerts.ToList();

            if (alertList.Count == 0)
            {
                return "All budgets are within limits. No alerts at this time.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Budget Alerts ({alertList.Count}):");
            sb.AppendLine();

            foreach (var alert in alertList)
            {
                sb.AppendLine(alert.Message);
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error checking alerts: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: DeleteBudget
    // =========================================================================

    [Description("Deletes a budget for a specific category or the global budget. Use this when the user wants to remove a budget limit.")]
    public async Task<string> DeleteBudget(
        [Description("The category ID to delete budget for. Use null or empty for the global budget.")]
        string? categoryId = null)
    {
        try
        {
            var deleted = await _budgetService.DeleteBudgetByCategoryAsync(
                _userId,
                string.IsNullOrEmpty(categoryId) ? null : categoryId);

            if (!deleted)
            {
                var scope = string.IsNullOrEmpty(categoryId) ? "global" : $"category '{categoryId}'";
                return $"No budget found for {scope}.";
            }

            var scopeText = string.IsNullOrEmpty(categoryId) ? "global budget" : $"budget for category '{categoryId}'";
            return $"Successfully deleted {scopeText}.";
        }
        catch (Exception ex)
        {
            return $"Error deleting budget: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: GetRemainingBudget
    // =========================================================================

    [Description("Gets the remaining budget for a specific category or globally. Use this when the user asks 'how much can I still spend' or similar questions.")]
    public async Task<string> GetRemainingBudget(
        [Description("The category ID to check. Use null or empty for global remaining budget.")]
        string? categoryId = null)
    {
        try
        {
            var status = await _budgetService.GetBudgetStatusAsync(
                _userId,
                string.IsNullOrEmpty(categoryId) ? null : categoryId);

            if (status == null)
            {
                var scope = string.IsNullOrEmpty(categoryId) ? "global" : $"category '{categoryId}'";
                return $"No budget configured for {scope}. Use SetBudget to create one.";
            }

            var periodText = status.Period.ToString().ToLower();

            if (status.IsOverBudget)
            {
                return $"⚠️ You've exceeded your {periodText} {status.CategoryName} budget by {Math.Abs(status.RemainingAmount):N2} EUR. " +
                       $"(Spent {status.SpentAmount:N2} EUR of {status.BudgetAmount:N2} EUR budget)";
            }

            return $"You have {status.RemainingAmount:N2} EUR remaining in your {periodText} {status.CategoryName} budget. " +
                   $"(Spent {status.SpentAmount:N2} EUR of {status.BudgetAmount:N2} EUR, {status.UsagePercentage:P0} used)";
        }
        catch (Exception ex)
        {
            return $"Error checking remaining budget: {ex.Message}";
        }
    }
}
