// ============================================================================
// ExpenseRepository
// ============================================================================
// Dapper-based implementation of IExpenseRepository.
// Handles all expense data access operations.
// ============================================================================

using Dapper;
using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Infrastructure.Data;

namespace ExpenseTracker.Infrastructure.Repositories;

/// <summary>
/// SQLite/Dapper implementation of IExpenseRepository.
/// </summary>
public class ExpenseRepository : IExpenseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExpenseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<Expense> CreateAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO Expenses (Id, UserId, Amount, Description, CategoryId, Date, Location, Notes, Source, CreatedAt, ModifiedAt)
            VALUES (@Id, @UserId, @Amount, @Description, @CategoryId, @Date, @Location, @Notes, @Source, @CreatedAt, @ModifiedAt)
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                expense.Id,
                expense.UserId,
                expense.Amount,
                expense.Description,
                expense.CategoryId,
                Date = expense.Date.ToString("yyyy-MM-dd"),
                expense.Location,
                expense.Notes,
                Source = (int)expense.Source,
                CreatedAt = expense.CreatedAt.ToString("o"),
                ModifiedAt = expense.ModifiedAt?.ToString("o")
            },
            cancellationToken: cancellationToken));

        return expense;
    }

    /// <inheritdoc />
    public async Task<Expense?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, UserId, Amount, Description, CategoryId, Date, Location, Notes, Source, CreatedAt, ModifiedAt
            FROM Expenses
            WHERE Id = @Id
            """;

        var dto = await connection.QuerySingleOrDefaultAsync<ExpenseDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return dto != null ? MapToEntity(dto) : null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Expense>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, UserId, Amount, Description, CategoryId, Date, Location, Notes, Source, CreatedAt, ModifiedAt
            FROM Expenses
            WHERE UserId = @UserId
            ORDER BY Date DESC, CreatedAt DESC
            """;

        var results = await connection.QueryAsync<ExpenseDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return results.Select(MapToEntity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Expense>> GetByDateRangeAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, UserId, Amount, Description, CategoryId, Date, Location, Notes, Source, CreatedAt, ModifiedAt
            FROM Expenses
            WHERE UserId = @UserId AND Date >= @FromDate AND Date <= @ToDate
            ORDER BY Date DESC, CreatedAt DESC
            """;

        var results = await connection.QueryAsync<ExpenseDto>(
            new CommandDefinition(
                sql,
                new
                {
                    UserId = userId,
                    FromDate = fromDate.ToString("yyyy-MM-dd"),
                    ToDate = toDate.ToString("yyyy-MM-dd")
                },
                cancellationToken: cancellationToken));

        return results.Select(MapToEntity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Expense>> GetByCategoryAsync(
        string userId,
        string categoryId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = """
            SELECT Id, UserId, Amount, Description, CategoryId, Date, Location, Notes, Source, CreatedAt, ModifiedAt
            FROM Expenses
            WHERE UserId = @UserId AND CategoryId = @CategoryId
            """;

        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);
        parameters.Add("CategoryId", categoryId);

        if (fromDate.HasValue)
        {
            sql += " AND Date >= @FromDate";
            parameters.Add("FromDate", fromDate.Value.ToString("yyyy-MM-dd"));
        }

        if (toDate.HasValue)
        {
            sql += " AND Date <= @ToDate";
            parameters.Add("ToDate", toDate.Value.ToString("yyyy-MM-dd"));
        }

        sql += " ORDER BY Date DESC, CreatedAt DESC";

        var results = await connection.QueryAsync<ExpenseDto>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return results.Select(MapToEntity);
    }

    /// <inheritdoc />
    public async Task<Expense> UpdateAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        expense.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE Expenses
            SET Amount = @Amount, Description = @Description, CategoryId = @CategoryId,
                Date = @Date, Location = @Location, Notes = @Notes, ModifiedAt = @ModifiedAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                expense.Id,
                expense.Amount,
                expense.Description,
                expense.CategoryId,
                Date = expense.Date.ToString("yyyy-MM-dd"),
                expense.Location,
                expense.Notes,
                ModifiedAt = expense.ModifiedAt?.ToString("o")
            },
            cancellationToken: cancellationToken));

        return expense;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = "DELETE FROM Expenses WHERE Id = @Id";

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, cancellationToken: cancellationToken));

        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalAmountAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate,
        string? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = """
            SELECT COALESCE(SUM(Amount), 0)
            FROM Expenses
            WHERE UserId = @UserId AND Date >= @FromDate AND Date <= @ToDate
            """;

        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);
        parameters.Add("FromDate", fromDate.ToString("yyyy-MM-dd"));
        parameters.Add("ToDate", toDate.ToString("yyyy-MM-dd"));

        if (categoryId != null)
        {
            sql += " AND CategoryId = @CategoryId";
            parameters.Add("CategoryId", categoryId);
        }

        return await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = "SELECT COUNT(*) FROM Expenses WHERE UserId = @UserId";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    // DTO for database mapping (SQLite returns Int64 for integers, Double for REAL)
    private record ExpenseDto(
        string Id,
        string UserId,
        double Amount,
        string Description,
        string CategoryId,
        string Date,
        string? Location,
        string? Notes,
        long Source,
        string CreatedAt,
        string? ModifiedAt);

    private static Expense MapToEntity(ExpenseDto dto) => new()
    {
        Id = dto.Id,
        UserId = dto.UserId,
        Amount = (decimal)dto.Amount,
        Description = dto.Description,
        CategoryId = dto.CategoryId,
        Date = DateTime.Parse(dto.Date),
        Location = dto.Location,
        Notes = dto.Notes,
        Source = (ExpenseSource)(int)dto.Source,
        CreatedAt = DateTime.Parse(dto.CreatedAt),
        ModifiedAt = dto.ModifiedAt != null ? DateTime.Parse(dto.ModifiedAt) : null
    };
}
