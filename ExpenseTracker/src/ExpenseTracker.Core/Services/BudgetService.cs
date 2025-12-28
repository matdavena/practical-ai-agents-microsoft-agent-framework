// ============================================================================
// BudgetService
// ============================================================================
// Implementation of IBudgetService.
// Contains business logic for budget operations and alert checking.
//
// BOOK CHAPTER NOTE:
// This demonstrates:
// 1. Budget tracking with period calculations
// 2. Alert generation based on spending thresholds
// 3. Integration with expense data for real-time status
// ============================================================================

using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Services;

/// <summary>
/// Service implementation for budget business operations.
/// </summary>
public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICategoryRepository _categoryRepository;

    public BudgetService(
        IBudgetRepository budgetRepository,
        IExpenseRepository expenseRepository,
        ICategoryRepository categoryRepository)
    {
        _budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
        _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    /// <inheritdoc />
    public async Task<Budget> SetBudgetAsync(
        string userId,
        decimal amount,
        BudgetPeriod period = BudgetPeriod.Monthly,
        string? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        // Check if budget already exists for this category
        var existing = await _budgetRepository.GetByCategoryAsync(userId, categoryId, cancellationToken);

        if (existing != null)
        {
            // Update existing budget
            existing.Amount = amount;
            existing.Period = period;
            existing.IsActive = true;
            return await _budgetRepository.UpdateAsync(existing, cancellationToken);
        }

        // Create new budget
        var budget = Budget.Create(userId, amount, period, categoryId);
        return await _budgetRepository.CreateAsync(budget, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BudgetStatus>> GetBudgetStatusAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var budgets = await _budgetRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        var categories = (await _categoryRepository.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id);

        var statuses = new List<BudgetStatus>();

        foreach (var budget in budgets)
        {
            var status = await CalculateBudgetStatusAsync(budget, categories, cancellationToken);
            statuses.Add(status);
        }

        return statuses;
    }

    /// <inheritdoc />
    public async Task<BudgetStatus?> GetBudgetStatusAsync(
        string userId,
        string? categoryId,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByCategoryAsync(userId, categoryId, cancellationToken);
        if (budget == null) return null;

        var categories = (await _categoryRepository.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id);

        return await CalculateBudgetStatusAsync(budget, categories, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Budget>> GetBudgetsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _budgetRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteBudgetAsync(
        string budgetId,
        CancellationToken cancellationToken = default)
    {
        return await _budgetRepository.DeleteAsync(budgetId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteBudgetByCategoryAsync(
        string userId,
        string? categoryId,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByCategoryAsync(userId, categoryId, cancellationToken);
        if (budget == null) return false;

        return await _budgetRepository.DeleteAsync(budget.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BudgetAlert>> CheckBudgetAlertsAsync(
        string userId,
        decimal warningThreshold = 0.8m,
        CancellationToken cancellationToken = default)
    {
        var statuses = await GetBudgetStatusAsync(userId, cancellationToken);
        var alerts = new List<BudgetAlert>();

        foreach (var status in statuses)
        {
            if (status.UsagePercentage >= warningThreshold)
            {
                var (level, message) = GetAlertInfo(status);
                alerts.Add(new BudgetAlert(status, level, message));
            }
        }

        return alerts.OrderByDescending(a => a.Status.UsagePercentage);
    }

    /// <summary>
    /// Calculates the budget status including current spending.
    /// </summary>
    private async Task<BudgetStatus> CalculateBudgetStatusAsync(
        Budget budget,
        Dictionary<string, Category> categories,
        CancellationToken cancellationToken)
    {
        var (periodStart, periodEnd) = GetPeriodDates(budget.Period);

        // Get spending for this period
        decimal spent;
        if (budget.IsGlobal)
        {
            spent = await _expenseRepository.GetTotalAmountAsync(
                budget.UserId, periodStart, periodEnd, null, cancellationToken);
        }
        else
        {
            spent = await _expenseRepository.GetTotalAmountAsync(
                budget.UserId, periodStart, periodEnd, budget.CategoryId, cancellationToken);
        }

        var remaining = budget.Amount - spent;
        var percentage = budget.Amount > 0 ? spent / budget.Amount : 0;

        Category? category = null;
        if (budget.CategoryId != null)
        {
            categories.TryGetValue(budget.CategoryId, out category);
        }

        return new BudgetStatus(
            BudgetId: budget.Id,
            CategoryId: budget.CategoryId,
            CategoryName: budget.IsGlobal ? "Global" : (category?.Name ?? budget.CategoryId),
            CategoryIcon: budget.IsGlobal ? "💰" : (category?.Icon ?? "📦"),
            BudgetAmount: budget.Amount,
            Period: budget.Period,
            SpentAmount: spent,
            RemainingAmount: remaining,
            UsagePercentage: percentage,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            IsOverBudget: spent > budget.Amount,
            IsWarning: percentage >= 0.8m && percentage < 1.0m);
    }

    /// <summary>
    /// Gets the start and end dates for a budget period.
    /// </summary>
    private static (DateTime Start, DateTime End) GetPeriodDates(BudgetPeriod period)
    {
        var today = DateTime.Today;

        return period switch
        {
            BudgetPeriod.Weekly => GetWeekDates(today),
            BudgetPeriod.Monthly => GetMonthDates(today),
            BudgetPeriod.Yearly => GetYearDates(today),
            _ => GetMonthDates(today)
        };
    }

    private static (DateTime Start, DateTime End) GetWeekDates(DateTime date)
    {
        // Week starts on Monday
        var daysFromMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        var weekStart = date.AddDays(-daysFromMonday);
        var weekEnd = weekStart.AddDays(6);
        return (weekStart, weekEnd);
    }

    private static (DateTime Start, DateTime End) GetMonthDates(DateTime date)
    {
        var monthStart = new DateTime(date.Year, date.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        return (monthStart, monthEnd);
    }

    private static (DateTime Start, DateTime End) GetYearDates(DateTime date)
    {
        var yearStart = new DateTime(date.Year, 1, 1);
        var yearEnd = new DateTime(date.Year, 12, 31);
        return (yearStart, yearEnd);
    }

    /// <summary>
    /// Determines alert level and message based on budget status.
    /// </summary>
    private static (BudgetAlertLevel Level, string Message) GetAlertInfo(BudgetStatus status)
    {
        var scopeName = status.CategoryId == null ? "global" : status.CategoryName;
        var percentText = $"{status.UsagePercentage:P0}";

        if (status.UsagePercentage >= 1.2m)
        {
            return (BudgetAlertLevel.Critical,
                $"⚠️ CRITICAL: Your {scopeName} budget is at {percentText}! You've exceeded by {Math.Abs(status.RemainingAmount):N2} EUR.");
        }

        if (status.UsagePercentage >= 1.0m)
        {
            return (BudgetAlertLevel.Exceeded,
                $"🔴 EXCEEDED: Your {scopeName} budget is at {percentText}. You've spent {Math.Abs(status.RemainingAmount):N2} EUR over budget.");
        }

        return (BudgetAlertLevel.Warning,
            $"🟡 WARNING: Your {scopeName} budget is at {percentText}. Only {status.RemainingAmount:N2} EUR remaining.");
    }
}
