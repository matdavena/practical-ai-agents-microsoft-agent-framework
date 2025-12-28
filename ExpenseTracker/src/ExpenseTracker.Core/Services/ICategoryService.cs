// ============================================================================
// ICategoryService Interface
// ============================================================================
// Defines the business logic contract for category operations.
// ============================================================================

using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Services;

/// <summary>
/// Service interface for category business operations.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets all available categories.
    /// </summary>
    Task<IEnumerable<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    Task<Category?> GetCategoryAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new custom category.
    /// </summary>
    Task<Category> CreateCategoryAsync(
        string name,
        string icon,
        string color = "#808080",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    Task<Category> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a custom category.
    /// Default categories cannot be deleted.
    /// </summary>
    Task<bool> DeleteCategoryAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures default categories exist in the database.
    /// </summary>
    Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a formatted list of categories for display.
    /// </summary>
    Task<string> GetCategoriesDisplayTextAsync(CancellationToken cancellationToken = default);
}
