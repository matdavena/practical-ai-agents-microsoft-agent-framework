// ============================================================================
// CategoryService
// ============================================================================
// Implementation of ICategoryService.
// Contains business logic for category operations.
// ============================================================================

using System.Text;
using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Services;

/// <summary>
/// Service implementation for category business operations.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _categoryRepository.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Category?> GetCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _categoryRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Category> CreateCategoryAsync(
        string name,
        string icon,
        string color = "#808080",
        CancellationToken cancellationToken = default)
    {
        var id = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "");

        var category = new Category(id, name, icon, color, isDefault: false);

        return await _categoryRepository.CreateAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Category> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        return await _categoryRepository.UpdateAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _categoryRepository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await _categoryRepository.EnsureDefaultCategoriesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GetCategoriesDisplayTextAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Categorie disponibili:");
        sb.AppendLine();

        foreach (var category in categories)
        {
            sb.AppendLine($"  {category.Icon} {category.Name} ({category.Id})");
        }

        return sb.ToString();
    }
}
