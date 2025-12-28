// ============================================================================
// RECIPE - Complex structured output with nested objects and arrays
// ============================================================================
// Demonstrates how to work with nested structures, arrays, and enums.
// The LLM generates complete recipes with ingredients and steps.
// ============================================================================

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HelloAgent.StructuredOutput.Models;

/// <summary>
/// Difficulty level for a recipe.
/// Enums are converted to string values in JSON Schema.
/// </summary>
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Expert
}

/// <summary>
/// Represents an ingredient with quantity and unit.
/// </summary>
[Description("An ingredient needed for the recipe")]
public class Ingredient
{
    [JsonPropertyName("name")]
    [Description("Name of the ingredient")]
    public required string Name { get; set; }

    [JsonPropertyName("quantity")]
    [Description("Amount needed (e.g., '2', '1/2', '200')")]
    public required string Quantity { get; set; }

    [JsonPropertyName("unit")]
    [Description("Unit of measurement (e.g., 'cups', 'grams', 'tablespoons')")]
    public required string Unit { get; set; }
}

/// <summary>
/// Represents a cooking step with time estimate.
/// </summary>
[Description("A single step in the recipe")]
public class CookingStep
{
    [JsonPropertyName("stepNumber")]
    [Description("The order of this step")]
    public int StepNumber { get; set; }

    [JsonPropertyName("instruction")]
    [Description("What to do in this step")]
    public required string Instruction { get; set; }

    [JsonPropertyName("durationMinutes")]
    [Description("Estimated time for this step in minutes")]
    public int? DurationMinutes { get; set; }
}

/// <summary>
/// Complete recipe with all details.
/// </summary>
[Description("A complete recipe with ingredients and instructions")]
public class Recipe
{
    [JsonPropertyName("name")]
    [Description("Name of the dish")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    [Description("Brief description of the dish")]
    public required string Description { get; set; }

    [JsonPropertyName("cuisine")]
    [Description("Type of cuisine (e.g., Italian, Mexican, Japanese)")]
    public required string Cuisine { get; set; }

    [JsonPropertyName("servings")]
    [Description("Number of servings this recipe makes")]
    public int Servings { get; set; }

    [JsonPropertyName("prepTimeMinutes")]
    [Description("Preparation time in minutes")]
    public int PrepTimeMinutes { get; set; }

    [JsonPropertyName("cookTimeMinutes")]
    [Description("Cooking time in minutes")]
    public int CookTimeMinutes { get; set; }

    [JsonPropertyName("difficulty")]
    [Description("Difficulty level of the recipe")]
    public DifficultyLevel Difficulty { get; set; }

    [JsonPropertyName("ingredients")]
    [Description("List of ingredients needed")]
    public required Ingredient[] Ingredients { get; set; }

    [JsonPropertyName("steps")]
    [Description("Step-by-step cooking instructions")]
    public required CookingStep[] Steps { get; set; }

    [JsonPropertyName("tips")]
    [Description("Optional cooking tips")]
    public string[]? Tips { get; set; }
}
