// ============================================================================
// API Models (DTOs)
// ============================================================================
// Data Transfer Objects for the Web API endpoints.
// ============================================================================

namespace ExpenseTracker.Api.Models;

// ============================================================================
// EXPENSE MODELS
// ============================================================================

/// <summary>
/// Request to create an expense from text (natural language).
/// </summary>
public record CreateExpenseFromTextRequest(
    string Text,
    string? UserId = null
);

/// <summary>
/// Request to create an expense from receipt image.
/// </summary>
public record CreateExpenseFromReceiptRequest(
    string Base64Image,
    string MimeType = "image/jpeg",
    string? UserId = null
);

/// <summary>
/// Request to create an expense manually.
/// </summary>
public record CreateExpenseRequest(
    decimal Amount,
    string Description,
    string Category,
    DateTime? Date = null,
    string? Location = null,
    string? UserId = null
);

/// <summary>
/// Response for expense operations.
/// </summary>
public record ExpenseResponse(
    string Id,
    decimal Amount,
    string Description,
    string CategoryId,
    string CategoryName,
    string CategoryIcon,
    DateTime Date,
    string? Location,
    string Source,
    DateTime CreatedAt
);

/// <summary>
/// Response for parsed expense (from text or receipt).
/// </summary>
public record ParsedExpenseResponse(
    bool Success,
    decimal? Amount,
    string? Description,
    string? Category,
    DateTime? Date,
    string? Location,
    float? Confidence,
    string? ErrorMessage
);

// ============================================================================
// CHAT MODELS
// ============================================================================

/// <summary>
/// Request for AI chat.
/// </summary>
public record ChatRequest(
    string Message,
    string? UserId = null,
    string? ConversationId = null
);

/// <summary>
/// Response from AI chat.
/// </summary>
public record ChatResponse(
    string Message,
    string ConversationId
);

// ============================================================================
// REPORT MODELS
// ============================================================================

/// <summary>
/// Request for reports with date filters.
/// </summary>
public record ReportRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? UserId = null
);

/// <summary>
/// Summary report response.
/// </summary>
public record SummaryReportResponse(
    decimal TotalAmount,
    int ExpenseCount,
    DateTime FromDate,
    DateTime ToDate,
    IEnumerable<CategorySummaryItem> ByCategory
);

/// <summary>
/// Category summary item.
/// </summary>
public record CategorySummaryItem(
    string CategoryId,
    string CategoryName,
    string CategoryIcon,
    decimal TotalAmount,
    int ExpenseCount,
    decimal Percentage
);

// ============================================================================
// CATEGORY MODELS
// ============================================================================

/// <summary>
/// Category response.
/// </summary>
public record CategoryResponse(
    string Id,
    string Name,
    string Icon
);
