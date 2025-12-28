// ============================================================================
// ExpenseSource Enum
// ============================================================================
// Indicates how an expense was recorded (manual entry, natural language,
// or receipt image).
// ============================================================================

namespace ExpenseTracker.Core.Domain.Entities;

/// <summary>
/// Indicates the source/method used to record an expense.
/// </summary>
public enum ExpenseSource
{
    /// <summary>
    /// Manually entered via form or direct input.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Parsed from natural language text (e.g., "Ho speso 45€ al supermercato").
    /// </summary>
    Text = 1,

    /// <summary>
    /// Extracted from a receipt image via AI vision.
    /// </summary>
    Receipt = 2,

    /// <summary>
    /// Imported from external source (bank, CSV, etc.).
    /// </summary>
    Import = 3
}
