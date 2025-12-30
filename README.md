# Practical AI Agents with Microsoft Agent Framework

> A hands-on guide for .NET developers

[![.NET](https://img.shields.io/badge/.NET-9.0+-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Microsoft Agent Framework](https://img.shields.io/badge/Microsoft-Agent%20Framework-green)](https://github.com/microsoft/agents)

## About This Repository

This is the companion source code for the book **"Practical AI Agents with Microsoft Agent Framework"** by Matteo D'Avena.

The code is organized as a progressive learning path—each project builds upon the previous one, introducing new concepts for creating AI-powered applications with .NET.

## Prerequisites

- **.NET 9 SDK** (or later)
- **OpenAI API Key** (for GPT models)
- **Docker Desktop** (for projects using vector stores)
- **Visual Studio 2022** or **VS Code** with C# extension

## Quick Start

```bash
# Clone the repository
git clone https://github.com/matdavena/practical-ai-agents-microsoft-agent-framework.git
cd practical-ai-agents-microsoft-agent-framework

# Set your OpenAI API key
# Windows PowerShell:
$env:OPENAI_API_KEY = "your-api-key-here"

# Run the first project
cd core/01.HelloAgent
dotnet run
```

## Learning Path

| # | Project | Concepts | Difficulty |
|---|---------|----------|------------|
| 01 | [Hello Agent](core/01.HelloAgent/) | Basic agent, OpenAI client, streaming | ⭐ Beginner |
| 01b | [Hello Agent - Providers](core/01b.HelloAgent.Providers/) | OpenAI, Azure OpenAI, Anthropic, Gemini, Ollama | ⭐ Beginner |
| 01c | [Hello Agent - Response API](core/01c.HelloAgent.ResponseAPI/) | Response API, stateful conversations, hosted tools | ⭐ Beginner |
| 01d | [Hello Agent - Structured Output](core/01d.HelloAgent.StructuredOutput/) | Typed responses, JSON schema, data extraction | ⭐ Beginner |
| 02 | [DevAssistant - Tools](core/02.DevAssistant.Tools/) | Function calling, AIFunctionFactory | ⭐ Beginner |
| 03 | [DevAssistant - Memory](core/03.DevAssistant.Memory/) | AgentThread, short-term memory | ⭐⭐ Intermediate |
| 04 | [DevAssistant - Long-Term Memory](core/04.DevAssistant.LongTermMemory/) | Persistent memory, session management | ⭐⭐ Intermediate |
| 05 | [Code Reviewer - RAG](core/05.CodeReviewer.RAG/) | Embeddings, vector search, RAG pattern | ⭐⭐ Intermediate |
| 06 | [Task Planner](core/06.TaskPlanner/) | Dependency injection, structured agents | ⭐⭐ Intermediate |
| 07 | [DevTeam - Multi-Agent](core/07.DevTeam.MultiAgent/) | Agent collaboration, A2A communication | ⭐⭐⭐ Advanced |
| 08 | [Workflows](core/08.Workflows.Native/) | Workflow orchestration, multi-step tasks | ⭐⭐⭐ Advanced |
| 09 | [MCP Integration](core/09.MCP.Integration/) | Model Context Protocol, external tools | ⭐⭐⭐ Advanced |
| 10 | [MCP Custom Server](core/10.MCP.CustomServer/) | Building MCP servers, tool exposure | ⭐⭐⭐ Advanced |
| 11 | [RAG with Vector Stores](core/11.RAG.VectorStores/) | Qdrant, PostgreSQL+pgvector, production RAG | ⭐⭐⭐ Advanced |
| 12 | [Web API - Chat Agents](core/12.WebApi.ChatAgents/) | ASP.NET Core, DI, Keyed Services, per-user conversations | ⭐⭐⭐ Advanced |
| 12c | [Web API - Client](core/12.WebApi.ChatAgents.Client/) | API test client, multi-user simulation | ⭐⭐ Intermediate |
| **14** | **[Expense Tracker](ExpenseTracker/)** | **Complete project: Tools, Vision, RAG, Multi-Agent, Telegram, API** | ⭐⭐⭐⭐ **Capstone** |

## Project Structure

```
LearningAgentFramework/
├── core/                              # Main learning projects
│   ├── 01.HelloAgent/                 # First agent
│   ├── 01b.HelloAgent.Providers/      # Multiple LLM providers
│   ├── 01c.HelloAgent.ResponseAPI/    # OpenAI Response API
│   ├── 01d.HelloAgent.StructuredOutput/# Typed responses
│   ├── 02.DevAssistant.Tools/         # Tools/Function calling
│   ├── 03.DevAssistant.Memory/        # Short-term memory
│   ├── 04.DevAssistant.LongTermMemory/# Long-term memory
│   ├── 05.CodeReviewer.RAG/           # RAG basics
│   ├── 06.TaskPlanner/                # DI and structured agents
│   ├── 07.DevTeam.MultiAgent/         # Multi-agent systems
│   ├── 08.Workflows.Native/           # Workflow orchestration
│   ├── 09.MCP.Integration/            # MCP client integration
│   ├── 10.MCP.CustomServer/           # Custom MCP server
│   ├── 11.RAG.VectorStores/           # Production vector stores
│   ├── 12.WebApi.ChatAgents/          # ASP.NET Core Web API with DI
│   └── 12.WebApi.ChatAgents.Client/   # API test client
├── shared/
│   └── Common/                        # Shared utilities
├── ExpenseTracker/                    # Capstone project
│   ├── src/
│   │   ├── ExpenseTracker.Core/       # Domain, Agents, Services
│   │   ├── ExpenseTracker.Infrastructure/ # Data access
│   │   ├── ExpenseTracker.Console/    # Console app
│   │   ├── ExpenseTracker.Telegram/   # Telegram bot
│   │   └── ExpenseTracker.Api/        # REST API
│   └── docs/                          # Book chapter
├── Directory.Build.props              # Common build settings
├── Directory.Packages.props           # Centralized package versions
└── README.md                          # This file
```

## Key Concepts Covered

### Core Agent Concepts
- **ChatClientAgent** - The main agent class
- **AgentThread** - Conversation context management
- **System Prompts** - Defining agent behavior
- **Streaming Responses** - Real-time output

### Tools & Function Calling
- **AIFunctionFactory** - Creating tools from .NET methods
- **[Description] Attribute** - Documenting tools for LLM
- **Static vs Instance Tools** - Different registration patterns
- **Tool Security** - Sandboxing and validation

### Memory Patterns
- **Short-Term Memory** - AgentThread-based context
- **Long-Term Memory** - Persistent storage patterns
- **Vector Memory** - Semantic search with embeddings

### RAG (Retrieval-Augmented Generation)
- **Embeddings** - Text to vector conversion
- **Vector Stores** - Qdrant, PostgreSQL+pgvector
- **Semantic Search** - Finding relevant context
- **Chunking Strategies** - Document processing

### Multi-Agent Systems
- **Agent Collaboration** - Multiple agents working together
- **A2A Communication** - Agent-to-agent messaging
- **Workflow Orchestration** - Multi-step task coordination

### MCP (Model Context Protocol)
- **MCP Clients** - Connecting to tool servers
- **MCP Servers** - Exposing tools via protocol
- **Tool Discovery** - Dynamic tool registration

## Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `OPENAI_API_KEY` | Your OpenAI API key | Yes |
| `OPENAI_MODEL` | Model to use (default: gpt-4o-mini) | No |

### User Secrets (Alternative)

```bash
cd core/01.HelloAgent
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"
```

## NuGet Packages Used

| Package | Purpose |
|---------|---------|
| `Microsoft.Agents.AI` | Core framework |
| `Microsoft.Agents.AI.OpenAI` | OpenAI integration |
| `Microsoft.Agents.AI.Workflows` | Workflow orchestration |
| `Microsoft.Agents.AI.A2A` | Agent-to-agent communication |
| `Microsoft.SemanticKernel.Connectors.Qdrant` | Qdrant vector store |
| `Microsoft.SemanticKernel.Connectors.PgVector` | PostgreSQL vector store |
| `ModelContextProtocol` | MCP client/server |

## Docker Services (for Vector Store projects)

```bash
cd core/11.RAG.VectorStores
docker compose up -d
```

| Service | Port | Purpose |
|---------|------|---------|
| Qdrant | 6333, 6334 | Vector database |
| PostgreSQL | 5433 | PostgreSQL + pgvector |
| SQL Server | 1434 | SQL Server (requires 2025) |

## Book Information

**Title**: Practical AI Agents with Microsoft Agent Framework
**Subtitle**: A hands-on guide for .NET developers
**Author**: Matteo D'Avena

## License

This code is provided for educational purposes as companion material for the book.

## Questions or Issues?

If you find bugs or have questions about the code, please [open an issue](https://github.com/matdavena/practical-ai-agents-microsoft-agent-framework/issues).

## Acknowledgments

- [Microsoft Agent Framework](https://github.com/microsoft/agents)
- [OpenAI](https://openai.com/)
- [Semantic Kernel](https://github.com/microsoft/semantic-kernel)

---

**Happy Learning!** Start with [01. Hello Agent](core/01.HelloAgent/) and work your way through the projects!
