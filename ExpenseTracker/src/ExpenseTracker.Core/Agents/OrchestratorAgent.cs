// ============================================================================
// OrchestratorAgent
// ============================================================================
// Main AI agent that orchestrates expense tracking operations.
// Uses Tools/Function Calling to interact with the expense system.
//
// BOOK CHAPTER NOTE:
// This agent demonstrates:
// 1. Tool Registration - Exposing business operations as AI-callable functions
// 2. Intelligent Routing - LLM decides which tool to use based on user intent
// 3. Natural Language Interface - Users speak naturally, agent acts appropriately
// ============================================================================

using ExpenseTracker.Core.Services;
using ExpenseTracker.Core.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace ExpenseTracker.Core.Agents;

/// <summary>
/// Main orchestrator agent for the expense tracker.
/// Handles all user interactions and delegates to appropriate tools.
/// </summary>
public class OrchestratorAgent
{
    private readonly ChatClientAgent _agent;
    private readonly ExpenseTools _expenseTools;
    private readonly BudgetTools _budgetTools;
    private readonly string _todayDate;

    /// <summary>
    /// System prompt that defines the orchestrator's behavior.
    /// </summary>
    private static string GetSystemPrompt(string todayDate) => $"""
        Sei un assistente intelligente per la gestione delle spese personali.
        La data di oggi è: {todayDate}

        CAPACITA':
        - Registrare nuove spese dal linguaggio naturale
        - Mostrare spese recenti e storiche
        - Fornire riepiloghi per categoria
        - Rispondere a domande sulle spese
        - Cercare spese specifiche
        - Gestire budget (impostare limiti, verificare stato, alert)

        COMPORTAMENTO:
        - Rispondi sempre in italiano
        - Sii conciso ma amichevole
        - Usa gli strumenti disponibili per ogni operazione
        - Non inventare dati: usa sempre gli strumenti per recuperare informazioni reali
        - Quando l'utente descrive una spesa, estraila e salvala automaticamente
        - Dopo aver registrato una spesa, verifica se ci sono alert budget da segnalare

        PARSING SPESE:
        Quando l'utente descrive una spesa (es. "Ho speso 45€ al supermercato"):
        1. Estrai l'importo (numero)
        2. Determina la categoria appropriata
        3. Crea una descrizione breve
        4. Usa la data di oggi se non specificata
        5. Chiama AddExpense per salvare
        6. Controlla eventuali alert budget

        GESTIONE BUDGET:
        - "Imposta un budget di 500€ mensili" → SetBudget(500, "monthly", null)
        - "Budget di 200€ per ristorante" → SetBudget(200, "monthly", "restaurant")
        - "Come sto col budget?" → GetBudgetStatus()
        - "Quanto posso ancora spendere?" → GetRemainingBudget()
        - "Elimina il budget" → DeleteBudget()

        PERIODI BUDGET:
        - weekly: settimanale
        - monthly: mensile (default)
        - yearly: annuale

        CATEGORIE DISPONIBILI:
        - food: alimentari, supermercato, spesa
        - restaurant: ristorante, pranzo, cena fuori, bar, caffè
        - transport: trasporti, taxi, uber, treno, autobus, metro
        - fuel: benzina, diesel, carburante
        - health: farmacia, medico, salute, medicine
        - entertainment: cinema, teatro, concerti, netflix, spotify
        - shopping: abbigliamento, scarpe, accessori, elettronica
        - bills: bollette, utenze, luce, gas, acqua, internet, telefono
        - home: casa, mobili, riparazioni
        - other: tutto il resto

        INTERPRETAZIONE DATE:
        - "oggi" = {todayDate}
        - "ieri" = data di ieri rispetto a {todayDate}
        - "questo mese" = dal primo giorno del mese corrente a oggi
        - "mese scorso" = tutto il mese precedente

        ESEMPI DI INTERAZIONE:
        - "Ho speso 45€ al supermercato" → AddExpense(45, "Spesa supermercato", "food", today)
        - "Quanto ho speso questo mese?" → GetCategorySummary()
        - "Mostrami le ultime spese" → GetRecentExpenses()
        - "Spese di dicembre" → GetExpensesByDateRange(2024-12-01, 2024-12-31)
        - "Imposta budget 1000€ al mese" → SetBudget(1000, "monthly", null)
        - "Sono nel budget?" → GetBudgetAlerts() o GetBudgetStatus()
        """;

    /// <summary>
    /// Creates a new OrchestratorAgent.
    /// </summary>
    /// <param name="chatClient">The OpenAI chat client.</param>
    /// <param name="expenseService">The expense service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="budgetService">The budget service.</param>
    /// <param name="userId">The current user ID.</param>
    public OrchestratorAgent(
        ChatClient chatClient,
        IExpenseService expenseService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        string userId)
    {
        _todayDate = DateTime.Today.ToString("yyyy-MM-dd");
        _expenseTools = new ExpenseTools(expenseService, categoryService, userId);
        _budgetTools = new BudgetTools(budgetService, categoryService, userId);

        // Create AI tools from ExpenseTools and BudgetTools methods
        var aiTools = new List<AITool>
        {
            // Expense tools
            AIFunctionFactory.Create(_expenseTools.AddExpense, "add_expense"),
            AIFunctionFactory.Create(_expenseTools.GetRecentExpenses, "get_recent_expenses"),
            AIFunctionFactory.Create(_expenseTools.GetExpensesByDateRange, "get_expenses_by_date_range"),
            AIFunctionFactory.Create(_expenseTools.GetCategorySummary, "get_category_summary"),
            AIFunctionFactory.Create(_expenseTools.GetCategories, "get_categories"),
            AIFunctionFactory.Create(_expenseTools.GetTotalSpending, "get_total_spending"),
            AIFunctionFactory.Create(_expenseTools.SearchExpenses, "search_expenses"),

            // Budget tools
            AIFunctionFactory.Create(_budgetTools.SetBudget, "set_budget"),
            AIFunctionFactory.Create(_budgetTools.GetBudgetStatus, "get_budget_status"),
            AIFunctionFactory.Create(_budgetTools.GetBudgetAlerts, "get_budget_alerts"),
            AIFunctionFactory.Create(_budgetTools.GetRemainingBudget, "get_remaining_budget"),
            AIFunctionFactory.Create(_budgetTools.DeleteBudget, "delete_budget")
        };

        _agent = chatClient
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build()
            .CreateAIAgent(
                instructions: GetSystemPrompt(_todayDate),
                tools: aiTools,
                name: "ExpenseOrchestrator");
    }

    /// <summary>
    /// Creates a new OrchestratorAgent from an OpenAI client.
    /// </summary>
    public static OrchestratorAgent Create(
        OpenAIClient openAIClient,
        IExpenseService expenseService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        string userId,
        string model = "gpt-4o-mini")
    {
        var chatClient = openAIClient.GetChatClient(model);
        return new OrchestratorAgent(chatClient, expenseService, categoryService, budgetService, userId);
    }

    /// <summary>
    /// Processes a user message and returns the agent's response.
    /// </summary>
    /// <param name="message">The user's message.</param>
    /// <param name="thread">The conversation thread (for context).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's response.</returns>
    public async Task<string> ProcessAsync(
        string message,
        AgentThread? thread = null,
        CancellationToken cancellationToken = default)
    {
        thread ??= _agent.GetNewThread();

        var response = await _agent.RunAsync(message, thread);
        return response.ToString();
    }

    /// <summary>
    /// Processes a user message with streaming response.
    /// </summary>
    /// <param name="message">The user's message.</param>
    /// <param name="thread">The conversation thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of response chunks.</returns>
    public IAsyncEnumerable<string> ProcessStreamingAsync(
        string message,
        AgentThread? thread = null,
        CancellationToken cancellationToken = default)
    {
        thread ??= _agent.GetNewThread();
        return StreamResponseAsync(message, thread);
    }

    private async IAsyncEnumerable<string> StreamResponseAsync(string message, AgentThread thread)
    {
        await foreach (var update in _agent.RunStreamingAsync(message, thread))
        {
            yield return update.ToString();
        }
    }

    /// <summary>
    /// Gets a new conversation thread.
    /// </summary>
    public AgentThread GetNewThread() => _agent.GetNewThread();
}
