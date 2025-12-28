// ============================================================================
// ExpensesController
// ============================================================================
// RESTful API endpoints for expense management.
// Supports CRUD operations and AI-powered expense creation from text/receipts.
// ============================================================================

using ExpenseTracker.Api.Models;
using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Agents;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Core.Services;
using Microsoft.AspNetCore.Mvc;
using OpenAI;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly IBudgetService _budgetService;
    private readonly IExpenseRepository _expenseRepository;
    private readonly OpenAIClient _openAIClient;
    private readonly string _model;

    // Default user for API (simplified - in production use authentication)
    private const string DefaultUserId = "api-user";

    public ExpensesController(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        IExpenseRepository expenseRepository,
        OpenAIClient openAIClient,
        string model)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _budgetService = budgetService;
        _expenseRepository = expenseRepository;
        _openAIClient = openAIClient;
        _model = model;
    }

    /// <summary>
    /// Gets all expenses with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExpenseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExpenseResponse>>> GetExpenses(
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? category = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var effectiveUserId = userId ?? DefaultUserId;
        var from = fromDate ?? DateTime.Today.AddMonths(-1);
        var to = toDate ?? DateTime.Today.AddDays(1);

        var expenses = await _expenseService.GetExpensesAsync(effectiveUserId, from, to, category, ct);
        var categories = (await _categoryService.GetAllCategoriesAsync(ct)).ToDictionary(c => c.Id);

        var response = expenses.Select(e => ToExpenseResponse(e, categories));
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific expense by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseResponse>> GetExpense(string id, CancellationToken ct = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, ct);
        if (expense == null)
            return NotFound();

        var categories = (await _categoryService.GetAllCategoriesAsync(ct)).ToDictionary(c => c.Id);
        return Ok(ToExpenseResponse(expense, categories));
    }

    /// <summary>
    /// Creates an expense manually.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseResponse>> CreateExpense(
        [FromBody] CreateExpenseRequest request,
        CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            return BadRequest("Amount must be positive");

        var effectiveUserId = request.UserId ?? DefaultUserId;

        var expense = await _expenseService.AddExpenseAsync(
            effectiveUserId,
            request.Amount,
            request.Description,
            request.Category,
            request.Date ?? DateTime.Today,
            request.Location,
            ExpenseSource.Manual,
            ct);

        var categories = (await _categoryService.GetAllCategoriesAsync(ct)).ToDictionary(c => c.Id);
        var response = ToExpenseResponse(expense, categories);

        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, response);
    }

    /// <summary>
    /// Creates an expense from natural language text using AI.
    /// </summary>
    [HttpPost("from-text")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ParsedExpenseResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseResponse>> CreateFromText(
        [FromBody] CreateExpenseFromTextRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text is required");

        var effectiveUserId = request.UserId ?? DefaultUserId;

        // Use OrchestratorAgent to parse and save
        var agent = OrchestratorAgent.Create(
            _openAIClient,
            _expenseService,
            _categoryService,
            _budgetService,
            effectiveUserId,
            _model);

        var thread = agent.GetNewThread();
        var response = await agent.ProcessAsync(request.Text, thread, ct);

        // Get the latest expense to return
        var expenses = await _expenseService.GetExpensesAsync(effectiveUserId, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), null, ct);
        var latestExpense = expenses.FirstOrDefault();

        if (latestExpense != null)
        {
            var categories = (await _categoryService.GetAllCategoriesAsync(ct)).ToDictionary(c => c.Id);
            return CreatedAtAction(nameof(GetExpense), new { id = latestExpense.Id }, ToExpenseResponse(latestExpense, categories));
        }

        // Return the AI response if no expense was created
        return BadRequest(new ParsedExpenseResponse(
            Success: false,
            Amount: null,
            Description: null,
            Category: null,
            Date: null,
            Location: null,
            Confidence: null,
            ErrorMessage: response
        ));
    }

    /// <summary>
    /// Creates an expense from a receipt image using Vision AI.
    /// </summary>
    [HttpPost("from-receipt")]
    [ProducesResponseType(typeof(ParsedExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ParsedExpenseResponse>> ParseReceipt(
        [FromBody] CreateExpenseFromReceiptRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Base64Image))
            return BadRequest("Base64Image is required");

        var receiptParser = ReceiptParserAgent.Create(_openAIClient, "gpt-4o");
        var result = await receiptParser.ParseFromBase64Async(request.Base64Image, request.MimeType, "api-upload", ct);

        if (!result.Success)
        {
            return Ok(new ParsedExpenseResponse(
                Success: false,
                Amount: null,
                Description: null,
                Category: null,
                Date: null,
                Location: null,
                Confidence: null,
                ErrorMessage: result.ErrorMessage
            ));
        }

        var parsed = result.Expense!;

        return Ok(new ParsedExpenseResponse(
            Success: true,
            Amount: parsed.Amount,
            Description: parsed.Description,
            Category: parsed.Category,
            Date: parsed.ParsedDate,
            Location: parsed.Location,
            Confidence: parsed.Confidence,
            ErrorMessage: null
        ));
    }

    /// <summary>
    /// Confirms and saves a parsed receipt expense.
    /// </summary>
    [HttpPost("from-receipt/confirm")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseResponse>> ConfirmReceipt(
        [FromBody] CreateExpenseRequest request,
        CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            return BadRequest("Amount must be positive");

        var effectiveUserId = request.UserId ?? DefaultUserId;

        var expense = await _expenseService.AddExpenseAsync(
            effectiveUserId,
            request.Amount,
            request.Description,
            request.Category,
            request.Date ?? DateTime.Today,
            request.Location,
            ExpenseSource.Receipt,
            ct);

        var categories = (await _categoryService.GetAllCategoriesAsync(ct)).ToDictionary(c => c.Id);
        var response = ToExpenseResponse(expense, categories);

        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, response);
    }

    /// <summary>
    /// Deletes an expense.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpense(string id, CancellationToken ct = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, ct);
        if (expense == null)
            return NotFound();

        await _expenseRepository.DeleteAsync(id, ct);
        return NoContent();
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    private static ExpenseResponse ToExpenseResponse(Expense expense, Dictionary<string, Category> categories)
    {
        var category = categories.GetValueOrDefault(expense.CategoryId);
        return new ExpenseResponse(
            Id: expense.Id,
            Amount: expense.Amount,
            Description: expense.Description,
            CategoryId: expense.CategoryId,
            CategoryName: category?.Name ?? expense.CategoryId,
            CategoryIcon: category?.Icon ?? "",
            Date: expense.Date,
            Location: expense.Location,
            Source: expense.Source.ToString(),
            CreatedAt: expense.CreatedAt
        );
    }
}
