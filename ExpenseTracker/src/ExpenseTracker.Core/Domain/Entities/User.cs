// ============================================================================
// User Entity
// ============================================================================
// Represents a user of the expense tracker.
// Users can be linked to Telegram accounts for bot interaction.
// ============================================================================

namespace ExpenseTracker.Core.Domain.Entities;

/// <summary>
/// Represents a user of the expense tracker.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user (GUID).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Telegram user ID for bot integration.
    /// Null if the user is not connected to Telegram.
    /// </summary>
    public long? TelegramId { get; set; }

    /// <summary>
    /// Display name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Telegram username (without @).
    /// </summary>
    public string? TelegramUsername { get; set; }

    /// <summary>
    /// When the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user was last active.
    /// </summary>
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a new user instance.
    /// </summary>
    public User() { }

    /// <summary>
    /// Creates a new user with a generated ID.
    /// </summary>
    public static User Create(string name, long? telegramId = null, string? telegramUsername = null)
    {
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            TelegramId = telegramId,
            TelegramUsername = telegramUsername,
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a user from Telegram information.
    /// </summary>
    public static User CreateFromTelegram(long telegramId, string firstName, string? lastName = null, string? username = null)
    {
        var name = string.IsNullOrEmpty(lastName)
            ? firstName
            : $"{firstName} {lastName}";

        return new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            TelegramId = telegramId,
            TelegramUsername = username,
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Returns a string representation of the user.
    /// </summary>
    public override string ToString() =>
        TelegramUsername != null ? $"{Name} (@{TelegramUsername})" : Name;
}
