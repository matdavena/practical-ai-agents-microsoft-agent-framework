// ============================================================================
// UserRepository
// ============================================================================
// Dapper-based implementation of IUserRepository.
// Handles all user data access operations.
// ============================================================================

using Dapper;
using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Infrastructure.Data;

namespace ExpenseTracker.Infrastructure.Repositories;

/// <summary>
/// SQLite/Dapper implementation of IUserRepository.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, TelegramId, Name, TelegramUsername, CreatedAt, LastActiveAt
            FROM Users
            WHERE Id = @Id
            """;

        var dto = await connection.QuerySingleOrDefaultAsync<UserDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return dto != null ? MapToEntity(dto) : null;
    }

    /// <inheritdoc />
    public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, TelegramId, Name, TelegramUsername, CreatedAt, LastActiveAt
            FROM Users
            WHERE TelegramId = @TelegramId
            """;

        var dto = await connection.QuerySingleOrDefaultAsync<UserDto>(
            new CommandDefinition(sql, new { TelegramId = telegramId }, cancellationToken: cancellationToken));

        return dto != null ? MapToEntity(dto) : null;
    }

    /// <inheritdoc />
    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO Users (Id, TelegramId, Name, TelegramUsername, CreatedAt, LastActiveAt)
            VALUES (@Id, @TelegramId, @Name, @TelegramUsername, @CreatedAt, @LastActiveAt)
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                user.Id,
                user.TelegramId,
                user.Name,
                user.TelegramUsername,
                CreatedAt = user.CreatedAt.ToString("o"),
                LastActiveAt = user.LastActiveAt.ToString("o")
            },
            cancellationToken: cancellationToken));

        return user;
    }

    /// <inheritdoc />
    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE Users
            SET Name = @Name, TelegramUsername = @TelegramUsername, LastActiveAt = @LastActiveAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                user.Id,
                user.Name,
                user.TelegramUsername,
                LastActiveAt = DateTime.UtcNow.ToString("o")
            },
            cancellationToken: cancellationToken));

        return user;
    }

    /// <inheritdoc />
    public async Task UpdateLastActiveAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE Users
            SET LastActiveAt = @LastActiveAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = userId, LastActiveAt = DateTime.UtcNow.ToString("o") },
            cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<User> GetOrCreateFromTelegramAsync(
        long telegramId,
        string firstName,
        string? lastName = null,
        string? username = null,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing user
        var existingUser = await GetByTelegramIdAsync(telegramId, cancellationToken);

        if (existingUser != null)
        {
            // Update last active and return
            await UpdateLastActiveAsync(existingUser.Id, cancellationToken);
            existingUser.LastActiveAt = DateTime.UtcNow;
            return existingUser;
        }

        // Create new user
        var newUser = User.CreateFromTelegram(telegramId, firstName, lastName, username);
        return await CreateAsync(newUser, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, TelegramId, Name, TelegramUsername, CreatedAt, LastActiveAt
            FROM Users
            ORDER BY CreatedAt DESC
            """;

        var results = await connection.QueryAsync<UserDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return results.Select(MapToEntity);
    }

    // DTO for database mapping (SQLite returns Int64 for integers, strings for dates)
    private record UserDto(
        string Id,
        long? TelegramId,
        string Name,
        string? TelegramUsername,
        string CreatedAt,
        string LastActiveAt);

    private static User MapToEntity(UserDto dto) => new()
    {
        Id = dto.Id,
        TelegramId = dto.TelegramId,
        Name = dto.Name,
        TelegramUsername = dto.TelegramUsername,
        CreatedAt = DateTime.Parse(dto.CreatedAt),
        LastActiveAt = DateTime.Parse(dto.LastActiveAt)
    };
}
