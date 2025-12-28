// ============================================================================
// BudgetRepository
// ============================================================================
// Dapper-based implementation of IBudgetRepository.
// Handles all budget data access operations.
// ============================================================================

using Dapper;
using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Infrastructure.Data;

namespace ExpenseTracker.Infrastructure.Repositories;

/// <summary>
/// SQLite/Dapper implementation of IBudgetRepository.
/// </summary>
public class BudgetRepository : IBudgetRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BudgetRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Budget>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, UserId, CategoryId, Amount, Period, IsActive, CreatedAt
            FROM Budgets
            WHERE UserId = @UserId
            ORDER BY CategoryId NULLS FIRST
            """;

        var results = await connection.QueryAsync<BudgetDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return results.Select(MapToEntity);
    }

    /// <inheritdoc />
    public async Task<Budget?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, UserId, CategoryId, Amount, Period, IsActive, CreatedAt
            FROM Budgets
            WHERE Id = @Id
            """;

        var dto = await connection.QuerySingleOrDefaultAsync<BudgetDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return dto != null ? MapToEntity(dto) : null;
    }

    /// <inheritdoc />
    public async Task<Budget?> GetByCategoryAsync(string userId, string? categoryId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = categoryId == null
            ? """
              SELECT Id, UserId, CategoryId, Amount, Period, IsActive, CreatedAt
              FROM Budgets
              WHERE UserId = @UserId AND CategoryId IS NULL AND IsActive = 1
              """
            : """
              SELECT Id, UserId, CategoryId, Amount, Period, IsActive, CreatedAt
              FROM Budgets
              WHERE UserId = @UserId AND CategoryId = @CategoryId AND IsActive = 1
              """;

        var dto = await connection.QuerySingleOrDefaultAsync<BudgetDto>(
            new CommandDefinition(sql, new { UserId = userId, CategoryId = categoryId }, cancellationToken: cancellationToken));

        return dto != null ? MapToEntity(dto) : null;
    }

    /// <inheritdoc />
    public async Task<Budget> CreateAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO Budgets (Id, UserId, CategoryId, Amount, Period, IsActive, CreatedAt)
            VALUES (@Id, @UserId, @CategoryId, @Amount, @Period, @IsActive, @CreatedAt)
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                budget.Id,
                budget.UserId,
                budget.CategoryId,
                budget.Amount,
                Period = (int)budget.Period,
                IsActive = budget.IsActive ? 1 : 0,
                CreatedAt = budget.CreatedAt.ToString("o")
            },
            cancellationToken: cancellationToken));

        return budget;
    }

    /// <inheritdoc />
    public async Task<Budget> UpdateAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE Budgets
            SET Amount = @Amount, Period = @Period, IsActive = @IsActive
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                budget.Id,
                budget.Amount,
                Period = (int)budget.Period,
                IsActive = budget.IsActive ? 1 : 0
            },
            cancellationToken: cancellationToken));

        return budget;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = "DELETE FROM Budgets WHERE Id = @Id";

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, cancellationToken: cancellationToken));

        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Budget>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, UserId, CategoryId, Amount, Period, IsActive, CreatedAt
            FROM Budgets
            WHERE UserId = @UserId AND IsActive = 1
            ORDER BY CategoryId NULLS FIRST
            """;

        var results = await connection.QueryAsync<BudgetDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return results.Select(MapToEntity);
    }

    // DTO for database mapping (SQLite returns Int64 for integers, Double for REAL)
    private record BudgetDto(
        string Id,
        string UserId,
        string? CategoryId,
        double Amount,
        long Period,
        long IsActive,
        string CreatedAt);

    private static Budget MapToEntity(BudgetDto dto) => new()
    {
        Id = dto.Id,
        UserId = dto.UserId,
        CategoryId = dto.CategoryId,
        Amount = (decimal)dto.Amount,
        Period = (BudgetPeriod)(int)dto.Period,
        IsActive = dto.IsActive == 1,
        CreatedAt = DateTime.Parse(dto.CreatedAt)
    };
}
