// ============================================================================
// IUserRepository Interface
// ============================================================================
// Defines the contract for user data access operations.
// ============================================================================

using ExpenseTracker.Core.Domain.Entities;

namespace ExpenseTracker.Core.Abstractions;

/// <summary>
/// Repository interface for user data operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their Telegram ID.
    /// </summary>
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last active timestamp for a user.
    /// </summary>
    Task UpdateLastActiveAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a user from Telegram information.
    /// If the user exists (by TelegramId), returns it; otherwise creates a new one.
    /// </summary>
    Task<User> GetOrCreateFromTelegramAsync(
        long telegramId,
        string firstName,
        string? lastName = null,
        string? username = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users.
    /// </summary>
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
}
