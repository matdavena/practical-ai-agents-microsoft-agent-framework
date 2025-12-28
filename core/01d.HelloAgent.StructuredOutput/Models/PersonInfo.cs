// ============================================================================
// PERSON INFO - Basic structured output model
// ============================================================================
// Demonstrates simple property extraction from unstructured text.
// The LLM extracts name, age, occupation, and location from any text.
// ============================================================================

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HelloAgent.StructuredOutput.Models;

/// <summary>
/// Represents information about a person extracted from text.
/// The [Description] attribute helps the LLM understand the expected data.
/// The [JsonPropertyName] attribute ensures consistent JSON serialization.
/// </summary>
[Description("Information about a person including their name, age, occupation, and location")]
public class PersonInfo
{
    [JsonPropertyName("name")]
    [Description("The person's full name")]
    public string? Name { get; set; }

    [JsonPropertyName("age")]
    [Description("The person's age in years")]
    public int? Age { get; set; }

    [JsonPropertyName("occupation")]
    [Description("The person's job or profession")]
    public string? Occupation { get; set; }

    [JsonPropertyName("location")]
    [Description("Where the person lives or works")]
    public string? Location { get; set; }

    [JsonPropertyName("skills")]
    [Description("List of skills or expertise areas")]
    public string[]? Skills { get; set; }
}
