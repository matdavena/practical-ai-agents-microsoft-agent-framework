// ============================================================================
// EXPENSE TRACKER - Telegram Bot
// ============================================================================
// Telegram bot interface for the AI-powered expense tracker.
// Supports text messages (natural language) and photo messages (receipts).
//
// BOOK CHAPTER NOTE:
// This demonstrates:
// 1. Telegram Bot API integration with Long Polling
// 2. Multi-user support with automatic user registration
// 3. Photo handling for receipt parsing
// 4. Conversation context per user
// ============================================================================

using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Agents;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Core.Services;
using ExpenseTracker.Infrastructure;
using ExpenseTracker.Telegram.Handlers;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

// ============================================================================
// CONFIGURATION
// ============================================================================

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║           EXPENSE TRACKER - Telegram Bot                  ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Get configuration from environment variables
var telegramToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

if (string.IsNullOrEmpty(telegramToken))
{
    Console.WriteLine("ERROR: TELEGRAM_BOT_TOKEN environment variable is not set.");
    Console.WriteLine("Create a bot with @BotFather on Telegram to get a token.");
    return 1;
}

if (string.IsNullOrEmpty(openAiApiKey))
{
    Console.WriteLine("ERROR: OPENAI_API_KEY environment variable is not set.");
    return 1;
}

// Database path
var databasePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ExpenseTracker",
    "expenses.db");

Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"AI Model: {openAiModel}");
Console.WriteLine();

// ============================================================================
// SERVICES SETUP
// ============================================================================

// Configure DI
var services = new ServiceCollection();
services.AddExpenseTracker(databasePath);
var serviceProvider = services.BuildServiceProvider();

// Initialize database
Console.WriteLine("Initializing database...");
await serviceProvider.InitializeExpenseTrackerAsync();
Console.WriteLine("Database initialized!");

// Get services
var expenseService = serviceProvider.GetRequiredService<IExpenseService>();
var categoryService = serviceProvider.GetRequiredService<ICategoryService>();
var budgetService = serviceProvider.GetRequiredService<IBudgetService>();
var userRepository = serviceProvider.GetRequiredService<IUserRepository>();

// Create OpenAI client
var openAIClient = new OpenAIClient(openAiApiKey);

Console.WriteLine();

// ============================================================================
// TELEGRAM BOT SETUP
// ============================================================================

var botClient = new TelegramBotClient(telegramToken);

// Verify bot connection
var me = await botClient.GetMe();
Console.WriteLine($"Bot connected: @{me.Username} ({me.FirstName})");
Console.WriteLine();

// Create message handler
var messageHandler = new MessageHandler(
    botClient,
    openAIClient,
    expenseService,
    categoryService,
    budgetService,
    userRepository,
    openAiModel);

// ============================================================================
// START RECEIVING MESSAGES
// ============================================================================

using var cts = new CancellationTokenSource();

// Handle graceful shutdown
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down...");
    cts.Cancel();
};

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
    DropPendingUpdates = true // Don't process old messages
};

Console.WriteLine("Bot is running! Press Ctrl+C to stop.");
Console.WriteLine("Send messages to @" + me.Username + " to test.");
Console.WriteLine();

// Start long polling
botClient.StartReceiving(
    updateHandler: async (client, update, ct) =>
    {
        try
        {
            await messageHandler.HandleUpdateAsync(update, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling update: {ex.Message}");
        }
    },
    errorHandler: (client, exception, ct) =>
    {
        Console.WriteLine($"Telegram error: {exception.Message}");
        return Task.CompletedTask;
    },
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token);

// Wait until cancelled
try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
    // Expected when Ctrl+C is pressed
}

Console.WriteLine("Bot stopped.");
return 0;
