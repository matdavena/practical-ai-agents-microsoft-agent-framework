// ============================================================================
// ExpenseService
// ============================================================================
// Implementation of IExpenseService.
// Contains business logic for expense operations.
// ============================================================================

using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Services;

/// <summary>
/// Service implementation for expense business operations.
/// </summary>
public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IVectorStore? _vectorStore;

    public ExpenseService(
        IExpenseRepository expenseRepository,
        ICategoryRepository categoryRepository,
        IVectorStore? vectorStore = null)
    {
        _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public async Task<Expense> AddExpenseAsync(
        string userId,
        decimal amount,
        string description,
        string categoryId = "other",
        DateTime? date = null,
        string? location = null,
        ExpenseSource source = ExpenseSource.Manual,
        CancellationToken cancellationToken = default)
    {
        // Validate category exists
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category == null)
        {
            categoryId = "other"; // Fallback to default
            category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        }

        var expense = Expense.Create(
            userId: userId,
            amount: amount,
            description: description,
            categoryId: categoryId,
            date: date,
            location: location,
            source: source);

        var savedExpense = await _expenseRepository.CreateAsync(expense, cancellationToken);

        // Index in vector store for semantic search
        await IndexExpenseAsync(savedExpense, category!, cancellationToken);

        return savedExpense;
    }

    /// <summary>
    /// Indexes an expense in the vector store for semantic search.
    /// </summary>
    private async Task IndexExpenseAsync(Expense expense, Category category, CancellationToken cancellationToken)
    {
        if (_vectorStore == null) return;

        try
        {
            // Create searchable text from expense
            var textToEmbed = $"{expense.Description} {category.Name} {expense.Location ?? ""}".Trim();

            var metadata = new ExpenseVectorMetadata(
                ExpenseId: expense.Id,
                UserId: expense.UserId,
                Description: expense.Description,
                CategoryId: expense.CategoryId,
                CategoryName: category.Name,
                Amount: expense.Amount,
                Date: expense.Date,
                Location: expense.Location);

            await _vectorStore.UpsertExpenseAsync(
                expense.Id,
                expense.UserId,
                textToEmbed,
                metadata,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Log but don't fail - vector search is optional
            Console.WriteLine($"[Warning] Failed to index expense: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Expense?> GetExpenseAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _expenseRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Expense>> GetExpensesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _expenseRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Expense>> GetExpensesAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        string? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (categoryId != null)
        {
            return await _expenseRepository.GetByCategoryAsync(userId, categoryId, fromDate, toDate, cancellationToken);
        }

        return await _expenseRepository.GetByDateRangeAsync(userId, fromDate, toDate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Expense>> GetRecentExpensesAsync(
        string userId,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var toDate = DateTime.Today;
        var fromDate = toDate.AddDays(-days);

        return await _expenseRepository.GetByDateRangeAsync(userId, fromDate, toDate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Expense> UpdateExpenseAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        return await _expenseRepository.UpdateAsync(expense, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteExpenseAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _expenseRepository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalSpentAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        string? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        return await _expenseRepository.GetTotalAmountAsync(userId, fromDate, toDate, categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CategorySummary>> GetCategorySummaryAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var expenses = await _expenseRepository.GetByDateRangeAsync(userId, fromDate, toDate, cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);

        var categoryDict = categories.ToDictionary(c => c.Id);
        var totalAmount = expenses.Sum(e => e.Amount);

        var summary = expenses
            .GroupBy(e => e.CategoryId)
            .Select(g =>
            {
                var category = categoryDict.GetValueOrDefault(g.Key);
                var groupTotal = g.Sum(e => e.Amount);

                return new CategorySummary(
                    CategoryId: g.Key,
                    CategoryName: category?.Name ?? "Sconosciuto",
                    CategoryIcon: category?.Icon ?? "❓",
                    TotalAmount: groupTotal,
                    ExpenseCount: g.Count(),
                    Percentage: totalAmount > 0 ? Math.Round(groupTotal / totalAmount * 100, 1) : 0);
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

        return summary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SemanticSearchResult>> SemanticSearchAsync(
        string userId,
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (_vectorStore == null)
        {
            return [];
        }

        try
        {
            var results = await _vectorStore.SearchAsync(userId, query, limit, 0.5f, cancellationToken);

            if (results.Count == 0)
            {
                return [];
            }

            // Get full expense details
            var expenseIds = results.Select(r => r.ExpenseId).ToList();
            var expenses = new List<SemanticSearchResult>();

            foreach (var result in results)
            {
                var expense = await _expenseRepository.GetByIdAsync(result.ExpenseId, cancellationToken);
                if (expense != null)
                {
                    expenses.Add(new SemanticSearchResult(
                        Expense: expense,
                        Score: result.Score,
                        CategoryName: result.Metadata.CategoryName,
                        CategoryIcon: "")); // Icon retrieved separately if needed
                }
            }

            return expenses;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Semantic search failed: {ex.Message}");
            return [];
        }
    }
}
