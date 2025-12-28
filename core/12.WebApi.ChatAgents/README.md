# 12. Web API - Chat Agents

> Integrating Microsoft Agent Framework with ASP.NET Core Web API, Dependency Injection, and Keyed Services

## Overview

This project demonstrates how to build a **production-ready chat API** using Microsoft Agent Framework with ASP.NET Core. It showcases:

- **Dependency Injection** patterns for agent management
- **Keyed Services** for registering multiple specialized agents
- **Conversation persistence** for resumable chats
- **ChatGPT-style REST API** for frontend integration

## Key Concepts

### 1. Why Dependency Injection?

Dependency Injection (DI) is fundamental to modern .NET applications:

```
Without DI:
┌─────────────────────────────────────────────────────┐
│ Controller                                          │
│   └── new OpenAIClient(apiKey)          // Coupled │
│       └── new ChatClientAgent(client)   // Coupled │
└─────────────────────────────────────────────────────┘

With DI:
┌─────────────────────────────────────────────────────┐
│ DI Container                                        │
│   ├── OpenAIClient (Singleton)                      │
│   ├── ChatClientAgent "assistant" (Keyed Singleton) │
│   ├── ChatClientAgent "coder" (Keyed Singleton)     │
│   └── IConversationStore (Singleton)                │
└─────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────┐
│ Controller                                          │
│   ├── IServiceProvider ─────► Get agent by key      │
│   └── IConversationStore ───► Injected automatically│
└─────────────────────────────────────────────────────┘
```

**Benefits:**
- **Testability**: Easy to mock dependencies in unit tests
- **Flexibility**: Swap implementations without changing code
- **Lifetime management**: Container handles object lifecycles
- **Loose coupling**: Components don't know concrete implementations

### 2. Keyed Services (.NET 8+)

Keyed Services allow registering multiple implementations with unique keys:

```csharp
// Registration
builder.Services.AddKeyedSingleton<ChatClientAgent>("assistant", (sp, key) =>
{
    var client = sp.GetRequiredService<OpenAIClient>();
    return new ChatClientAgentBuilder()
        .WithName("Assistant")
        .WithInstructions("You are a helpful assistant...")
        .WithChatClient(client.GetChatClient(model).AsIChatClient())
        .Build();
});

builder.Services.AddKeyedSingleton<ChatClientAgent>("coder", (sp, key) =>
{
    // Different configuration for code assistant
});

// Resolution
var agent = serviceProvider.GetRequiredKeyedService<ChatClientAgent>("assistant");
```

### 3. Conversation Persistence

Agent threads can be serialized and restored for resumable conversations:

```csharp
// Save conversation state
JsonElement threadState = thread.Serialize();
await store.SaveThreadStateAsync(conversationId, threadState);

// Restore later
var savedState = await store.GetThreadStateAsync(conversationId);
if (savedState.HasValue)
{
    AgentThread thread = agent.DeserializeThread(savedState.Value);
}
```

### 4. Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         React Frontend                          │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core Web API                       │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                     ChatController                        │  │
│  │  GET  /api/agents              - List agents              │  │
│  │  POST /api/chat/{agentKey}     - Chat with agent          │  │
│  │  GET  /api/conversations       - List conversations       │  │
│  │  GET  /api/conversations/{id}  - Get conversation         │  │
│  │  DELETE /api/conversations/{id} - Delete conversation     │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                │                                │
│                                ▼                                │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    DI Container                           │  │
│  │                                                           │  │
│  │  ┌─────────────────┐  ┌─────────────────┐                 │  │
│  │  │ Keyed Services  │  │    Services     │                 │  │
│  │  │                 │  │                 │                 │  │
│  │  │ "assistant"  ───┼──┤ OpenAIClient    │                 │  │
│  │  │ "coder"      ───┼──┤ IConversationStore                │  │
│  │  │ "translator" ───┼──┤                 │                 │  │
│  │  └─────────────────┘  └─────────────────┘                 │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      OpenAI API                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5. Thread Safety and Concurrent Users

A critical aspect of this architecture: **agents are thread-safe and can handle thousands of concurrent conversations**.

#### Separation of Agent and State

```
┌─────────────────────────────────────────────────────────────────────┐
│                    ChatClientAgent (Singleton)                      │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │  • System Prompt (instructions) - immutable                   │  │
│  │  • OpenAI ChatClient - thread-safe                            │  │
│  │  • Agent name - immutable                                     │  │
│  │                                                               │  │
│  │  ⚠️  NO CONVERSATION STATE STORED HERE                        │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ RunAsync(message, thread)
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    AgentThread (per-conversation)                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐      │
│  │  Thread Alice   │  │  Thread Bob     │  │  Thread Charlie │      │
│  │  ────────────── │  │  ────────────── │  │  ────────────── │      │
│  │  - Message 1    │  │  - Message 1    │  │  - Message 1    │      │
│  │  - Message 2    │  │  - Message 2    │  │  - Message 2    │      │
│  │  - ...          │  │  - ...          │  │  - ...          │      │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘      │
└─────────────────────────────────────────────────────────────────────┘
```

#### Why This Works

1. **The agent is stateless** regarding conversations:
   - Contains only configuration (instructions, OpenAI client)
   - Does not store messages or user state
   - Acts like a "factory" that processes requests

2. **State lives in AgentThread**:
   - Each conversation has its own thread
   - The thread contains message history
   - Passed to each `RunAsync(message, thread)` call

3. **Request flow**:
   ```csharp
   // 1. Get the singleton agent (shared across all users)
   var agent = serviceProvider.GetRequiredKeyedService<ChatClientAgent>("assistant");

   // 2. Get/create the thread for THIS specific conversation
   var thread = agent.GetNewThread();  // or deserialize from storage

   // 3. Execute - the thread carries the state
   var response = await agent.RunAsync(message, thread);

   // 4. Save the thread for later
   var state = thread.Serialize();
   await store.SaveThreadStateAsync(conversationId, state);
   ```

#### Restaurant Analogy

- **Chef (Agent Singleton)**: knows how to cook, has recipes, there's only one
- **Orders (AgentThread)**: each table has its own separate order
- The chef can handle multiple orders concurrently because each order is independent

#### Why ChatClientAgent is Thread-Safe

- `OpenAIClient` is thread-safe (uses `HttpClient` internally)
- Instructions are immutable (set once at creation)
- No shared mutable state

**Conclusion**: A single agent instance can handle thousands of concurrent conversations - each conversation simply has its own `AgentThread`.

## Running the Project

### Prerequisites

- .NET 10 SDK
- OpenAI API Key

### Setup

```bash
# Set your API key
$env:OPENAI_API_KEY = "your-api-key"

# Run the API
cd core/12.WebApi.ChatAgents
dotnet run
```

### Access

- **Swagger UI**: http://localhost:5200
- **API Base URL**: http://localhost:5200/api

## API Reference

### Agents

#### List Available Agents
```http
GET /api/agents
```

Response:
```json
[
  {
    "key": "assistant",
    "name": "General Assistant",
    "description": "A helpful general-purpose AI assistant for everyday tasks."
  },
  {
    "key": "coder",
    "name": "Code Assistant",
    "description": "A specialized assistant for programming and code-related tasks."
  },
  {
    "key": "translator",
    "name": "Translator",
    "description": "A multilingual translator for translating text between languages."
  }
]
```

### Chat

#### Send a Message
```http
POST /api/chat/{agentKey}
Content-Type: application/json
X-User-Id: user123

{
  "message": "Hello, how can you help me?",
  "conversationId": null
}
```

Response:
```json
{
  "conversationId": "abc-123-def",
  "message": "Hello! I'm your AI assistant. I can help you with...",
  "role": "assistant",
  "timestamp": "2024-01-15T10:30:00Z",
  "messageId": "msg-456"
}
```

#### Continue a Conversation
```http
POST /api/chat/assistant
Content-Type: application/json
X-User-Id: user123

{
  "message": "Tell me more about that",
  "conversationId": "abc-123-def"
}
```

### Conversations

#### List User's Conversations
```http
GET /api/conversations
X-User-Id: user123
```

#### Get Conversation with History
```http
GET /api/conversations/{conversationId}
X-User-Id: user123
```

#### Delete a Conversation
```http
DELETE /api/conversations/{conversationId}
X-User-Id: user123
```

## Testing with cURL

```bash
# List agents
curl http://localhost:5200/api/agents

# Start a new conversation
curl -X POST http://localhost:5200/api/chat/assistant \
  -H "Content-Type: application/json" \
  -H "X-User-Id: demo-user" \
  -d '{"message": "What is dependency injection?"}'

# Continue conversation (use the conversationId from above)
curl -X POST http://localhost:5200/api/chat/assistant \
  -H "Content-Type: application/json" \
  -H "X-User-Id: demo-user" \
  -d '{"message": "Can you give me an example?", "conversationId": "YOUR-CONVERSATION-ID"}'

# List conversations
curl http://localhost:5200/api/conversations \
  -H "X-User-Id: demo-user"
```

## Code Highlights

### Keyed Service Registration (Program.cs)

```csharp
// Register multiple agents with different configurations
builder.Services.AddKeyedSingleton<ChatClientAgent>("assistant", (sp, key) =>
{
    var client = sp.GetRequiredService<OpenAIClient>();
    return new ChatClientAgentBuilder()
        .WithName("Assistant")
        .WithInstructions("You are a helpful assistant...")
        .WithChatClient(client.GetChatClient(model).AsIChatClient())
        .Build();
});

builder.Services.AddKeyedSingleton<ChatClientAgent>("coder", (sp, key) =>
{
    // Different system prompt for code-focused agent
});
```

### Resolving Keyed Services (ChatController.cs)

```csharp
// Get specific agent by key at runtime
var agent = _serviceProvider.GetRequiredKeyedService<ChatClientAgent>(agentKey);
```

### Thread Persistence (ChatController.cs)

```csharp
// Save state after each interaction
var threadState = thread.Serialize();
await _conversationStore.SaveThreadStateAsync(conversationId, threadState);

// Restore state when resuming
var savedState = await _conversationStore.GetThreadStateAsync(conversationId);
if (savedState.HasValue)
{
    thread = agent.DeserializeThread(savedState.Value);
}
```

## Production Considerations

### 1. Authentication

Replace the `X-User-Id` header with proper authentication:

```csharp
// In ChatController
private string GetUserId()
{
    // Production: Get from JWT claims
    return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedException();
}
```

### 2. Persistent Storage

Replace `InMemoryConversationStore` with a database implementation:

```csharp
// SQL Server / PostgreSQL
builder.Services.AddSingleton<IConversationStore, SqlConversationStore>();

// Redis for distributed scenarios
builder.Services.AddSingleton<IConversationStore, RedisConversationStore>();
```

### 3. Rate Limiting

Add rate limiting to protect your API:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("chat", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

### 4. Streaming Responses

For real-time chat experience, implement Server-Sent Events:

```csharp
[HttpPost("chat/{agentKey}/stream")]
public async Task StreamChat(string agentKey, ChatRequest request)
{
    Response.ContentType = "text/event-stream";

    await foreach (var chunk in agent.InvokeStreamingAsync(message, thread))
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n");
        await Response.Body.FlushAsync();
    }
}
```

## Key Takeaways

1. **DI is Essential**: Modern .NET apps should use dependency injection for testability and flexibility

2. **Keyed Services Enable Multi-Agent**: Register multiple agents with different configurations using the same interface

3. **Thread Serialization Enables Persistence**: Save and restore conversation state for resumable chats

4. **API-First Design**: RESTful endpoints make it easy to integrate with any frontend

5. **Abstraction Enables Flexibility**: `IConversationStore` interface allows swapping storage implementations

## Next Steps

- Add streaming support with Server-Sent Events
- Implement a database-backed conversation store
- Add JWT authentication
- Build a React frontend
- Add agent tools for dynamic functionality

---

**Previous**: [11. RAG with Vector Stores](../11.RAG.VectorStores/)
