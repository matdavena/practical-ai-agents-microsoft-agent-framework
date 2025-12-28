# 02. DevAssistant - Tools

> Giving your AI agent the ability to act in the real world

## Overview

In this project, we extend our AI agent with **Tools** - functions that the agent can call to perform actions and retrieve information from the real world. This is the foundation of the "Function Calling" pattern that makes AI agents truly useful.

## What You'll Learn

- ✅ How to define Tools using C# methods with `[Description]` attribute
- ✅ How to use `AIFunctionFactory.Create()` to register tools
- ✅ The Function Calling pattern: LLM decides when to call tools
- ✅ Difference between static tools and instance tools
- ✅ Best practices for tool security (sandboxing)

## Key Concepts

| Concept | Description |
|---------|-------------|
| `Tool/Function` | A function that the agent can invoke |
| `AIFunctionFactory` | Factory to create `AITool` from .NET methods |
| `[Description]` | Attribute that describes the tool to the LLM |
| `Function Calling` | Pattern where the LLM decides to use a tool |

## Function Calling Flow

```
┌──────────────────────────────────────────────────────────────────────┐
│  1. User: "What time is it?"                                         │
│                    ↓                                                 │
│  2. LLM analyzes available tools                                     │
│                    ↓                                                 │
│  3. LLM decides: "I should call get_current_datetime"               │
│                    ↓                                                 │
│  4. Framework executes the .NET method                               │
│                    ↓                                                 │
│  5. Result: "Sunday, December 22, 2024 - 14:30:00 (LOCAL)"          │
│                    ↓                                                 │
│  6. LLM: "The current time is 2:30 PM on Sunday, December 22nd"     │
└──────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
02.DevAssistant.Tools/
├── Program.cs                    # Main program with agent setup
├── Tools/
│   ├── DateTimeTools.cs         # Date/time tools (static)
│   ├── CalculatorTools.cs       # Math tools (static)
│   └── FileSystemTools.cs       # File operations (instance)
└── README.md
```

## Tools Included

### DateTime Tools (Static)
| Tool | Description |
|------|-------------|
| `get_current_datetime` | Gets current date and time |
| `get_timezone` | Gets system timezone info |
| `calculate_date_difference` | Calculates time between dates |
| `get_day_of_week` | Gets day name for a date |

### Calculator Tools (Static)
| Tool | Description |
|------|-------------|
| `calculate` | Basic math operations (+, -, *, /, ^, %) |
| `calculate_percentage` | Percentage calculations |
| `convert_units` | Unit conversions (km/mi, kg/lb, °C/°F) |
| `calculate_statistics` | Mean, median, min, max, sum |

### FileSystem Tools (Instance)
| Tool | Description |
|------|-------------|
| `get_working_directory` | Gets sandbox directory |
| `list_files` | Lists directory contents |
| `read_file` | Reads file content |
| `write_file` | Creates/modifies files |
| `create_directory` | Creates directories |
| `delete_file` | Deletes files |

## Code Examples

### Defining a Static Tool

```csharp
public static class DateTimeTools
{
    [Description("Gets the current date and time. Use when user asks what time it is.")]
    public static string GetCurrentDateTime(
        [Description("'local' or 'utc'")]
        string timeType = "local")
    {
        var dt = timeType == "utc" ? DateTime.UtcNow : DateTime.Now;
        return $"{dt:dddd, MMMM dd, yyyy - HH:mm:ss}";
    }
}
```

### Defining an Instance Tool

```csharp
public class FileSystemTools
{
    public string WorkingDirectory { get; }

    public FileSystemTools(string? workingDir = null)
    {
        WorkingDirectory = workingDir ?? "./workspace";
    }

    [Description("Reads a file's content")]
    public string ReadFile(
        [Description("Relative path to the file")]
        string path)
    {
        var fullPath = ValidatePath(path);
        return File.ReadAllText(fullPath);
    }
}
```

### Registering Tools

```csharp
var fileTools = new FileSystemTools();

var tools = new List<AITool>
{
    // Static tools
    AIFunctionFactory.Create(DateTimeTools.GetCurrentDateTime, "get_current_datetime"),
    AIFunctionFactory.Create(CalculatorTools.Calculate, "calculate"),

    // Instance tools
    AIFunctionFactory.Create(fileTools.ReadFile, "read_file"),
    AIFunctionFactory.Create(fileTools.WriteFile, "write_file"),
};

ChatClientAgent agent = openAiClient
    .GetChatClient(model)
    .CreateAIAgent(
        instructions: "You are a helpful assistant with access to tools...",
        tools: tools
    );
```

## Security Best Practices

1. **Sandbox file operations** - Limit to a specific directory
2. **Validate all paths** - Prevent path traversal attacks (`../../../etc/passwd`)
3. **Handle errors gracefully** - Return useful error messages
4. **Document tool behavior** - Clear `[Description]` attributes

## Running the Project

```bash
cd core/02.DevAssistant.Tools
dotnet run
```

## Sample Interactions

```
You: What time is it?
Agent: [calls get_current_datetime] It's currently 2:30 PM on Sunday, December 22, 2024.

You: What is 15% of 250?
Agent: [calls calculate_percentage] 15% of 250 is 37.5.

You: Create a file called notes.txt with "Hello World"
Agent: [calls write_file] File 'notes.txt' has been created successfully with your content.

You: Convert 100 kilometers to miles
Agent: [calls convert_units] 100 km equals approximately 62.14 miles.
```

## Key Takeaways

1. **[Description] is crucial** - The LLM uses it to decide when to call the tool
2. **Return strings** - Easy for the LLM to interpret and use in responses
3. **Handle errors** - Return error messages that the LLM can explain to users
4. **Security first** - Always validate inputs, especially for file operations

## Next Steps

Continue to [03. DevAssistant - Memory](../03.DevAssistant.Memory/) to learn about:
- Short-term memory (conversation context)
- Long-term memory (persists between sessions)
- How agents can remember user preferences

## Related Resources

- [Microsoft Agent Framework - Tools](https://github.com/microsoft/agents)
- [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling)
