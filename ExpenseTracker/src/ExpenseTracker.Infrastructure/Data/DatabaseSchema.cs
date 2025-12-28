// ============================================================================
// DatabaseSchema
// ============================================================================
// Contains SQL statements for creating and managing the database schema.
// SQLite-specific SQL syntax.
// ============================================================================

namespace ExpenseTracker.Infrastructure.Data;

/// <summary>
/// Contains SQL schema definitions for the expense tracker database.
/// </summary>
public static class DatabaseSchema
{
    /// <summary>
    /// SQL to create the Users table.
    /// </summary>
    public const string CreateUsersTable = """
        CREATE TABLE IF NOT EXISTS Users (
            Id TEXT PRIMARY KEY NOT NULL,
            TelegramId INTEGER UNIQUE,
            Name TEXT NOT NULL,
            TelegramUsername TEXT,
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            LastActiveAt TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE INDEX IF NOT EXISTS IX_Users_TelegramId ON Users(TelegramId);
        """;

    /// <summary>
    /// SQL to create the Categories table.
    /// </summary>
    public const string CreateCategoriesTable = """
        CREATE TABLE IF NOT EXISTS Categories (
            Id TEXT PRIMARY KEY NOT NULL,
            Name TEXT NOT NULL,
            Icon TEXT NOT NULL,
            Color TEXT NOT NULL DEFAULT '#808080',
            IsDefault INTEGER NOT NULL DEFAULT 0
        );
        """;

    /// <summary>
    /// SQL to create the Expenses table.
    /// </summary>
    public const string CreateExpensesTable = """
        CREATE TABLE IF NOT EXISTS Expenses (
            Id TEXT PRIMARY KEY NOT NULL,
            UserId TEXT NOT NULL,
            Amount REAL NOT NULL,
            Description TEXT NOT NULL,
            CategoryId TEXT NOT NULL DEFAULT 'other',
            Date TEXT NOT NULL,
            Location TEXT,
            Notes TEXT,
            Source INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            ModifiedAt TEXT,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
            FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET DEFAULT
        );

        CREATE INDEX IF NOT EXISTS IX_Expenses_UserId ON Expenses(UserId);
        CREATE INDEX IF NOT EXISTS IX_Expenses_Date ON Expenses(Date);
        CREATE INDEX IF NOT EXISTS IX_Expenses_CategoryId ON Expenses(CategoryId);
        CREATE INDEX IF NOT EXISTS IX_Expenses_UserId_Date ON Expenses(UserId, Date);
        """;

    /// <summary>
    /// SQL to create the Budgets table.
    /// </summary>
    public const string CreateBudgetsTable = """
        CREATE TABLE IF NOT EXISTS Budgets (
            Id TEXT PRIMARY KEY NOT NULL,
            UserId TEXT NOT NULL,
            CategoryId TEXT,
            Amount REAL NOT NULL,
            Period INTEGER NOT NULL DEFAULT 1,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
            FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_Budgets_UserId ON Budgets(UserId);
        CREATE UNIQUE INDEX IF NOT EXISTS IX_Budgets_UserId_CategoryId ON Budgets(UserId, CategoryId)
            WHERE IsActive = 1;
        """;

    /// <summary>
    /// Gets all table creation SQL statements in the correct order.
    /// </summary>
    public static IEnumerable<string> GetAllTableCreationStatements()
    {
        yield return CreateUsersTable;
        yield return CreateCategoriesTable;
        yield return CreateExpensesTable;
        yield return CreateBudgetsTable;
    }
}
