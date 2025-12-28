# 01. Hello Agent

> Your first AI agent with Microsoft Agent Framework

## Overview

This is the starting point of your journey with Microsoft Agent Framework. In this project, you'll create your first AI agent and understand the fundamental concepts that power all agent-based applications.

## What You'll Learn

- ✅ How to create an `OpenAIClient` to communicate with OpenAI APIs
- ✅ How to create a `ChatClientAgent` (the actual agent)
- ✅ How to execute a single request with `RunAsync()`
- ✅ How to handle response streaming with `RunStreamingAsync()`
- ✅ How to maintain conversation context with `AgentThread`

## Key Concepts

| Concept | Description |
|---------|-------------|
| `OpenAIClient` | The HTTP client that communicates with OpenAI APIs |
| `ChatClient` | The chat-specific client from the OpenAI SDK |
| `ChatClientAgent` | The framework agent that wraps the ChatClient with agentic capabilities |
| `AgentThread` | Maintains conversation state (short-term memory) |
| `AgentRunResponse` | The agent's response object |

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  OpenAIClient   │────▶│   ChatClient    │────▶│ChatClientAgent  │
│  (HTTP Client)  │     │   (Chat API)    │     │  (AI Agent)     │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Prerequisites

1. **.NET 10 SDK** installed
2. **OpenAI API Key** configured (see [Configuration](#configuration))

## Configuration

Set your OpenAI API key using one of these methods:

### Option 1: Environment Variable (Recommended)
```bash
# Windows (PowerShell)
$env:OPENAI_API_KEY = "your-api-key-here"

# Windows (CMD)
set OPENAI_API_KEY=your-api-key-here

# Linux/macOS
export OPENAI_API_KEY="your-api-key-here"
```

### Option 2: User Secrets
```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
```

## Running the Project

```bash
cd core/01.HelloAgent
dotnet run
```

## Code Walkthrough

### Step 1: Create the OpenAI Client

```csharp
OpenAIClient openAiClient = new(apiKey);
```

The `OpenAIClient` is the entry point for all communications with OpenAI APIs.

### Step 2: Create the Agent

```csharp
ChatClientAgent agent = openAiClient
    .GetChatClient(model)
    .CreateAIAgent(
        instructions: "You are a friendly AI assistant...",
        name: "HelloAgent"
    );
```

The `CreateAIAgent()` extension method transforms a simple `ChatClient` into a `ChatClientAgent` with advanced capabilities.

### Step 3: Single Request (RunAsync)

```csharp
AgentRunResponse response = await agent.RunAsync("Your question here");
Console.WriteLine(response.ToString());
```

`RunAsync()` sends a message and waits for the complete response.

### Step 4: Streaming Response (RunStreamingAsync)

```csharp
await foreach (var update in agent.RunStreamingAsync("Your question"))
{
    Console.Write(update.ToString());
}
```

`RunStreamingAsync()` returns the response token by token for better UX.

### Step 5: Conversation with Thread

```csharp
AgentThread thread = agent.GetNewThread();

await foreach (var update in agent.RunStreamingAsync(userInput, thread))
{
    Console.Write(update.ToString());
}
```

`AgentThread` maintains conversation history, enabling multi-turn conversations.

## Demo Sections

The project includes three interactive demos:

1. **Demo 1: Single Request** - Basic question/answer with `RunAsync()`
2. **Demo 2: Streaming Response** - Real-time response display with `RunStreamingAsync()`
3. **Demo 3: Conversation with Thread** - Interactive chat with memory

## Key Takeaways

1. **System Prompt (instructions)** - Defines the agent's behavior and personality
2. **RunAsync vs RunStreamingAsync** - Choose based on UX requirements
3. **AgentThread** - Essential for maintaining conversation context
4. **Token Usage** - Longer threads consume more tokens

## Next Steps

Continue to [02. Chat With History](../02.ChatWithHistory/) to learn about:
- Adding tools to your agent
- Enabling the agent to perform real-world actions
- Understanding the Tool Calling pattern

## Related Resources

- [Microsoft Agent Framework Documentation](https://github.com/microsoft/agents)
- [OpenAI API Documentation](https://platform.openai.com/docs)
