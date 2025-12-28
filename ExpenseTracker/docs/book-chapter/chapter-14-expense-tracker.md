# Chapter 14: Expense Tracker - A Complete Project

## Introduction

This chapter brings together all the concepts we've explored throughout the book into a complete, production-ready application. **Expense Tracker** is an AI-powered expense management system that demonstrates:

- Multi-agent orchestration
- Vision AI for receipt parsing
- Natural language understanding
- Multiple client interfaces (Console, Telegram, Web API)
- Conversation context management
- Tool-based agent interactions

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Domain Model](#domain-model)
4. [The Orchestrator Agent](#the-orchestrator-agent)
5. [Vision AI: Receipt Parsing](#vision-ai-receipt-parsing)
6. [Telegram Bot Integration](#telegram-bot-integration)
7. [Web API](#web-api)
8. [Deployment Considerations](#deployment-considerations)

---

## Project Overview

Expense Tracker allows users to:

- **Record expenses** using natural language ("I spent 45 euros at the supermarket")
- **Scan receipts** by sending photos that are automatically parsed
- **Query expenses** conversationally ("How much did I spend this month?")
- **Generate reports** with category breakdowns and trends

The application supports multiple interfaces, all sharing the same core AI agents and business logic.

---

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
│  │                         AGENTS                                 │  │
│  │  ┌─────────────────┐     ┌─────────────────┐                  │  │
│  │  │  Orchestrator   │     │ Receipt Parser  │                  │  │
│  │  │     Agent       │     │     Agent       │                  │  │
│  │  └────────┬────────┘     └─────────────────┘                  │  │
│  │           │                    (Vision AI)                     │  │
│  │           ▼                                                    │  │
│  │      Tools Layer                                               │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                       SERVICES                                 │  │
│  │  IExpenseService │ ICategoryService │ IUserRepository          │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                            │
│  ┌──────────────┐  ┌──────────────┐                                │
│  │    SQLite    │  │    OpenAI    │                                │
│  │  (Database)  │  │  (LLM +      │                                │
│  │              │  │   Vision)    │                                │
│  └──────────────┘  └──────────────┘                                │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Telegram Bot Integration

Telegram provides an excellent platform for AI-powered bots. Its rich media support (text, photos, inline keyboards), widespread adoption, and developer-friendly API make it ideal for conversational applications like Expense Tracker.

### Creating a Telegram Bot

Telegram offers an extremely accessible bot platform. Unlike other messaging services, it requires no approvals, business verifications, or fees. Any developer can create a bot in minutes.

#### Step 1: Start BotFather

**BotFather** is Telegram's official bot for creating and managing other bots. It's the only way to obtain a valid API token.

1. Open Telegram (mobile or desktop app)
2. Search for `@BotFather` in the search bar
3. Start a chat and click **Start**

#### Step 2: Create the Bot

Send the command:

```
/newbot
```

BotFather will ask for two pieces of information:

1. **Display name** - The name users will see (e.g., "Expense Tracker")
2. **Username** - Must be unique and end with `bot` (e.g., `ExpenseTracker_bot` or `MyExpenseTrackerBot`)

#### Step 3: Get the Token

After creation, BotFather responds with:

```
Done! Congratulations on your new bot. You will find it at t.me/YourBot_bot.

Use this token to access the HTTP API:
7123456789:AAHxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

Keep your token secure and store it safely.
```

> **Security Warning**: The token is like a password. Anyone who has it can control your bot. Never commit it to source code or share it publicly.

#### Step 4: Optional Configuration

BotFather offers additional commands to customize your bot:

| Command | Description |
|---------|-------------|
| `/setdescription` | Description visible in the bot's profile |
| `/setabouttext` | "About" text for the bot |
| `/setuserpic` | Profile picture |
| `/setcommands` | Command menu (suggestions) |

For our Expense Tracker, configure the suggested commands:

```
/setcommands
```

Then select your bot and send:

```
start - Start the bot
help - Show help
report - Monthly summary
categories - List categories
reset - New conversation
```

These will appear as suggestions when users type `/` in the chat.

#### Step 5: Application Configuration

Store the token as an environment variable:

**Windows (PowerShell)**:
```powershell
$env:TELEGRAM_BOT_TOKEN = "7123456789:AAHxxxxxxxx..."
```

**Windows (CMD)**:
```cmd
set TELEGRAM_BOT_TOKEN=7123456789:AAHxxxxxxxx...
```

**Linux/macOS**:
```bash
export TELEGRAM_BOT_TOKEN="7123456789:AAHxxxxxxxx..."
```

For permanent development setup, add it to a `.env` file or system environment variables.

### Polling vs Webhook Architecture

Telegram supports two modes for receiving messages:

| Mode | Pros | Cons | Use Case |
|------|------|------|----------|
| **Long Polling** | Simple, works anywhere, no network configuration | Slightly higher latency | Development, simple applications |
| **Webhook** | Real-time, more efficient | Requires public HTTPS, server configuration | Production |

In our implementation, we use **Long Polling** for simplicity:

```csharp
botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    receiverOptions: new ReceiverOptions
    {
        AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
        DropPendingUpdates = true
    },
    cancellationToken: cts.Token);
```

For production with webhooks, you would need a public HTTPS endpoint configured with `SetWebhookAsync()`.

### Multi-User Conversation Management

One of the key challenges in building a Telegram bot with AI is managing separate conversation contexts for each user. Our implementation uses `ConcurrentDictionary` for thread-safe, per-user state:

```csharp
// Per-user orchestrator agents and conversation threads
private readonly ConcurrentDictionary<long, OrchestratorAgent> _userAgents = new();
private readonly ConcurrentDictionary<long, AgentThread> _userThreads = new();
```

When a message arrives, we retrieve or create the agent and thread for that specific user:

```csharp
private OrchestratorAgent GetOrCreateAgent(string userId)
{
    var telegramId = long.Parse(userId.Split('-').Last());

    return _userAgents.GetOrAdd(telegramId, _ =>
        OrchestratorAgent.Create(
            _openAIClient,
            _expenseService,
            _categoryService,
            userId,
            _model));
}

private AgentThread GetOrCreateThread(long telegramId, OrchestratorAgent agent)
{
    return _userThreads.GetOrAdd(telegramId, _ => agent.GetNewThread());
}
```

This ensures that:
- Each user has their own agent instance with their user ID
- Conversation history is maintained per-user
- The bot can handle multiple concurrent users without context mixing

### Handling Different Message Types

The Telegram bot needs to handle various input types:

#### Text Messages
Route to the OrchestratorAgent for natural language processing:

```csharp
private async Task HandleTextMessageAsync(Message message, User user, CancellationToken ct)
{
    var agent = GetOrCreateAgent(user.Id);
    var thread = GetOrCreateThread(message.From!.Id, agent);

    var response = await agent.ProcessAsync(message.Text!, thread, ct);

    await _botClient.SendMessage(message.Chat.Id, response, cancellationToken: ct);
}
```

#### Photo Messages
Route to the ReceiptParserAgent for Vision AI processing:

```csharp
private async Task HandlePhotoAsync(Message message, User user, CancellationToken ct)
{
    // Get the largest photo
    var photo = message.Photo!.OrderByDescending(p => p.FileSize).First();

    // Download and convert to base64
    var file = await _botClient.GetFile(photo.FileId, ct);
    using var stream = new MemoryStream();
    await _botClient.DownloadFile(file.FilePath!, stream, ct);
    var base64 = Convert.ToBase64String(stream.ToArray());

    // Parse with Vision AI
    var result = await _receiptParser.ParseFromBase64Async(base64, "image/jpeg", ct);

    // Show result with confirmation buttons...
}
```

#### Inline Keyboard Callbacks
Handle user confirmations for parsed receipts:

```csharp
var keyboard = new InlineKeyboardMarkup(new[]
{
    new[]
    {
        InlineKeyboardButton.WithCallbackData("Save", "receipt_save"),
        InlineKeyboardButton.WithCallbackData("Cancel", "receipt_cancel")
    }
});
```

### Complete Message Handler Structure

```csharp
public async Task HandleUpdateAsync(Update update, CancellationToken ct)
{
    // Handle callback queries (button clicks)
    if (update.CallbackQuery != null)
    {
        await HandleCallbackQueryAsync(update.CallbackQuery, ct);
        return;
    }

    // Handle messages
    if (update.Message == null) return;

    var message = update.Message;
    var user = await EnsureUserExistsAsync(message.From!, ct);

    // Route to appropriate handler
    if (message.Photo != null)
    {
        await HandlePhotoAsync(message, user, ct);
    }
    else if (message.Text != null)
    {
        if (message.Text.StartsWith('/'))
            await HandleCommandAsync(message, user, ct);
        else
            await HandleTextMessageAsync(message, user, ct);
    }
}
```

This routing pattern ensures clean separation of concerns while maintaining a unified entry point for all Telegram updates.

---

## Vision AI: Receipt Parsing

*[Section to be expanded]*

---

## Web API

*[Section to be expanded]*

---

## Deployment Considerations

*[Section to be expanded]*

---
