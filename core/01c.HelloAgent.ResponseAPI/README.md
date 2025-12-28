# 01c. Hello Agent - OpenAI Response API

> Understanding OpenAI's evolved API for agentic applications

## Overview

This project demonstrates **OpenAI's Response API**, an evolution of the Chat Completions API launched in March 2025. While Chat Completions remains supported, Response API is OpenAI's recommended approach for new agentic applications.

## Why Response API?

OpenAI introduced Response API to address limitations of Chat Completions for agent use cases:

- **Stateful Conversations**: No need to send full conversation history
- **Built-in Tools**: Web search, code interpreter, file search without custom implementation
- **Better Performance**: 40-80% improved cache utilization
- **Reasoning Access**: Get reasoning summaries from reasoning models
- **MCP Support**: Native integration with Model Context Protocol servers

## Response API vs Chat Completions

| Feature | Chat Completions | Response API |
|---------|------------------|--------------|
| **API Pattern** | `GetChatClient()` | `GetResponsesClient()` |
| **State** | Stateless | Stateful (server-side) |
| **Conversation** | Send full history | Use `previous_response_id` |
| **Web Search** | Custom implementation | Built-in `HostedWebSearchTool` |
| **Code Interpreter** | Not available | Built-in `HostedCodeInterpreterTool` |
| **File Search** | Custom RAG needed | Built-in `HostedFileSearchTool` |
| **MCP Servers** | Not supported | Native support |
| **Cache** | Standard | 40-80% better |
| **Reasoning** | Hidden | Accessible summaries |
| **Future** | Maintained | Primary development |

## Key Concepts

| Concept | Description |
|---------|-------------|
| `ResponsesClient` | Client for OpenAI Response API |
| `GetResponsesClient()` | Extension method to get Response API client |
| `ResponseId` | Unique ID for each response (for resuming conversations) |
| `ConversationId` | Previous response ID to continue conversation |
| `HostedWebSearchTool` | Built-in web search tool |
| `HostedCodeInterpreterTool` | Built-in Python code execution |
| `HostedFileSearchTool` | Built-in file/vector search |

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         RESPONSE API ARCHITECTURE                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                        OpenAI Server-Side                             │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │   │
│  │  │ Web Search  │  │    Code     │  │    File     │  │     MCP     │  │   │
│  │  │   (Bing)    │  │ Interpreter │  │   Search    │  │   Servers   │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  │   │
│  │                                                                       │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐ │   │
│  │  │              Conversation State Storage                          │ │   │
│  │  │         (Accessible via previous_response_id)                    │ │   │
│  │  └─────────────────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                            Response API                                      │
│                                    │                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                        Your Application                               │   │
│  │                                                                       │   │
│  │  ┌─────────────┐      ┌─────────────┐      ┌─────────────┐           │   │
│  │  │ OpenAIClient│─────▶│Responses    │─────▶│ChatClient   │           │   │
│  │  │             │      │Client       │      │Agent        │           │   │
│  │  └─────────────┘      └─────────────┘      └─────────────┘           │   │
│  │                                                                       │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Prerequisites

- **OpenAI API Key** from [platform.openai.com](https://platform.openai.com)
- Note: Some features (Web Search, Code Interpreter) are **OpenAI-only** and not available on Azure OpenAI

## Configuration

```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:OPENAI_MODEL = "gpt-4o-mini"  # optional, default: gpt-4o-mini
```

## Running the Project

```bash
cd core/01c.HelloAgent.ResponseAPI
dotnet run
```

## Code Walkthrough

### Basic Response API Usage

```csharp
using OpenAI;
using OpenAI.Responses;
using Microsoft.Agents.AI;

OpenAIClient client = new(apiKey);

// KEY DIFFERENCE: GetResponsesClient() instead of GetChatClient()
ResponsesClient responsesClient = client.GetResponsesClient("gpt-4o-mini");

// Same CreateAIAgent() extension method
AIAgent agent = responsesClient.CreateAIAgent(
    instructions: "You are a helpful assistant."
);

// Same RunAsync() method
AgentRunResponse response = await agent.RunAsync("Hello!");
```

### Stateful Conversation (Resume with Response ID)

```csharp
// First message
AgentRunResponse response1 = await agent.RunAsync("My name is Marco.", thread);

// Store response ID (save to database in production)
string responseId = response1.ResponseId;

// Later: Resume conversation with just the ID (no history needed!)
var options = new ChatClientAgentRunOptions
{
    ChatOptions = new ChatOptions
    {
        ConversationId = responseId  // Just pass the previous ID!
    }
};

AgentRunResponse response2 = await agent.RunAsync("What's my name?", options: options);
// Agent remembers: "Your name is Marco"
```

### Web Search Tool

```csharp
AIAgent agent = responsesClient.CreateAIAgent(
    instructions: "Search the web for current information.",
    tools: [new HostedWebSearchTool()]  // Built-in!
);

await agent.RunAsync("What are today's tech news headlines?");
// Agent automatically searches the web and synthesizes results
```

### Code Interpreter Tool

```csharp
AIAgent agent = responsesClient.CreateAIAgent(
    instructions: "You are a data analyst.",
    tools: [new HostedCodeInterpreterTool()]  // Built-in!
);

await agent.RunAsync("Calculate first 20 Fibonacci numbers and show as table.");
// Agent writes and executes Python code, returns formatted results
```

### File Search Tool

```csharp
// Upload file to OpenAI
var fileClient = client.GetOpenAIFileClient();
var uploadResult = await fileClient.UploadFileAsync(data, "document.pdf", FileUploadPurpose.UserData);

// Create vector store
var vectorClient = client.GetVectorStoreClient();
var vectorStore = await vectorClient.CreateVectorStoreAsync(new VectorStoreCreationOptions { Name = "MyDocs" });
await vectorClient.AddFileToVectorStoreAsync(vectorStore.Value.Id, uploadResult.Value.Id);

// Create agent with file search
AIAgent agent = responsesClient.CreateAIAgent(
    instructions: "Answer questions from the uploaded documents.",
    tools: [new HostedFileSearchTool
    {
        Inputs = [new HostedVectorStoreContent(vectorStore.Value.Id)]
    }]
);
```

## Demo Sections

1. **Basic Response API** - Simple agent using Response API
2. **Stateful Conversation** - Resume conversations with response ID
3. **Web Search Tool** - Built-in web search capability
4. **Code Interpreter** - Execute Python in sandboxed environment
5. **Compare APIs** - Side-by-side comparison with Chat Completions

## When to Use Each API

### Use Response API When:
- Building agentic applications
- Need built-in tools (web search, code interpreter, file search)
- Want stateful conversations without managing history
- Working with reasoning models and need reasoning summaries
- Planning to use MCP servers

### Use Chat Completions When:
- Simple, stateless text generation
- Need maximum compatibility (Azure OpenAI, other providers)
- Don't need built-in tools
- Have existing Chat Completions integration

## Azure OpenAI Support

> **Important**: As of 2025, some Response API features are OpenAI-only:
> - `HostedWebSearchTool` - OpenAI only
> - `HostedCodeInterpreterTool` - OpenAI only (file download issues on Azure)
> - `HostedFileSearchTool` - Limited support on Azure
>
> Basic Response API (stateful conversations) works on Azure OpenAI.

## Key Takeaways

1. **Same Agent Code** - `ChatClientAgent` works with both APIs
2. **Stateful by Default** - Response API stores conversation state server-side
3. **Built-in Tools** - No need to implement web search or code execution
4. **Future-Proof** - OpenAI's primary API for new development
5. **Better Performance** - Improved caching reduces cost and latency

## Next Steps

Continue to [02. DevAssistant - Tools](../02.DevAssistant.Tools/) to learn about custom function calling.

## Related Resources

- [OpenAI Response API Documentation](https://platform.openai.com/docs/api-reference/responses)
- [Why We Built the Responses API](https://developers.openai.com/blog/responses-api/)
- [Responses vs Chat Completions](https://platform.openai.com/docs/guides/responses-vs-chat-completions)
- [Migrate to Responses API](https://platform.openai.com/docs/guides/migrate-to-responses)
- [Microsoft Agent Framework](https://github.com/microsoft/agents)

## Sources

- [OpenAI API: Responses vs. Chat Completions](https://simonwillison.net/2025/Mar/11/responses-vs-chat-completions/)
- [OpenAI Migrate to Responses API](https://platform.openai.com/docs/guides/migrate-to-responses)
- [Why We Built the Responses API - OpenAI](https://developers.openai.com/blog/responses-api/)
