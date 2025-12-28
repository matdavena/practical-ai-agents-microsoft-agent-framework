// ============================================================================
// SENTIMENT ANALYSIS - Structured output for text analysis
// ============================================================================
// Demonstrates how to use structured output for NLP tasks.
// The LLM analyzes text and returns structured sentiment data.
// ============================================================================

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HelloAgent.StructuredOutput.Models;

/// <summary>
/// Overall sentiment classification.
/// </summary>
public enum Sentiment
{
    VeryNegative,
    Negative,
    Neutral,
    Positive,
    VeryPositive
}

/// <summary>
/// Named entity extracted from text.
/// </summary>
[Description("An entity mentioned in the text")]
public class NamedEntity
{
    [JsonPropertyName("text")]
    [Description("The entity as it appears in the text")]
    public required string Text { get; set; }

    [JsonPropertyName("type")]
    [Description("Type of entity (Person, Organization, Location, Product, Date, etc.)")]
    public required string Type { get; set; }
}

/// <summary>
/// Complete sentiment analysis result.
/// </summary>
[Description("Sentiment analysis result for a piece of text")]
public class SentimentAnalysis
{
    [JsonPropertyName("sentiment")]
    [Description("Overall sentiment of the text")]
    public Sentiment Sentiment { get; set; }

    [JsonPropertyName("confidenceScore")]
    [Description("Confidence score from 0.0 to 1.0")]
    public double ConfidenceScore { get; set; }

    [JsonPropertyName("summary")]
    [Description("Brief summary of the text content")]
    public required string Summary { get; set; }

    [JsonPropertyName("keyPhrases")]
    [Description("Important phrases extracted from the text")]
    public required string[] KeyPhrases { get; set; }

    [JsonPropertyName("entities")]
    [Description("Named entities found in the text")]
    public NamedEntity[]? Entities { get; set; }

    [JsonPropertyName("emotionalTone")]
    [Description("The emotional tone (e.g., angry, happy, frustrated, excited)")]
    public required string EmotionalTone { get; set; }

    [JsonPropertyName("suggestedActions")]
    [Description("Recommended actions based on the sentiment")]
    public string[]? SuggestedActions { get; set; }
}
