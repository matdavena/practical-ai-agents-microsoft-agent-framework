# 01b. Hello Agent - Multiple Providers

> Using Microsoft Agent Framework with different LLM backends

## Overview

This project demonstrates that Microsoft Agent Framework is **provider-agnostic**. The same agent code works seamlessly with different LLM providers: OpenAI, Azure OpenAI, Anthropic Claude, Google Gemini, and Ollama (local models).

Understanding provider flexibility is essential because:
- **Development**: Use free local models (Ollama) or cheap cloud options (Gemini Flash)
- **Production**: Switch to cloud providers (OpenAI, Azure) for reliability
- **Enterprise**: Use Azure OpenAI for compliance and data residency
- **Reasoning**: Use Anthropic Claude for complex analytical tasks
- **Multimodal**: Use Google Gemini for text, images, video, and audio
- **Privacy**: Use Ollama when data cannot leave your infrastructure

## What You'll Learn

- How to use **OpenAI** (direct API) - cloud, pay per token
- How to use **Azure OpenAI** - enterprise, managed by Azure
- How to use **Anthropic Claude** - excellent reasoning and analysis
- How to use **Google Gemini** - multimodal capabilities
- How to use **Ollama** - local models, free, private
- How the same `ChatClientAgent` works with any backend

## Key Concepts

| Concept | Description |
|---------|-------------|
| `OpenAIClient` | Client for OpenAI's direct API |
| `AzureOpenAIClient` | Client for Azure-hosted OpenAI |
| `AnthropicClient` | Client for Anthropic Claude models |
| `GeminiChatClient` | Client for Google Gemini models |
| `OllamaApiClient` | Client for local Ollama models |
| `IChatClient` | Common interface all providers implement |

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PROVIDER ABSTRACTION                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐           │
│  │ OpenAI  │  │ Azure   │  │Anthropic│  │ Google  │  │ Ollama  │           │
│  │ (Cloud) │  │ OpenAI  │  │ Claude  │  │ Gemini  │  │ (Local) │           │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘           │
│       │            │            │            │            │                 │
│       └────────────┴─────┬──────┴────────────┴────────────┘                 │
│                          │                                                   │
│                  ┌───────▼───────┐                                          │
│                  │  IChatClient  │  (Common Interface)                      │
│                  └───────┬───────┘                                          │
│                          │                                                   │
│                  ┌───────▼───────┐                                          │
│                  │ChatClientAgent│  (Same Agent Code)                       │
│                  └───────────────┘                                          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Provider Comparison

| Feature | OpenAI | Azure OpenAI | Anthropic | Google Gemini | Ollama |
|---------|--------|--------------|-----------|---------------|--------|
| **Location** | Cloud | Azure | Cloud | Cloud | Local |
| **Cost** | Per token | Per token | Per token | Per token | Free |
| **Best For** | Quality | Enterprise | Reasoning | Multimodal | Privacy |
| **Top Models** | GPT-4o, o1 | GPT-4o | Claude 3.5 | Gemini 2.0 | Llama, Gemma |
| **Context** | 128K | 128K | 200K | 1M+ | Varies |
| **Offline** | No | No | No | No | Yes |

## Prerequisites

### For OpenAI
- API key from [platform.openai.com](https://platform.openai.com)

### For Azure OpenAI
- Azure subscription with OpenAI resource deployed

### For Anthropic Claude
- API key from [console.anthropic.com](https://console.anthropic.com)

### For Google Gemini
- API key from [aistudio.google.com](https://aistudio.google.com/apikey)

### For Ollama
1. Install from [ollama.ai](https://ollama.ai)
2. Pull a model: `ollama pull gemma3:1b`

## Configuration

### OpenAI
```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:OPENAI_MODEL = "gpt-4o-mini"  # optional
```

### Azure OpenAI
```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_DEPLOYMENT = "your-deployment-name"
$env:AZURE_OPENAI_API_KEY = "your-api-key"  # or use: az login
```

### Anthropic Claude
```powershell
$env:ANTHROPIC_API_KEY = "sk-ant-..."
$env:ANTHROPIC_MODEL = "claude-3-5-haiku-latest"  # optional
```

### Google Gemini
```powershell
$env:GOOGLE_API_KEY = "AI..."
$env:GOOGLE_MODEL = "gemini-2.0-flash"  # optional
```

### Ollama
```powershell
$env:OLLAMA_ENDPOINT = "http://localhost:11434"  # optional
$env:OLLAMA_MODEL = "gemma3:1b"  # optional
```

## Running the Project

```bash
cd core/01b.HelloAgent.Providers
dotnet run
```

## Code Walkthrough

### OpenAI

```csharp
OpenAIClient client = new(apiKey);
ChatClientAgent agent = client
    .GetChatClient(model)
    .CreateAIAgent(instructions: "...", name: "OpenAI-Agent");
```

### Azure OpenAI

```csharp
AzureOpenAIClient client = new(new Uri(endpoint), new ApiKeyCredential(apiKey));
ChatClientAgent agent = client
    .GetChatClient(deploymentName)
    .CreateAIAgent(instructions: "...", name: "Azure-Agent");
```

### Anthropic Claude

```csharp
var anthropicClient = new AnthropicClient(new APIAuthentication(apiKey));
IChatClient chatClient = anthropicClient.Messages.AsBuilder().Build();
ChatClientAgent agent = new(chatClient, instructions: "...", name: "Claude-Agent");

// Claude requires options with ModelId
var options = new ChatClientAgentRunOptions(new ChatOptions { ModelId = model });
await agent.RunAsync("Hello!", options: options);
```

### Google Gemini

```csharp
IChatClient geminiClient = new GeminiChatClient(apiKey: apiKey, model: model);
ChatClientAgent agent = new(geminiClient, instructions: "...", name: "Gemini-Agent");
```

### Ollama (Local)

```csharp
IChatClient ollamaClient = new OllamaApiClient(new Uri(endpoint), modelName);
ChatClientAgent agent = new(ollamaClient, instructions: "...", name: "Ollama-Agent");
```

## Demo Sections

1. **OpenAI Demo** - Chat using OpenAI's direct API
2. **Azure OpenAI Demo** - Chat using Azure-hosted models
3. **Anthropic Claude Demo** - Chat using Claude models
4. **Google Gemini Demo** - Chat using Gemini models
5. **Ollama Demo** - Chat using local models
6. **Compare All** - Run the same prompt on all available providers

## Choosing a Provider

| Use Case | Recommended Provider |
|----------|---------------------|
| Development/Testing | Ollama (free) or Gemini Flash (cheap) |
| Best Quality | OpenAI GPT-4o or Anthropic Claude |
| Complex Reasoning | Anthropic Claude |
| Multimodal (images/video) | Google Gemini |
| Enterprise/Compliance | Azure OpenAI |
| Privacy-Sensitive | Ollama |
| Offline Required | Ollama |

## Key Takeaways

1. **Provider Abstraction** - `ChatClientAgent` works with any `IChatClient`
2. **Same Agent Code** - Switch providers without changing logic
3. **Configuration Only** - Provider selection is configuration, not code
4. **Trade-offs Matter** - Choose based on cost, privacy, and capabilities

## Next Steps

Continue to [02. DevAssistant - Tools](../02.DevAssistant.Tools/) to learn about function calling.

## Related Resources

- [Microsoft Agent Framework](https://github.com/microsoft/agents)
- [OpenAI API](https://platform.openai.com/docs)
- [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/)
- [Anthropic Claude](https://docs.anthropic.com/)
- [Google Gemini](https://ai.google.dev/)
- [Ollama](https://ollama.ai)
