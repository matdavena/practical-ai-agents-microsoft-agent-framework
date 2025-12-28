// ============================================================================
// DatabaseInitializer
// ============================================================================
// Handles database initialization, schema creation, and seeding.
// Called during application startup to ensure database is ready.
// ============================================================================

using Dapper;
using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Infrastructure.Data;

/// <summary>
/// Initializes the database schema and seeds default data.
/// </summary>
public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates a new database initializer.
    /// </summary>
    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// Initializes the database by creating tables and seeding default data.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await CreateTablesAsync(cancellationToken);
        await SeedDefaultCategoriesAsync(cancellationToken);
    }

    /// <summary>
    /// Creates all database tables if they don't exist.
    /// </summary>
    private async Task CreateTablesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        foreach (var sql in DatabaseSchema.GetAllTableCreationStatements())
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        }
    }

    /// <summary>
    /// Seeds default categories into the database.
    /// </summary>
    private async Task SeedDefaultCategoriesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Check if categories already exist
        var existingCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM Categories", cancellationToken: cancellationToken));

        if (existingCount > 0)
        {
            return; // Categories already seeded
        }

        // Insert default categories
        const string insertSql = """
            INSERT INTO Categories (Id, Name, Icon, Color, IsDefault)
            VALUES (@Id, @Name, @Icon, @Color, @IsDefault)
            """;

        foreach (var category in Category.GetDefaults())
        {
            await connection.ExecuteAsync(new CommandDefinition(
                insertSql,
                new
                {
                    category.Id,
                    category.Name,
                    category.Icon,
                    category.Color,
                    IsDefault = category.IsDefault ? 1 : 0
                },
                cancellationToken: cancellationToken));
        }
    }

    /// <summary>
    /// Drops all tables (useful for testing or reset).
    /// </summary>
    public async Task DropAllTablesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var dropStatements = new[]
        {
            "DROP TABLE IF EXISTS Budgets",
            "DROP TABLE IF EXISTS Expenses",
            "DROP TABLE IF EXISTS Categories",
            "DROP TABLE IF EXISTS Users"
        };

        foreach (var sql in dropStatements)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        }
    }

    /// <summary>
    /// Resets the database by dropping all tables and reinitializing.
    /// </summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await DropAllTablesAsync(cancellationToken);
        await InitializeAsync(cancellationToken);
    }
}
