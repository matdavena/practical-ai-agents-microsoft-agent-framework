// ============================================================================
// ExpenseTools
// ============================================================================
// AI Tools for expense management operations.
// These tools allow the AI agent to perform CRUD operations on expenses.
//
// BOOK CHAPTER NOTE:
// This demonstrates the Tools/Function Calling pattern:
// 1. Methods decorated with [Description] become AI-callable functions
// 2. The LLM decides WHEN to call these tools based on user intent
// 3. Tools return strings that the LLM uses to formulate responses
// ============================================================================

using System.ComponentModel;
using System.Text;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Core.Models;
using ExpenseTracker.Core.Services;

namespace ExpenseTracker.Core.Tools;

/// <summary>
/// Tools for expense management operations.
/// These methods are exposed to the AI agent via Function Calling.
/// </summary>
public class ExpenseTools
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly string _userId;

    /// <summary>
    /// Creates a new ExpenseTools instance.
    /// </summary>
    /// <param name="expenseService">The expense service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="userId">The current user ID.</param>
    public ExpenseTools(
        IExpenseService expenseService,
        ICategoryService categoryService,
        string userId)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _userId = userId ?? throw new ArgumentNullException(nameof(userId));
    }

    // =========================================================================
    // TOOL: AddExpense
    // =========================================================================

    [Description("Adds a new expense to the database. Use this after parsing expense information from user input.")]
    public async Task<string> AddExpense(
        [Description("The amount spent in EUR (e.g., 45.50)")]
        decimal amount,
        [Description("A brief description of the expense")]
        string description,
        [Description("The category ID: food, restaurant, transport, fuel, health, entertainment, shopping, bills, home, or other")]
        string categoryId,
        [Description("The date of the expense in yyyy-MM-dd format (use today if not specified)")]
        string? date = null,
        [Description("Where the expense occurred (optional)")]
        string? location = null)
    {
        try
        {
            var expenseDate = string.IsNullOrEmpty(date)
                ? DateTime.Today
                : DateTime.Parse(date);

            var expense = await _expenseService.AddExpenseAsync(
                userId: _userId,
                amount: amount,
                description: description,
                categoryId: categoryId,
                date: expenseDate,
                location: location,
                source: ExpenseSource.Text);

            return $"Expense added successfully! ID: {expense.Id}, Amount: {expense.Amount:N2} EUR, Category: {categoryId}, Date: {expenseDate:yyyy-MM-dd}";
        }
        catch (Exception ex)
        {
            return $"Error adding expense: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: GetRecentExpenses
    // =========================================================================

    [Description("Gets the user's recent expenses from the last 30 days. Use this when the user asks to see their expenses or spending history.")]
    public async Task<string> GetRecentExpenses(
        [Description("Maximum number of expenses to return (default: 10)")]
        int limit = 10)
    {
        try
        {
            var expenses = await _expenseService.GetRecentExpensesAsync(_userId);
            var expenseList = expenses.Take(limit).ToList();

            if (expenseList.Count == 0)
            {
                return "No expenses found in the last 30 days.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Recent expenses (last 30 days, showing {expenseList.Count}):");
            sb.AppendLine();

            decimal total = 0;
            foreach (var expense in expenseList)
            {
                sb.AppendLine($"- {expense.Date:dd/MM/yyyy}: {expense.Amount:N2} EUR - {expense.Description} [{expense.CategoryId}]");
                total += expense.Amount;
            }

            sb.AppendLine();
            sb.AppendLine($"Total: {total:N2} EUR");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error retrieving expenses: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: GetExpensesByDateRange
    // =========================================================================

    [Description("Gets expenses within a specific date range. Use this when the user asks about expenses in a specific period.")]
    public async Task<string> GetExpensesByDateRange(
        [Description("Start date in yyyy-MM-dd format")]
        string fromDate,
        [Description("End date in yyyy-MM-dd format")]
        string toDate,
        [Description("Optional category filter")]
        string? categoryId = null)
    {
        try
        {
            var from = DateTime.Parse(fromDate);
            var to = DateTime.Parse(toDate);

            var expenses = await _expenseService.GetExpensesAsync(_userId, from, to);

            if (!string.IsNullOrEmpty(categoryId))
            {
                expenses = expenses.Where(e => e.CategoryId == categoryId);
            }

            var expenseList = expenses.ToList();

            if (expenseList.Count == 0)
            {
                return $"No expenses found between {from:dd/MM/yyyy} and {to:dd/MM/yyyy}.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Expenses from {from:dd/MM/yyyy} to {to:dd/MM/yyyy}:");
            sb.AppendLine();

            decimal total = 0;
            foreach (var expense in expenseList)
            {
                sb.AppendLine($"- {expense.Date:dd/MM/yyyy}: {expense.Amount:N2} EUR - {expense.Description} [{expense.CategoryId}]");
                total += expense.Amount;
            }

            sb.AppendLine();
            sb.AppendLine($"Total: {total:N2} EUR ({expenseList.Count} expenses)");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error retrieving expenses: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: GetCategorySummary
    // =========================================================================

    [Description("Gets a summary of expenses grouped by category for the current month. Use this when the user asks about spending by category or wants a summary.")]
    public async Task<string> GetCategorySummary()
    {
        try
        {
            var today = DateTime.Today;
            var fromDate = new DateTime(today.Year, today.Month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            var summary = await _expenseService.GetCategorySummaryAsync(_userId, fromDate, toDate);
            var summaryList = summary.ToList();

            if (summaryList.Count == 0)
            {
                return "No expenses recorded this month.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Expense summary for {today:MMMM yyyy}:");
            sb.AppendLine();

            foreach (var item in summaryList)
            {
                sb.AppendLine($"- {item.CategoryIcon} {item.CategoryName}: {item.TotalAmount:N2} EUR ({item.ExpenseCount} expenses, {item.Percentage:N1}%)");
            }

            sb.AppendLine();
            sb.AppendLine($"Total: {summaryList.Sum(s => s.TotalAmount):N2} EUR");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error retrieving summary: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: GetCategories
    // =========================================================================

    [Description("Gets the list of available expense categories. Use this when the user asks about categories or needs to know valid category options.")]
    public async Task<string> GetCategories()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Available expense categories:");
            sb.AppendLine();

            foreach (var category in categories)
            {
                sb.AppendLine($"- {category.Icon} {category.Name} (ID: {category.Id})");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error retrieving categories: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: GetTotalSpending
    // =========================================================================

    [Description("Gets the total amount spent in a date range. Use this when the user asks 'how much did I spend' in a period.")]
    public async Task<string> GetTotalSpending(
        [Description("Start date in yyyy-MM-dd format")]
        string fromDate,
        [Description("End date in yyyy-MM-dd format")]
        string toDate,
        [Description("Optional category filter")]
        string? categoryId = null)
    {
        try
        {
            var from = DateTime.Parse(fromDate);
            var to = DateTime.Parse(toDate);

            var total = await _expenseService.GetTotalSpentAsync(_userId, from, to, categoryId);

            var categoryText = string.IsNullOrEmpty(categoryId)
                ? ""
                : $" in category '{categoryId}'";

            return $"Total spending from {from:dd/MM/yyyy} to {to:dd/MM/yyyy}{categoryText}: {total:N2} EUR";
        }
        catch (Exception ex)
        {
            return $"Error calculating total: {ex.Message}";
        }
    }

    // =========================================================================
    // TOOL: SearchExpenses
    // =========================================================================

    [Description("Searches expenses by description. Use this when the user wants to find specific expenses by keyword.")]
    public async Task<string> SearchExpenses(
        [Description("The keyword to search for in expense descriptions")]
        string keyword,
        [Description("Maximum number of results (default: 10)")]
        int limit = 10)
    {
        try
        {
            var expenses = await _expenseService.GetRecentExpensesAsync(_userId);
            var matches = expenses
                .Where(e => e.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .ToList();

            if (matches.Count == 0)
            {
                return $"No expenses found matching '{keyword}'.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Expenses matching '{keyword}':");
            sb.AppendLine();

            foreach (var expense in matches)
            {
                sb.AppendLine($"- {expense.Date:dd/MM/yyyy}: {expense.Amount:N2} EUR - {expense.Description} [{expense.CategoryId}]");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error searching expenses: {ex.Message}";
        }
    }
}
