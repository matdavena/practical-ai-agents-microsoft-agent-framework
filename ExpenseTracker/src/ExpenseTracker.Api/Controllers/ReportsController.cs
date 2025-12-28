// ============================================================================
// ReportsController
// ============================================================================
// Endpoints for expense reports and analytics.
// ============================================================================

using ExpenseTracker.Api.Models;
using ExpenseTracker.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    // Default user for API
    private const string DefaultUserId = "api-user";

    public ReportsController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Gets a summary report of expenses by category.
    /// </summary>
    /// <remarks>
    /// Returns total expenses grouped by category for the specified date range.
    /// Defaults to current month if no dates provided.
    /// </remarks>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SummaryReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SummaryReportResponse>> GetSummary(
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var effectiveUserId = userId ?? DefaultUserId;
        var today = DateTime.Today;
        var from = fromDate ?? new DateTime(today.Year, today.Month, 1);
        var to = toDate ?? from.AddMonths(1).AddDays(-1);

        var summary = await _expenseService.GetCategorySummaryAsync(effectiveUserId, from, to, ct);
        var summaryList = summary.ToList();

        var totalAmount = summaryList.Sum(s => s.TotalAmount);

        var categoryItems = summaryList.Select(s => new CategorySummaryItem(
            CategoryId: s.CategoryId,
            CategoryName: s.CategoryName,
            CategoryIcon: s.CategoryIcon,
            TotalAmount: s.TotalAmount,
            ExpenseCount: s.ExpenseCount,
            Percentage: totalAmount > 0 ? Math.Round(s.TotalAmount / totalAmount * 100, 1) : 0
        ));

        return Ok(new SummaryReportResponse(
            TotalAmount: totalAmount,
            ExpenseCount: summaryList.Sum(s => s.ExpenseCount),
            FromDate: from,
            ToDate: to,
            ByCategory: categoryItems
        ));
    }

    /// <summary>
    /// Gets monthly expense totals for trend analysis.
    /// </summary>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(IEnumerable<MonthlyTrendItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MonthlyTrendItem>>> GetMonthlyTrend(
        [FromQuery] string? userId = null,
        [FromQuery] int months = 6,
        CancellationToken ct = default)
    {
        var effectiveUserId = userId ?? DefaultUserId;
        var today = DateTime.Today;
        var results = new List<MonthlyTrendItem>();

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var summary = await _expenseService.GetCategorySummaryAsync(effectiveUserId, monthStart, monthEnd, ct);
            var total = summary.Sum(s => s.TotalAmount);
            var count = summary.Sum(s => s.ExpenseCount);

            results.Add(new MonthlyTrendItem(
                Year: monthStart.Year,
                Month: monthStart.Month,
                MonthName: monthStart.ToString("MMMM"),
                TotalAmount: total,
                ExpenseCount: count
            ));
        }

        return Ok(results);
    }
}

/// <summary>
/// Monthly trend item for reports.
/// </summary>
public record MonthlyTrendItem(
    int Year,
    int Month,
    string MonthName,
    decimal TotalAmount,
    int ExpenseCount
);
