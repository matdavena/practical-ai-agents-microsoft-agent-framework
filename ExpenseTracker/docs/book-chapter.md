# Chapter 14: Expense Tracker - A Complete Project

> Building a production-ready AI-powered application with Microsoft Agent Framework

## 14.1 Introduction and Objectives

In this chapter, we bring together all the concepts learned throughout this book to build a complete, real-world application: **Expense Tracker** - an AI-powered personal expense management system.

### What Makes This Project Special

This isn't just another tutorial project. Expense Tracker is designed to be:

1. **Actually Useful** - A tool you can use daily for personal finance
2. **Multi-Interface** - Console, Telegram Bot, and REST API
3. **AI-Native** - Natural language input, receipt scanning, intelligent categorization
4. **Production-Ready** - Clean architecture, error handling, graceful degradation

### Concepts Covered

| Concept | How It's Used |
|---------|---------------|
| Structured Output | Parsing expenses from natural language |
| Tools/Function Calling | CRUD operations, budget management |
| Vision AI | Receipt photo analysis |
| RAG | Semantic expense search |
| Multi-Agent | Orchestrator delegating to specialized agents |
| DI & Architecture | Clean separation of concerns |

---

## 14.2 System Architecture

### Layered Architecture

We follow a classic layered architecture adapted for AI agents:

```
┌─────────────────────────────────────────────────────┐
│                 PRESENTATION LAYER                   │
│   Console App │ Telegram Bot │ Web API              │
└─────────────────────────┬───────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────┐
│                    AGENT LAYER                       │
│   OrchestratorAgent → ExpenseParser, ReceiptParser  │
│   Tools: ExpenseTools, BudgetTools                  │
└─────────────────────────┬───────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────┐
│                   SERVICE LAYER                      │
│   IExpenseService │ ICategoryService │ IBudgetService│
└─────────────────────────┬───────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────┐
│                INFRASTRUCTURE LAYER                  │
│   SQLite + Dapper │ Qdrant │ OpenAI                 │
└─────────────────────────────────────────────────────┘
```

### Why This Architecture?

1. **Testability** - Each layer can be tested independently
2. **Flexibility** - Swap implementations without changing business logic
3. **Scalability** - Add new interfaces without touching core logic
4. **AI Integration** - Agents sit between presentation and services

---

## 14.3 Domain-Driven Design for AI Agents

### Domain Entities

Our domain is simple but well-defined:

```csharp
public class Expense
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    public string CategoryId { get; set; }
    public DateTime Date { get; set; }
    public string? Location { get; set; }
    public ExpenseSource Source { get; set; }  // Manual, Text, Receipt
    public DateTime CreatedAt { get; set; }
}

public class Budget
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string? CategoryId { get; set; }  // null = global
    public decimal Amount { get; set; }
    public BudgetPeriod Period { get; set; }  // Weekly, Monthly, Yearly
    public bool IsActive { get; set; }
}
```

### Key Design Decisions

1. **String IDs** - Using GUIDs as strings for simplicity with SQLite
2. **Nullable CategoryId** - Allows global budgets
3. **ExpenseSource** - Tracks how the expense was created
4. **Value Objects** - BudgetPeriod as enum for type safety

---

## 14.4 Intelligent Parsing with Structured Output

### The Problem

Users want to say: "I spent 45 EUR at the supermarket yesterday"

We need to extract:
- Amount: 45.00
- Category: food
- Description: Supermarket purchase
- Date: Yesterday's date
- Location: Supermarket

### The Solution: Structured Output

```csharp
public record ParsedExpense
{
    [Description("The expense amount as a decimal number")]
    public decimal Amount { get; init; }

    [Description("A brief description of the expense")]
    public string Description { get; init; } = "";

    [Description("The category ID: food, restaurant, transport, etc.")]
    public string Category { get; init; } = "other";

    [Description("The date in yyyy-MM-dd format")]
    public string Date { get; init; } = "";

    [Description("Where the expense occurred (optional)")]
    public string? Location { get; init; }

    [Description("Confidence score from 0.0 to 1.0")]
    public float Confidence { get; init; }
}
```

### The Agent

```csharp
public class ExpenseParserAgent
{
    private const string SystemPrompt = """
        You are an expense parser. Extract structured data from natural language.

        Categories: food, restaurant, transport, fuel, health,
                   entertainment, shopping, bills, home, other

        For dates, interpret relative terms:
        - "today" = current date
        - "yesterday" = current date - 1
        - If no date mentioned, use today
        """;

    public async Task<ParseResult> ParseAsync(string text)
    {
        try
        {
            var result = await _agent.RunAsync<ParsedExpense>(text);
            return new ParseResult(true, result, null);
        }
        catch (Exception ex)
        {
            return new ParseResult(false, null, ex.Message);
        }
    }
}
```

### Why This Works

1. **Type Safety** - The LLM output is validated against our C# model
2. **Default Values** - Graceful handling of missing data
3. **Confidence Score** - Know when to ask for confirmation
4. **Category Mapping** - AI matches natural language to our predefined categories

---

## 14.5 Vision AI: Reading Receipts

### The Challenge

Receipt parsing is complex:
- Various formats and layouts
- Different languages
- Poor image quality
- Multiple items vs. total only

### Our Approach

We focus on extracting key information:
- Total amount
- Date
- Merchant name
- Inferred category

```csharp
public class ReceiptParserAgent
{
    private const string SystemPrompt = """
        You are a receipt parser with vision capabilities.

        Extract from the receipt image:
        1. Total amount (look for "TOTAL", "TOTALE", final amount)
        2. Date of purchase
        3. Merchant/store name
        4. Infer the category from the merchant type

        If you can't read something clearly, set confidence lower.
        For partial reads, extract what you can.
        """;

    public async Task<ParseResult> ParseFromBase64Async(
        string base64Image,
        string mimeType)
    {
        var message = new ChatMessage(ChatRole.User, [
            ChatContentPart.CreateText("Extract expense from this receipt"),
            ChatContentPart.CreateImage(
                BinaryData.FromBytes(Convert.FromBase64String(base64Image)),
                mimeType)
        ]);

        var result = await _agent.RunAsync<ParsedExpense>(message);
        return new ParseResult(true, result, null);
    }
}
```

### Best Practices for Vision AI

1. **Use GPT-4o** - Better OCR than gpt-4o-mini
2. **Handle Failures Gracefully** - Receipts are messy
3. **Request Confirmation** - Let users verify extracted data
4. **Provide Notes** - Agent can explain uncertainty

---

## 14.6 Multi-Agent Orchestration

### The Orchestrator Pattern

Instead of one monolithic agent, we use an orchestrator that:
1. Analyzes user intent
2. Decides which specialized agent/tool to use
3. Coordinates the response

```csharp
public class OrchestratorAgent
{
    private readonly ExpenseTools _expenseTools;
    private readonly BudgetTools _budgetTools;

    public OrchestratorAgent(...)
    {
        var aiTools = new List<AITool>
        {
            // Expense operations
            AIFunctionFactory.Create(_expenseTools.AddExpense),
            AIFunctionFactory.Create(_expenseTools.GetRecentExpenses),
            AIFunctionFactory.Create(_expenseTools.GetCategorySummary),

            // Budget operations
            AIFunctionFactory.Create(_budgetTools.SetBudget),
            AIFunctionFactory.Create(_budgetTools.GetBudgetStatus),
            AIFunctionFactory.Create(_budgetTools.GetBudgetAlerts),
        };

        _agent = chatClient.CreateAIAgent(
            instructions: GetSystemPrompt(),
            tools: aiTools);
    }
}
```

### The System Prompt

```
You are an intelligent expense management assistant.

CAPABILITIES:
- Record expenses from natural language
- Show recent expenses and history
- Provide category summaries
- Manage budgets and alerts

BEHAVIOR:
- Always respond in the user's language
- Use tools for all operations - don't invent data
- After recording an expense, check for budget alerts

PARSING EXPENSES:
When user describes an expense:
1. Extract amount
2. Determine appropriate category
3. Create brief description
4. Use today's date if not specified
5. Call AddExpense to save
6. Check budget alerts
```

---

## 14.7 Telegram Integration

### Why Telegram?

1. **Ubiquitous** - Works on any device
2. **Photo Support** - Easy receipt scanning
3. **Rich UI** - Inline keyboards for confirmation
4. **Persistent** - Conversations survive app restarts

### Message Handler Architecture

```csharp
public class MessageHandler
{
    // Per-user agent instances
    private readonly ConcurrentDictionary<long, OrchestratorAgent> _userAgents;
    private readonly ConcurrentDictionary<long, AgentThread> _userThreads;

    public async Task HandleUpdateAsync(Update update)
    {
        if (update.Message?.Photo != null)
            await HandlePhotoAsync(update.Message);
        else if (update.Message?.Text?.StartsWith('/') == true)
            await HandleCommandAsync(update.Message);
        else
            await HandleTextMessageAsync(update.Message);
    }
}
```

### Key Features

1. **Per-User Agents** - Each user has their own context
2. **Inline Keyboards** - Confirm/cancel receipt saves
3. **Budget Alerts** - Proactive notifications after saves
4. **Commands** - /report, /budget, /categories

---

## 14.8 RAG for Semantic Search

### The Value of Semantic Search

Traditional search: "Find expenses containing 'pizza'"
Semantic search: "Find expenses similar to eating out"

### Implementation with Qdrant

```csharp
public class QdrantVectorStore : IVectorStore
{
    public async Task UpsertExpenseAsync(
        string expenseId,
        string text,
        ExpenseVectorMetadata metadata)
    {
        // Generate embedding
        var embedding = await _embeddingGenerator.GenerateAsync(text);

        // Store with metadata
        var point = new PointStruct
        {
            Id = new PointId { Uuid = expenseId },
            Vectors = embedding.Vector.ToArray(),
            Payload = {
                ["description"] = metadata.Description,
                ["category"] = metadata.CategoryName,
                ["amount"] = metadata.Amount,
                // ... more metadata
            }
        };

        await _qdrantClient.UpsertAsync(_collectionName, [point]);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query,
        int limit = 10)
    {
        var queryVector = await _embeddingGenerator.GenerateAsync(query);

        return await _qdrantClient.SearchAsync(
            _collectionName,
            queryVector.Vector.ToArray(),
            limit: limit,
            scoreThreshold: 0.7f);
    }
}
```

### Graceful Degradation

```csharp
public class NullVectorStore : IVectorStore
{
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);
    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(...)
        => Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);
    // ... other methods return empty/no-op
}
```

This allows the application to work without Qdrant, just without semantic search.

---

## 14.9 Dependency Injection and Testing

### Service Registration

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddExpenseTracker(
        this IServiceCollection services,
        string databasePath)
    {
        // Infrastructure
        services.AddSingleton<IDbConnectionFactory>(
            SqliteConnectionFactory.CreateForFile(databasePath));
        services.AddSingleton<DatabaseInitializer>();

        // Repositories
        services.AddSingleton<IExpenseRepository, ExpenseRepository>();
        services.AddSingleton<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<IBudgetRepository, BudgetRepository>();

        // Services
        services.AddSingleton<IExpenseService, ExpenseService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IBudgetService, BudgetService>();

        return services;
    }

    public static IServiceCollection AddExpenseTrackerVectorStore(
        this IServiceCollection services,
        OpenAIClient openAIClient)
    {
        services.AddSingleton<IVectorStore>(
            new QdrantVectorStore("localhost", 6334, openAIClient));
        return services;
    }
}
```

### Testing Considerations

1. **Mock Repositories** - Test services without database
2. **Mock OpenAI** - Test agents without API calls
3. **NullVectorStore** - Test without Qdrant
4. **In-Memory SQLite** - Fast integration tests

---

## 14.10 Deployment and Production Considerations

### Environment Configuration

```bash
# Required
OPENAI_API_KEY=sk-...

# Optional
OPENAI_MODEL=gpt-4o-mini
TELEGRAM_BOT_TOKEN=...
```

### Docker Deployment

```yaml
# docker-compose.yml
services:
  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - qdrant_storage:/qdrant/storage

  expense-tracker-api:
    build: ./src/ExpenseTracker.Api
    environment:
      - OPENAI_API_KEY=${OPENAI_API_KEY}
    depends_on:
      - qdrant
```

### Production Checklist

- [ ] Use PostgreSQL instead of SQLite for multi-user
- [ ] Add authentication (JWT, API keys)
- [ ] Rate limiting on API endpoints
- [ ] Monitoring and logging
- [ ] Backup strategy for data
- [ ] HTTPS for all endpoints

---

## Summary

In this chapter, we built a complete AI-powered application that demonstrates:

1. **Clean Architecture** - Separating concerns properly
2. **Structured Output** - Type-safe AI responses
3. **Vision AI** - Processing images with AI
4. **Multi-Agent Design** - Orchestration and specialization
5. **RAG** - Semantic search capabilities
6. **Multi-Platform** - One core, many interfaces

The Expense Tracker isn't just a demo - it's a foundation you can extend and use. Consider adding:

- Recurring expenses
- Export to Excel/CSV
- Charts and visualizations
- Voice input (Whisper API)
- Expense sharing between users

---

## Exercises

1. **Add a new category** - "Travel" with appropriate keywords
2. **Weekly budget summary** - Telegram command for weekly stats
3. **Expense editing** - Allow modifying saved expenses via chat
4. **Multi-currency** - Support USD, GBP with conversion
5. **Expense predictions** - Use historical data to predict monthly spending

---

## Code Repository

Full source code available at:
`LearningAgentFramework/ExpenseTracker/`

Run with:
```bash
cd ExpenseTracker/src/ExpenseTracker.Console
dotnet run
```
