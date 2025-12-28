// ============================================================================
// ExpenseParserAgent
// ============================================================================
// AI agent specialized in parsing expense information from natural language.
// Uses GPT-4o with Structured Output for reliable expense extraction.
//
// BOOK CHAPTER NOTE:
// This agent demonstrates:
// 1. Structured Output - Getting typed responses from AI
// 2. System Prompts - Defining agent behavior and constraints
// 3. Agent Factory Pattern - Creating specialized agents
// ============================================================================

using ExpenseTracker.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace ExpenseTracker.Core.Agents;

/// <summary>
/// Agent specialized in parsing expense information from natural language text.
/// </summary>
public class ExpenseParserAgent
{
    private readonly ChatClientAgent _agent;
    private readonly string _todayDate;

    /// <summary>
    /// System prompt that defines the agent's behavior.
    /// </summary>
    private static string GetSystemPrompt(string todayDate) => $"""
        Sei un assistente specializzato nell'estrazione di informazioni sulle spese dal testo in linguaggio naturale.

        Il tuo compito è analizzare il testo dell'utente e estrarre:
        1. **Importo**: La cifra spesa (in EUR)
        2. **Descrizione**: Una breve descrizione della spesa
        3. **Categoria**: Una delle seguenti categorie:
           - food (alimentari, supermercato, spesa)
           - restaurant (ristorante, pranzo, cena fuori, bar, caffè)
           - transport (trasporti, taxi, uber, treno, autobus, metro)
           - fuel (benzina, diesel, carburante, rifornimento)
           - health (farmacia, medico, salute, medicine)
           - entertainment (cinema, teatro, concerti, netflix, spotify, svago)
           - shopping (abbigliamento, scarpe, accessori, elettronica)
           - bills (bollette, utenze, luce, gas, acqua, internet, telefono)
           - home (casa, mobili, riparazioni, manutenzione)
           - other (altro, se non rientra nelle categorie precedenti)
        4. **Data**: La data della spesa (usa {todayDate} se non specificata)
        5. **Luogo**: Dove è stata fatta la spesa (se menzionato)

        REGOLE IMPORTANTI:
        - Se l'importo non è chiaro, chiedi chiarimenti (confidence basso)
        - Se la categoria è ambigua, usa quella più probabile e indica nelle note
        - Interpreta espressioni come "ieri", "lunedì scorso", "la settimana scorsa" rispetto a oggi ({todayDate})
        - Gli importi possono essere espressi come "45€", "45 euro", "quarantacinque euro"
        - Se il testo non contiene informazioni su una spesa, imposta confidence a 0

        ESEMPI:
        - "Ho speso 45€ al supermercato" → amount: 45, category: food, description: "Spesa al supermercato"
        - "Ieri cena da Mario, 32 euro" → amount: 32, category: restaurant, description: "Cena da Mario", date: ieri
        - "Benzina 50€" → amount: 50, category: fuel, description: "Rifornimento carburante"
        - "Netflix 12.99" → amount: 12.99, category: entertainment, description: "Abbonamento Netflix"
        """;

    /// <summary>
    /// Creates a new ExpenseParserAgent.
    /// </summary>
    /// <param name="chatClient">The OpenAI chat client to use.</param>
    public ExpenseParserAgent(ChatClient chatClient)
    {
        _todayDate = DateTime.Today.ToString("yyyy-MM-dd");

        _agent = chatClient
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build()
            .CreateAIAgent(
                instructions: GetSystemPrompt(_todayDate),
                name: "ExpenseParser");
    }

    /// <summary>
    /// Creates a new ExpenseParserAgent from an OpenAI client.
    /// </summary>
    /// <param name="openAIClient">The OpenAI client.</param>
    /// <param name="model">The model to use (default: gpt-4o-mini).</param>
    public static ExpenseParserAgent Create(OpenAIClient openAIClient, string model = "gpt-4o-mini")
    {
        var chatClient = openAIClient.GetChatClient(model);
        return new ExpenseParserAgent(chatClient);
    }

    /// <summary>
    /// Parses expense information from natural language text.
    /// </summary>
    /// <param name="text">The text to parse (e.g., "Ho speso 45€ al supermercato").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsing result containing the structured expense data.</returns>
    public async Task<ExpenseParseResult> ParseAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ExpenseParseResult.Fail("Il testo è vuoto", text);
        }

        try
        {
            // Create a new thread for this parsing request
            var thread = _agent.GetNewThread();

            // Use structured output to get a ParsedExpense
            var response = await _agent.RunAsync<ParsedExpense>(
                $"Analizza questa spesa: {text}",
                thread);

            var parsedExpense = response.Result;

            if (parsedExpense == null)
            {
                return ExpenseParseResult.Fail("Non sono riuscito a interpretare la spesa", text);
            }

            // Validate the result
            if (parsedExpense.Amount <= 0)
            {
                return ExpenseParseResult.Fail("Non ho trovato un importo valido nel testo", text);
            }

            if (parsedExpense.Confidence < 0.3f)
            {
                return ExpenseParseResult.Fail(
                    $"Non sono sicuro dell'interpretazione: {parsedExpense.Notes ?? "informazioni insufficienti"}",
                    text);
            }

            return ExpenseParseResult.Ok(parsedExpense, text);
        }
        catch (Exception ex)
        {
            return ExpenseParseResult.Fail($"Errore durante il parsing: {ex.Message}", text);
        }
    }

    /// <summary>
    /// Parses multiple expense texts at once.
    /// </summary>
    /// <param name="texts">The texts to parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsing results for each text.</returns>
    public async Task<IEnumerable<ExpenseParseResult>> ParseManyAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ExpenseParseResult>();

        foreach (var text in texts)
        {
            var result = await ParseAsync(text, cancellationToken);
            results.Add(result);
        }

        return results;
    }
}
