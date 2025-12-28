// ============================================================================
// MessageHandler
// ============================================================================
// Handles all incoming Telegram messages (text, photos, commands).
// Manages per-user conversation context and agent instances.
//
// BOOK CHAPTER NOTE:
// This demonstrates:
// 1. Per-user conversation state management
// 2. Command routing (/start, /help, /report)
// 3. Photo message handling with Vision AI
// 4. Integration of OrchestratorAgent and ReceiptParserAgent
// ============================================================================

using System.Collections.Concurrent;
using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Agents;
using ExpenseTracker.Core.Domain.Entities;
using ExpenseTracker.Core.Services;
using Microsoft.Agents.AI;
using OpenAI;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExpenseTracker.Telegram.Handlers;

/// <summary>
/// Handles all incoming Telegram updates (messages, commands, photos).
/// </summary>
public class MessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly OpenAIClient _openAIClient;
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly IBudgetService _budgetService;
    private readonly IUserRepository _userRepository;
    private readonly string _model;

    // Per-user orchestrator agents and conversation threads
    private readonly ConcurrentDictionary<long, OrchestratorAgent> _userAgents = new();
    private readonly ConcurrentDictionary<long, AgentThread> _userThreads = new();

    // Receipt parser (shared, stateless)
    private readonly ReceiptParserAgent _receiptParser;

    // Pending receipt confirmations
    private readonly ConcurrentDictionary<long, PendingReceipt> _pendingReceipts = new();

    public MessageHandler(
        ITelegramBotClient botClient,
        OpenAIClient openAIClient,
        IExpenseService expenseService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        IUserRepository userRepository,
        string model)
    {
        _botClient = botClient;
        _openAIClient = openAIClient;
        _expenseService = expenseService;
        _categoryService = categoryService;
        _budgetService = budgetService;
        _userRepository = userRepository;
        _model = model;

        // Create receipt parser (gpt-4o for better OCR)
        _receiptParser = ReceiptParserAgent.Create(openAIClient, "gpt-4o");
    }

    /// <summary>
    /// Handles an incoming Telegram update.
    /// </summary>
    public async Task HandleUpdateAsync(Update update, CancellationToken ct = default)
    {
        // Handle callback queries (button clicks)
        if (update.CallbackQuery != null)
        {
            await HandleCallbackQueryAsync(update.CallbackQuery, ct);
            return;
        }

        // Handle messages
        if (update.Message == null)
            return;

        var message = update.Message;
        var telegramUser = message.From;

        if (telegramUser == null)
            return;

        // Log the message
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] @{telegramUser.Username ?? telegramUser.Id.ToString()}: " +
            $"{(message.Photo != null ? "[PHOTO]" : message.Text ?? "[unknown]")}");

        // Ensure user exists in database
        var user = await EnsureUserExistsAsync(telegramUser, ct);

        // Route to appropriate handler
        if (message.Photo != null)
        {
            await HandlePhotoAsync(message, user, ct);
        }
        else if (message.Text != null)
        {
            if (message.Text.StartsWith('/'))
            {
                await HandleCommandAsync(message, user, ct);
            }
            else
            {
                await HandleTextMessageAsync(message, user, ct);
            }
        }
    }

    /// <summary>
    /// Ensures the Telegram user exists in the database.
    /// </summary>
    private async Task<Core.Domain.Entities.User> EnsureUserExistsAsync(
        global::Telegram.Bot.Types.User telegramUser,
        CancellationToken ct)
    {
        return await _userRepository.GetOrCreateFromTelegramAsync(
            telegramUser.Id,
            telegramUser.FirstName,
            telegramUser.LastName,
            telegramUser.Username,
            ct);
    }

    /// <summary>
    /// Gets or creates an OrchestratorAgent for a user.
    /// </summary>
    private OrchestratorAgent GetOrCreateAgent(long telegramId, string userId)
    {
        return _userAgents.GetOrAdd(telegramId, _ =>
            OrchestratorAgent.Create(
                _openAIClient,
                _expenseService,
                _categoryService,
                _budgetService,
                userId,
                _model));
    }

    /// <summary>
    /// Gets or creates a conversation thread for a user.
    /// </summary>
    private AgentThread GetOrCreateThread(long telegramId, OrchestratorAgent agent)
    {
        return _userThreads.GetOrAdd(telegramId, _ => agent.GetNewThread());
    }

    // =========================================================================
    // COMMAND HANDLERS
    // =========================================================================

    private async Task HandleCommandAsync(Message message, Core.Domain.Entities.User user, CancellationToken ct)
    {
        var command = message.Text!.Split(' ')[0].ToLowerInvariant();
        var chatId = message.Chat.Id;

        switch (command)
        {
            case "/start":
                await SendStartMessageAsync(chatId, user, ct);
                break;

            case "/help":
                await SendHelpMessageAsync(chatId, ct);
                break;

            case "/report":
                await SendReportAsync(chatId, user, ct);
                break;

            case "/categories":
                await SendCategoriesAsync(chatId, ct);
                break;

            case "/budget":
                await SendBudgetStatusAsync(chatId, user, ct);
                break;

            case "/reset":
                ResetUserConversation(message.From!.Id);
                await _botClient.SendMessage(chatId, "Conversazione resettata!", cancellationToken: ct);
                break;

            default:
                await _botClient.SendMessage(chatId,
                    "Comando non riconosciuto. Usa /help per vedere i comandi disponibili.",
                    cancellationToken: ct);
                break;
        }
    }

    private async Task SendStartMessageAsync(long chatId, Core.Domain.Entities.User user, CancellationToken ct)
    {
        var welcomeMessage = $"""
            Ciao {user.Name}! Sono il tuo assistente per la gestione delle spese.

            Puoi:
            - Scrivermi le spese in linguaggio naturale
              "Ho speso 45 euro al supermercato"

            - Inviarmi foto di scontrini
              Li analizzo automaticamente!

            - Chiedermi informazioni sulle tue spese
              "Quanto ho speso questo mese?"
              "Mostrami le ultime spese"

            - Gestire i tuoi budget
              "Imposta budget 500€ al mese"
              "Come sto col budget?"

            Comandi disponibili:
            /report - Riepilogo mensile
            /budget - Stato budget
            /categories - Lista categorie
            /reset - Nuova conversazione
            /help - Aiuto
            """;

        await _botClient.SendMessage(chatId, welcomeMessage, cancellationToken: ct);
    }

    private async Task SendHelpMessageAsync(long chatId, CancellationToken ct)
    {
        var helpMessage = """
            Come usare Expense Tracker:

            REGISTRARE SPESE:
            Scrivi semplicemente la spesa:
            - "Ho speso 45€ al supermercato"
            - "Cena ieri 32 euro"
            - "Benzina 50€"
            - "Netflix 12.99"

            Oppure invia una FOTO dello scontrino!

            VEDERE LE SPESE:
            - "Mostrami le ultime spese"
            - "Quanto ho speso questo mese?"
            - "Spese di dicembre"
            - "Riepilogo per categoria"

            GESTIRE BUDGET:
            - "Imposta budget 500€ al mese"
            - "Budget 200€ per ristorante"
            - "Come sto col budget?"
            - "Quanto posso ancora spendere?"

            COMANDI:
            /report - Riepilogo del mese
            /budget - Stato budget
            /categories - Lista categorie
            /reset - Nuova conversazione
            /help - Questo messaggio
            """;

        await _botClient.SendMessage(chatId, helpMessage, cancellationToken: ct);
    }

    private async Task SendReportAsync(long chatId, Core.Domain.Entities.User user, CancellationToken ct)
    {
        var today = DateTime.Today;
        var fromDate = new DateTime(today.Year, today.Month, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);

        var summary = await _expenseService.GetCategorySummaryAsync(user.Id, fromDate, toDate, ct);
        var summaryList = summary.ToList();

        if (summaryList.Count == 0)
        {
            await _botClient.SendMessage(chatId,
                $"Nessuna spesa registrata per {today:MMMM yyyy}.",
                cancellationToken: ct);
            return;
        }

        var report = $"📊 *Riepilogo {today:MMMM yyyy}*\n\n";

        foreach (var item in summaryList)
        {
            report += $"{item.CategoryIcon} {item.CategoryName}: *{item.TotalAmount:N2}€* ({item.ExpenseCount})\n";
        }

        var total = summaryList.Sum(s => s.TotalAmount);
        report += $"\n*Totale: {total:N2}€*";

        await _botClient.SendMessage(chatId, report, parseMode: ParseMode.Markdown, cancellationToken: ct);
    }

    private async Task SendCategoriesAsync(long chatId, CancellationToken ct)
    {
        var categories = await _categoryService.GetAllCategoriesAsync(ct);

        var message = "Categorie disponibili:\n\n";
        foreach (var category in categories)
        {
            message += $"{category.Icon} {category.Name}\n";
        }

        await _botClient.SendMessage(chatId, message, cancellationToken: ct);
    }

    private async Task SendBudgetStatusAsync(long chatId, Core.Domain.Entities.User user, CancellationToken ct)
    {
        var statuses = await _budgetService.GetBudgetStatusAsync(user.Id, ct);
        var statusList = statuses.ToList();

        if (statusList.Count == 0)
        {
            await _botClient.SendMessage(chatId,
                "Nessun budget configurato.\n\nPuoi impostarne uno scrivendo:\n\"Imposta budget 500€ al mese\"",
                cancellationToken: ct);
            return;
        }

        var message = "📊 *Stato Budget*\n\n";

        foreach (var status in statusList)
        {
            var statusIcon = status.IsOverBudget ? "🔴" : status.IsWarning ? "🟡" : "🟢";
            var remainingText = status.RemainingAmount >= 0
                ? $"Rimangono: *{status.RemainingAmount:N2}€*"
                : $"Sforato di: *{Math.Abs(status.RemainingAmount):N2}€*";

            message += $"{statusIcon} {status.CategoryIcon} *{status.CategoryName}* ({status.Period})\n";
            message += $"   Budget: {status.BudgetAmount:N2}€ | Speso: {status.SpentAmount:N2}€\n";
            message += $"   {remainingText} ({status.UsagePercentage:P0})\n\n";
        }

        // Check for alerts
        var alerts = await _budgetService.CheckBudgetAlertsAsync(user.Id, 0.8m, ct);
        var alertList = alerts.ToList();

        if (alertList.Count > 0)
        {
            message += "⚠️ *Avvisi:*\n";
            foreach (var alert in alertList)
            {
                message += $"• {alert.Message}\n";
            }
        }

        await _botClient.SendMessage(chatId, message, parseMode: ParseMode.Markdown, cancellationToken: ct);
    }

    /// <summary>
    /// Checks and sends budget alerts for a user after an expense is added.
    /// </summary>
    private async Task CheckAndSendBudgetAlertsAsync(long chatId, string userId, CancellationToken ct)
    {
        try
        {
            var alerts = await _budgetService.CheckBudgetAlertsAsync(userId, 0.8m, ct);
            var alertList = alerts.ToList();

            if (alertList.Count > 0)
            {
                var alertMessage = "⚠️ *Avviso Budget*\n\n";
                foreach (var alert in alertList)
                {
                    alertMessage += $"• {alert.Message}\n";
                }

                await _botClient.SendMessage(chatId, alertMessage, parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
        }
        catch
        {
            // Don't fail on alert check errors
        }
    }

    private void ResetUserConversation(long telegramId)
    {
        _userThreads.TryRemove(telegramId, out _);
    }

    // =========================================================================
    // TEXT MESSAGE HANDLER
    // =========================================================================

    private async Task HandleTextMessageAsync(Message message, Core.Domain.Entities.User user, CancellationToken ct)
    {
        var chatId = message.Chat.Id;
        var text = message.Text!;

        // Show typing indicator
        await _botClient.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

        try
        {
            // Get or create agent and thread for this user
            var agent = GetOrCreateAgent(message.From!.Id, user.Id);
            var thread = GetOrCreateThread(message.From.Id, agent);

            // Process with AI
            var response = await agent.ProcessAsync(text, thread, ct);

            // Send response
            await _botClient.SendMessage(chatId, response, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            await _botClient.SendMessage(chatId,
                "Mi dispiace, si è verificato un errore. Riprova più tardi.",
                cancellationToken: ct);
        }
    }

    // =========================================================================
    // PHOTO MESSAGE HANDLER
    // =========================================================================

    private async Task HandlePhotoAsync(Message message, Core.Domain.Entities.User user, CancellationToken ct)
    {
        var chatId = message.Chat.Id;

        // Get the largest photo
        var photo = message.Photo!.OrderByDescending(p => p.FileSize).First();

        await _botClient.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);
        await _botClient.SendMessage(chatId, "Analizzo lo scontrino...", cancellationToken: ct);

        try
        {
            // Download the photo
            var file = await _botClient.GetFile(photo.FileId, ct);
            using var stream = new MemoryStream();
            await _botClient.DownloadFile(file.FilePath!, stream, ct);

            // Convert to base64
            var base64 = Convert.ToBase64String(stream.ToArray());

            // Parse with Vision AI
            var result = await _receiptParser.ParseFromBase64Async(base64, "image/jpeg", "telegram_photo", ct);

            if (!result.Success)
            {
                await _botClient.SendMessage(chatId,
                    $"Non sono riuscito a leggere lo scontrino: {result.ErrorMessage}",
                    cancellationToken: ct);
                return;
            }

            var parsed = result.Expense!;

            // Store pending receipt for confirmation
            _pendingReceipts[message.From!.Id] = new PendingReceipt
            {
                UserId = user.Id,
                Expense = parsed
            };

            // Send result with confirmation buttons
            var resultMessage = $"""
                📄 *Scontrino analizzato:*

                💰 Importo: *{parsed.Amount:N2}€*
                📝 {parsed.Description}
                📁 Categoria: {parsed.Category}
                📅 Data: {parsed.Date}
                {(parsed.Location != null ? $"📍 {parsed.Location}" : "")}

                Vuoi salvare questa spesa?
                """;

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Salva", "receipt_save"),
                    InlineKeyboardButton.WithCallbackData("❌ Annulla", "receipt_cancel")
                }
            });

            await _botClient.SendMessage(chatId, resultMessage,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing photo: {ex.Message}");
            await _botClient.SendMessage(chatId,
                "Si è verificato un errore nell'analisi dello scontrino.",
                cancellationToken: ct);
        }
    }

    // =========================================================================
    // CALLBACK QUERY HANDLER (Button clicks)
    // =========================================================================

    private async Task HandleCallbackQueryAsync(CallbackQuery query, CancellationToken ct)
    {
        var chatId = query.Message!.Chat.Id;
        var userId = query.From.Id;

        await _botClient.AnswerCallbackQuery(query.Id, cancellationToken: ct);

        switch (query.Data)
        {
            case "receipt_save":
                if (_pendingReceipts.TryRemove(userId, out var pending))
                {
                    var expense = await _expenseService.AddExpenseAsync(
                        pending.UserId,
                        pending.Expense.Amount,
                        pending.Expense.Description,
                        pending.Expense.Category,
                        pending.Expense.ParsedDate,
                        pending.Expense.Location,
                        ExpenseSource.Receipt,
                        ct);

                    await _botClient.EditMessageText(chatId, query.Message.MessageId,
                        $"✅ Spesa salvata!\n\n💰 {expense.Amount:N2}€ - {expense.Description}",
                        cancellationToken: ct);

                    // Check for budget alerts after saving
                    await CheckAndSendBudgetAlertsAsync(chatId, pending.UserId, ct);
                }
                break;

            case "receipt_cancel":
                _pendingReceipts.TryRemove(userId, out _);
                await _botClient.EditMessageText(chatId, query.Message.MessageId,
                    "❌ Scontrino annullato.",
                    cancellationToken: ct);
                break;
        }
    }

    // =========================================================================
    // HELPER CLASSES
    // =========================================================================

    private class PendingReceipt
    {
        public required string UserId { get; init; }
        public required Core.Models.ParsedExpense Expense { get; init; }
    }
}
