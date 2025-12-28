// ============================================================================
// Category Entity
// ============================================================================
// Represents an expense category (e.g., Food, Transport, Entertainment).
// Categories are used to classify and group expenses for reporting.
// ============================================================================

namespace ExpenseTracker.Core.Domain.Entities;

/// <summary>
/// Represents an expense category.
/// </summary>
public class Category
{
    /// <summary>
    /// Unique identifier for the category.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the category (e.g., "Alimentari", "Ristorante").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Emoji icon for visual representation.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Hex color code for UI display (e.g., "#FF5733").
    /// </summary>
    public string Color { get; set; } = "#808080";

    /// <summary>
    /// Whether this is a default system category.
    /// Default categories cannot be deleted.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Creates a new category instance.
    /// </summary>
    public Category() { }

    /// <summary>
    /// Creates a new category with the specified values.
    /// </summary>
    public Category(string id, string name, string icon, string color = "#808080", bool isDefault = true)
    {
        Id = id;
        Name = name;
        Icon = icon;
        Color = color;
        IsDefault = isDefault;
    }

    /// <summary>
    /// Returns a string representation of the category.
    /// </summary>
    public override string ToString() => $"{Icon} {Name}";

    /// <summary>
    /// Gets the default categories for the expense tracker.
    /// </summary>
    public static IEnumerable<Category> GetDefaults() =>
    [
        new("food", "Alimentari", "🛒", "#4CAF50"),
        new("restaurant", "Ristorante", "🍽️", "#FF9800"),
        new("transport", "Trasporti", "🚗", "#2196F3"),
        new("fuel", "Carburante", "⛽", "#607D8B"),
        new("health", "Salute", "💊", "#E91E63"),
        new("entertainment", "Intrattenimento", "🎬", "#9C27B0"),
        new("shopping", "Shopping", "🛍️", "#00BCD4"),
        new("bills", "Bollette", "📄", "#795548"),
        new("home", "Casa", "🏠", "#3F51B5"),
        new("other", "Altro", "📦", "#9E9E9E")
    ];
}
