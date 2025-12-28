# 01d. Hello Agent - Structured Output

> Getting typed, structured responses from LLMs

## Overview

This project demonstrates **Structured Output**, a feature that ensures the LLM returns data in a specific format defined by a C# class. Instead of free-form text that requires parsing, you get typed objects you can use directly in your code.

## Why Structured Output?

Without structured output:
```csharp
// Free-form text response
string response = "John Smith is 35 years old and works as a software engineer in Seattle...";

// You need regex, string parsing, or another LLM call to extract data
// Error-prone and unreliable!
```

With structured output:
```csharp
// Typed response
PersonInfo person = response.Result;

// Direct property access - type-safe!
Console.WriteLine(person.Name);        // "John Smith"
Console.WriteLine(person.Age);         // 35
Console.WriteLine(person.Occupation);  // "Software Engineer"
```

## Key Benefits

| Benefit | Description |
|---------|-------------|
| **Type Safety** | Compile-time checking of response structure |
| **No Parsing** | Direct property access, no regex needed |
| **Guaranteed Schema** | LLM always returns valid JSON matching your class |
| **Nested Objects** | Support for complex hierarchies and arrays |
| **Enum Support** | Enums are converted to string values |

## How It Works

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        STRUCTURED OUTPUT FLOW                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐                    │
│  │  C# Class   │────▶│ JSON Schema │────▶│  LLM API    │                    │
│  │ (PersonInfo)│     │ (generated) │     │  Request    │                    │
│  └─────────────┘     └─────────────┘     └──────┬──────┘                    │
│                                                  │                           │
│                                                  ▼                           │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐                    │
│  │   Typed     │◀────│   JSON      │◀────│  LLM API    │                    │
│  │   Object    │     │  Response   │     │  Response   │                    │
│  └─────────────┘     └─────────────┘     └─────────────┘                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Usage Methods

### Method 1: Generic RunAsync<T>() (Recommended)

```csharp
// Simplest approach - specify type at call time
AgentRunResponse<PersonInfo> response = await agent.RunAsync<PersonInfo>(
    "Extract info about John Smith, 35, software engineer."
);

// Access typed result
PersonInfo person = response.Result;
Console.WriteLine(person.Name);  // Direct property access
```

### Method 2: ResponseFormat at Agent Creation

```csharp
// Specify format when creating the agent
ChatClientAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "PersonExtractor",
    ChatOptions = new ChatOptions
    {
        Instructions = "Extract person information.",
        ResponseFormat = ChatResponseFormat.ForJsonSchema<PersonInfo>()
    }
});

// All responses from this agent will be PersonInfo
var response = await agent.RunAsync("...");
PersonInfo person = response.Deserialize<PersonInfo>();
```

### Method 3: Manual with ChatClientAgentRunOptions

```csharp
// Most control - specify per-request options
AgentRunResponse response = await agent.RunAsync(query, options: new ChatClientAgentRunOptions
{
    ChatOptions = new ChatOptions
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema<PersonInfo>(jsonOptions)
    }
});

PersonInfo person = response.Deserialize<PersonInfo>(jsonOptions);
```

## Model Class Guidelines

### Basic Properties

```csharp
using System.ComponentModel;
using System.Text.Json.Serialization;

[Description("Information about a person")]  // Helps LLM understand the class
public class PersonInfo
{
    [JsonPropertyName("name")]               // JSON property name
    [Description("The person's full name")]  // Property description for LLM
    public string? Name { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }            // Nullable for optional fields
}
```

### Nested Objects

```csharp
public class Recipe
{
    public required string Name { get; set; }
    public required Ingredient[] Ingredients { get; set; }  // Array of objects
    public required CookingStep[] Steps { get; set; }       // Nested objects
}

public class Ingredient
{
    public required string Name { get; set; }
    public required string Quantity { get; set; }
}
```

### Enums

```csharp
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Expert
}

public class Recipe
{
    public DifficultyLevel Difficulty { get; set; }  // Serialized as string
}
```

## Limitations

| Limitation | Details |
|------------|---------|
| **No DateTime** | Use string for dates (e.g., "2024-01-15") |
| **No Uri** | Use string for URLs |
| **Max Depth** | 5 levels of nesting |
| **Max Properties** | 100 total across all objects |
| **No minLength/maxLength** | Type constraints not enforced |

## Demo Sections

1. **Person Extraction** - Extract person info from unstructured text
2. **Recipe Generator** - Generate structured recipes with nested objects
3. **Sentiment Analysis** - Analyze text sentiment with entities
4. **Compare Outputs** - Side-by-side structured vs unstructured
5. **Interactive** - Try custom queries with different types

## Prerequisites

- **OpenAI API Key** from [platform.openai.com](https://platform.openai.com)
- Structured Output requires GPT-4o or newer models

## Configuration

```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:OPENAI_MODEL = "gpt-4o-mini"  # Must support structured output
```

## Running the Project

```bash
cd core/01d.HelloAgent.StructuredOutput
dotnet run
```

## Project Structure

```
01d.HelloAgent.StructuredOutput/
├── Models/
│   ├── PersonInfo.cs         # Simple extraction model
│   ├── Recipe.cs             # Complex nested model
│   └── SentimentAnalysis.cs  # NLP analysis model
├── Program.cs                # Demo implementations
└── README.md                 # This file
```

## Use Cases

| Use Case | Model | Description |
|----------|-------|-------------|
| **Data Extraction** | PersonInfo | Extract entities from unstructured text |
| **Content Generation** | Recipe | Generate structured content |
| **Text Analysis** | SentimentAnalysis | NLP tasks with structured results |
| **Form Filling** | Custom | Auto-fill forms from natural language |
| **API Responses** | Custom | Generate structured API responses |

## Key Takeaways

1. **Type Safety** - Compile-time checking prevents runtime errors
2. **RunAsync<T>()** - Simplest way to get typed responses
3. **[Description]** - Helps LLM understand your schema
4. **JsonPropertyName** - Controls JSON serialization
5. **Nested Objects** - Support for complex data structures
6. **Enums as Strings** - Use JsonStringEnumConverter for readability

## Next Steps

Continue to [02. DevAssistant - Tools](../02.DevAssistant.Tools/) to learn about function calling.

## Related Resources

- [OpenAI Structured Outputs Guide](https://platform.openai.com/docs/guides/structured-outputs)
- [Using JSON Schema for Structured Output in .NET](https://devblogs.microsoft.com/semantic-kernel/using-json-schema-for-structured-output-in-net-for-openai-models/)
- [Microsoft Agent Framework](https://github.com/microsoft/agents)

## Sources

- [OpenAI Structured Outputs Documentation](https://platform.openai.com/docs/guides/structured-outputs)
- [Introducing Structured Outputs - OpenAI](https://openai.com/index/introducing-structured-outputs-in-the-api/)
- [Semantic Kernel JSON Schema Guide](https://devblogs.microsoft.com/semantic-kernel/using-json-schema-for-structured-output-in-net-for-openai-models/)
