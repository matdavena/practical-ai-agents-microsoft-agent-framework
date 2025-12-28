// ============================================================================
// CategoriesController
// ============================================================================
// Endpoints for expense categories.
// ============================================================================

using ExpenseTracker.Api.Models;
using ExpenseTracker.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Gets all available expense categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories(CancellationToken ct = default)
    {
        var categories = await _categoryService.GetAllCategoriesAsync(ct);

        var response = categories.Select(c => new CategoryResponse(
            Id: c.Id,
            Name: c.Name,
            Icon: c.Icon
        ));

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific category by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetCategory(string id, CancellationToken ct = default)
    {
        var category = await _categoryService.GetCategoryAsync(id, ct);

        if (category == null)
            return NotFound();

        return Ok(new CategoryResponse(
            Id: category.Id,
            Name: category.Name,
            Icon: category.Icon
        ));
    }
}
