# Expense Tracker

> A complete AI-powered expense management application demonstrating all Microsoft Agent Framework concepts

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Overview

Expense Tracker is a multi-interface application for personal expense management, powered by AI agents. It allows users to record expenses through natural language or receipt photos, with automatic categorization and conversational queries.

This project serves as a **capstone project** demonstrating all concepts covered in the Learning Agent Framework course:

- Tools & Function Calling
- Structured Output
- Vision AI (Receipt Parsing)
- Multi-Agent Orchestration
- RAG with Vector Stores
- Budget Management & Alerts
- Multi-platform deployment (Console, Telegram, Web API)

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │   Console    │  │   Telegram   │  │   Web API    │              │
│  │     App      │  │     Bot      │  │              │              │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │
└─────────┼─────────────────┼─────────────────┼───────────────────────┘
          │                 │                 │
          └─────────────────┴────────┬────────┘
                                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      EXPENSE TRACKER CORE                            │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                    AGENTS                                      │  │
│  │  ┌─────────────────┐                                          │  │
│  │  │   Orchestrator  │ ─── Analyzes intent, delegates to agents │  │
│  │  │     Agent       │                                          │  │
│  │  └────────┬────────┘                                          │  │
│  │           │                                                    │  │
│  │     ┌─────┴─────┬─────────────┐                               │  │
│  │     ▼           ▼             ▼                               │  │
│  │  ┌──────┐  ┌──────────┐  ┌──────────┐                        │  │
│  │  │Parser│  │ Receipt  │  │  Budget  │                        │  │
│  │  │Agent │  │  Agent   │  │  Tools   │                        │  │
│  │  └──────┘  └──────────┘  └──────────┘                        │  │
│  │  (Text)    (Vision)      (Alerts)                            │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                         TOOLS                                  │  │
│  │  AddExpense │ GetExpenses │ GetCategories │ SearchExpenses    │  │
│  │  SetBudget │ GetBudgetStatus │ GetBudgetAlerts                │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                       SERVICES                                 │  │
│  │  IExpenseService │ ICategoryService │ IBudgetService          │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │    SQLite    │  │    Qdrant    │  │    OpenAI    │              │
│  │  (Expenses,  │  │  (Semantic   │  │  (LLM +      │              │
│  │   Budgets)   │  │   Search)    │  │   Vision)    │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
ExpenseTracker/
├── src/
│   ├── ExpenseTracker.Core/              # Core Library
│   │   ├── Domain/
│   │   │   └── Entities/
│   │   │       ├── Expense.cs
│   │   │       ├── Category.cs
│   │   │       ├── User.cs
│   │   │       └── Budget.cs
│   │   │
│   │   ├── Agents/
│   │   │   ├── OrchestratorAgent.cs      # Main AI orchestrator
│   │   │   ├── ExpenseParserAgent.cs     # Text → Expense
│   │   │   └── ReceiptParserAgent.cs     # Image → Expense
│   │   │
│   │   ├── Tools/
│   │   │   ├── ExpenseTools.cs           # CRUD operations
│   │   │   └── BudgetTools.cs            # Budget management
│   │   │
│   │   ├── Services/
│   │   │   ├── IExpenseService.cs
│   │   │   ├── ExpenseService.cs
│   │   │   ├── ICategoryService.cs
│   │   │   ├── CategoryService.cs
│   │   │   ├── IBudgetService.cs
│   │   │   └── BudgetService.cs
│   │   │
│   │   ├── Models/
│   │   │   └── ParsedExpense.cs          # Structured output model
│   │   │
│   │   └── Abstractions/
│   │       ├── IExpenseRepository.cs
│   │       ├── ICategoryRepository.cs
│   │       ├── IBudgetRepository.cs
│   │       └── IVectorStore.cs
│   │
│   ├── ExpenseTracker.Infrastructure/    # Data Access
│   │   ├── Data/
│   │   │   ├── SqliteConnectionFactory.cs
│   │   │   └── DatabaseInitializer.cs
│   │   ├── Repositories/
│   │   │   ├── ExpenseRepository.cs
│   │   │   ├── CategoryRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   └── BudgetRepository.cs
│   │   ├── VectorStore/
│   │   │   ├── QdrantVectorStore.cs
│   │   │   └── NullVectorStore.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── ExpenseTracker.Console/           # Console Client
│   │   └── Program.cs
│   │
│   ├── ExpenseTracker.Telegram/          # Telegram Bot
│   │   ├── Handlers/
│   │   │   └── MessageHandler.cs
│   │   └── Program.cs
│   │
│   └── ExpenseTracker.Api/               # Web API
│       ├── Controllers/
│       │   ├── ExpensesController.cs
│       │   ├── ChatController.cs
│       │   ├── ReportsController.cs
│       │   └── CategoriesController.cs
│       └── Program.cs
│
└── docker-compose.yml                    # Qdrant for semantic search
```

## Features

| Feature | Description |
|---------|-------------|
| Natural Language Input | "I spent 45 EUR at the supermarket" |
| Receipt Scanning | Vision AI extracts data from receipt photos |
| Automatic Categorization | AI determines expense category |
| Budget Management | Set limits per category or globally |
| Budget Alerts | Warnings at 80%, exceeded, critical levels |
| Semantic Search | Find similar expenses with Qdrant |
| Multi-Platform | Console, Telegram Bot, REST API |

## Key Concepts Demonstrated

### 1. Structured Output (ParsedExpense)

```csharp
public record ParsedExpense
{
    public decimal Amount { get; init; }
    public string Description { get; init; }
    public string Category { get; init; }
    public string Date { get; init; }
    public string? Location { get; init; }
    public float Confidence { get; init; }
}

// Usage
var result = await agent.RunAsync<ParsedExpense>(userInput);
```

### 2. Tools / Function Calling

```csharp
public class ExpenseTools
{
    [Description("Adds a new expense to the database")]
    public async Task<string> AddExpense(
        [Description("Amount in EUR")] decimal amount,
        [Description("Brief description")] string description,
        [Description("Category ID")] string categoryId)
    {
        var expense = await _expenseService.AddExpenseAsync(...);
        return $"Expense saved: {expense.Id}";
    }
}

// Registration
var tools = new List<AITool>
{
    AIFunctionFactory.Create(_tools.AddExpense, "add_expense"),
    AIFunctionFactory.Create(_tools.GetRecentExpenses, "get_recent_expenses"),
    // ... more tools
};
```

### 3. Vision AI (Receipt Parsing)

```csharp
public class ReceiptParserAgent
{
    public async Task<ParseResult> ParseFromFileAsync(string imagePath)
    {
        var base64 = Convert.ToBase64String(File.ReadAllBytes(imagePath));

        var message = new ChatMessage(ChatRole.User, [
            ChatContentPart.CreateText("Extract expense data from this receipt"),
            ChatContentPart.CreateImage(BinaryData.FromBytes(imageBytes), mimeType)
        ]);

        return await _agent.RunAsync<ParsedExpense>(message);
    }
}
```

### 4. RAG with Vector Store

```csharp
public class QdrantVectorStore : IVectorStore
{
    public async Task UpsertExpenseAsync(string expenseId, string text, ...)
    {
        var embedding = await _embeddingGenerator.GenerateAsync(text);
        await _qdrantClient.UpsertAsync(_collectionName, [point]);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query, int limit = 10)
    {
        var queryVector = await _embeddingGenerator.GenerateAsync(query);
        return await _qdrantClient.SearchAsync(_collectionName, queryVector);
    }
}
```

### 5. Budget Alerts

```csharp
public async Task<IEnumerable<BudgetAlert>> CheckBudgetAlertsAsync(string userId)
{
    var statuses = await GetBudgetStatusAsync(userId);

    foreach (var status in statuses)
    {
        if (status.UsagePercentage >= 1.2m)
            yield return new BudgetAlert(status, BudgetAlertLevel.Critical, ...);
        else if (status.UsagePercentage >= 1.0m)
            yield return new BudgetAlert(status, BudgetAlertLevel.Exceeded, ...);
        else if (status.UsagePercentage >= 0.8m)
            yield return new BudgetAlert(status, BudgetAlertLevel.Warning, ...);
    }
}
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- OpenAI API Key
- Docker (optional, for Qdrant semantic search)
- Telegram Bot Token (optional, for Telegram bot)

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `OPENAI_API_KEY` | OpenAI API key | Yes |
| `OPENAI_MODEL` | Model to use (default: gpt-4o-mini) | No |
| `TELEGRAM_BOT_TOKEN` | Telegram bot token | For Telegram only |

### Running the Console App

```bash
cd ExpenseTracker/src/ExpenseTracker.Console
dotnet run
```

### Running the Telegram Bot

```bash
# Set Telegram token
$env:TELEGRAM_BOT_TOKEN = "your-bot-token"

cd ExpenseTracker/src/ExpenseTracker.Telegram
dotnet run
```

### Running the Web API

```bash
cd ExpenseTracker/src/ExpenseTracker.Api
dotnet run
```

Swagger UI available at: http://localhost:5000

### Starting Qdrant (for Semantic Search)

```bash
cd ExpenseTracker
docker compose up -d
```

## Default Categories

| ID | Name | Icon |
|----|------|------|
| food | Groceries | :shopping_cart: |
| restaurant | Restaurant | :fork_and_knife: |
| transport | Transport | :car: |
| fuel | Fuel | :fuelpump: |
| health | Health | :pill: |
| entertainment | Entertainment | :clapper: |
| shopping | Shopping | :shopping_bags: |
| bills | Bills | :page_facing_up: |
| home | Home | :house: |
| other | Other | :package: |

## API Endpoints

### Expenses
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/expenses` | List expenses (with filters) |
| GET | `/api/expenses/{id}` | Get expense by ID |
| POST | `/api/expenses` | Create expense |
| POST | `/api/expenses/from-text` | Create from natural language |
| DELETE | `/api/expenses/{id}` | Delete expense |

### Chat
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/chat` | Send message to AI assistant |
| DELETE | `/api/chat/{conversationId}` | Clear conversation |

### Reports
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/reports/summary` | Monthly summary |
| GET | `/api/reports/by-category` | Expenses by category |

### Categories
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` | List all categories |

## Telegram Commands

| Command | Description |
|---------|-------------|
| `/start` | Welcome message and registration |
| `/help` | Usage instructions |
| `/report` | Monthly expense summary |
| `/budget` | Budget status |
| `/categories` | List available categories |
| `/reset` | Reset conversation |

## Sample Interactions

### Console / Telegram / API Chat

```
User: I spent 45 euros at the supermarket
AI: [calls add_expense] Expense saved! 45.00 EUR for Groceries.

User: How much did I spend this month?
AI: [calls get_category_summary]
    📊 Summary for December 2024:
    🛒 Groceries: 245.50 EUR (12 expenses)
    🍽️ Restaurant: 89.00 EUR (3 expenses)
    ⛽ Fuel: 60.00 EUR (2 expenses)
    Total: 394.50 EUR

User: Set a budget of 500€ per month
AI: [calls set_budget] Budget set! Monthly limit of 500.00 EUR (global).

User: Am I within budget?
AI: [calls get_budget_status]
    🟢 You have 105.50 EUR remaining (79% used).
```

### Receipt Photo (Telegram)

```
User: [sends receipt photo]
AI: 📄 Receipt analyzed:
    💰 Amount: 32.50€
    📝 Supermarket purchase
    📁 Category: food
    📅 Date: 2024-12-24

    Would you like to save this expense?
    [✅ Save] [❌ Cancel]
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10 |
| AI Framework | Microsoft Agent Framework |
| LLM | OpenAI GPT-4o (text + vision) |
| Database | SQLite + Dapper |
| Vector Store | Qdrant |
| Telegram | Telegram.Bot |
| Web API | ASP.NET Core |
| Console UI | Spectre.Console |

## Implementation Phases

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Core Domain & Database | ✅ Complete |
| 2 | Expense Parser Agent (Structured Output) | ✅ Complete |
| 3 | Tools & Orchestrator Agent | ✅ Complete |
| 4 | Vision AI (Receipt Parsing) | ✅ Complete |
| 5 | Multi-Agent Orchestration | ✅ Complete |
| 6 | Telegram Bot | ✅ Complete |
| 7 | Web API | ✅ Complete |
| 8 | Semantic Search (RAG + Qdrant) | ✅ Complete |
| 9 | Budget & Alerts | ✅ Complete |
| 10 | Documentation | ✅ Complete |

## Best Practices Demonstrated

1. **Clean Architecture** - Separation of Core, Infrastructure, and Presentation
2. **Dependency Injection** - Services registered via extension methods
3. **Repository Pattern** - Data access abstraction
4. **Graceful Degradation** - Works without Qdrant (semantic search disabled)
5. **Tool Security** - Validated inputs, safe operations
6. **Error Handling** - Friendly error messages for users

## Related Resources

- [Microsoft Agent Framework](https://github.com/microsoft/agents)
- [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling)
- [OpenAI Vision](https://platform.openai.com/docs/guides/vision)
- [Qdrant Vector Database](https://qdrant.tech/)
- [Telegram Bot API](https://core.telegram.org/bots/api)

---

**This is a capstone project demonstrating all Microsoft Agent Framework concepts in a real-world application.**
