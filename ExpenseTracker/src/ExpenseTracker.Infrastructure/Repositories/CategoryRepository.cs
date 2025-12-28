// ============================================================================
// CategoryRepository
// ============================================================================
// Dapper-based implementation of ICategoryRepository.
// Handles all category data access operations.
//
// DESIGN NOTE:
// This repository uses a private DTO (CategoryDto) that matches SQLite's type
// system (long for integers, string for dates), then maps to the domain entity
// with proper business types (bool, DateTime). See README-DataAccess.md for
// the rationale behind this pattern.
// ============================================================================

using Dapper;
using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Infrastructure.Data;

namespace ExpenseTracker.Infrastructure.Repositories;

/// <summary>
/// SQLite/Dapper implementation of ICategoryRepository.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, Name, Icon, Color, IsDefault
            FROM Categories
            ORDER BY IsDefault DESC, Name
            """;

        var results = await connection.QueryAsync<CategoryDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return results.Select(MapToEntity);
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, Name, Icon, Color, IsDefault
            FROM Categories
            WHERE Id = @Id
            """;

        var result = await connection.QuerySingleOrDefaultAsync<CategoryDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return result != null ? MapToEntity(result) : null;
    }

    /// <inheritdoc />
    public async Task<Category> CreateAsync(Category category, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO Categories (Id, Name, Icon, Color, IsDefault)
            VALUES (@Id, @Name, @Icon, @Color, @IsDefault)
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                category.Id,
                category.Name,
                category.Icon,
                category.Color,
                IsDefault = category.IsDefault ? 1 : 0
            },
            cancellationToken: cancellationToken));

        return category;
    }

    /// <inheritdoc />
    public async Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE Categories
            SET Name = @Name, Icon = @Icon, Color = @Color
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { category.Id, category.Name, category.Icon, category.Color },
            cancellationToken: cancellationToken));

        return category;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Don't delete default categories
        const string sql = """
            DELETE FROM Categories
            WHERE Id = @Id AND IsDefault = 0
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, cancellationToken: cancellationToken));

        return affected > 0;
    }

    /// <inheritdoc />
    public async Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        foreach (var category in Category.GetDefaults())
        {
            const string sql = """
                INSERT OR IGNORE INTO Categories (Id, Name, Icon, Color, IsDefault)
                VALUES (@Id, @Name, @Icon, @Color, @IsDefault)
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    category.Id,
                    category.Name,
                    category.Icon,
                    category.Color,
                    IsDefault = 1
                },
                cancellationToken: cancellationToken));
        }
    }

    // DTO for SQLite integer boolean mapping (SQLite returns Int64)
    private record CategoryDto(string Id, string Name, string Icon, string Color, long IsDefault);

    private static Category MapToEntity(CategoryDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Icon = dto.Icon,
        Color = dto.Color,
        IsDefault = dto.IsDefault == 1
    };
}
