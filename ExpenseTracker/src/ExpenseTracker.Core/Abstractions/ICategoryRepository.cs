// ============================================================================
// ICategoryRepository Interface
// ============================================================================
// Defines the contract for category data access operations.
// ============================================================================

using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Abstractions;

/// <summary>
/// Repository interface for category data operations.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets all categories.
    /// </summary>
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category by its ID.
    /// </summary>
    Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new category.
    /// </summary>
    Task<Category> CreateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category by its ID.
    /// Only non-default categories can be deleted.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures default categories exist in the database.
    /// Called during application startup.
    /// </summary>
    Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken = default);
}
