// ============================================================================
// 06. TASK PLANNER
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBIETTIVO DI QUESTO PROGETTO:
// Imparare il pattern Plan-Execute per agenti goal-oriented.
// L'agente decompone obiettivi complessi in step atomici ed eseguibili.
//
// SCENARIO:
// Un TaskPlanner che riceve obiettivi dall'utente, li decompone
// in un piano strutturato, e li esegue step by step con feedback.
//
// CONCETTI CHIAVE:
//
// 1. PATTERN PLAN-EXECUTE:
//    ┌─────────────────────────────────────────────────────────────────┐
//    │  FASE 1: PLANNING                                               │
//    │  L'agente analizza l'obiettivo e crea un piano strutturato      │
//    │  con step atomici e verificabili.                               │
//    └─────────────────────────────────────────────────────────────────┘
//                              │
//                              ▼
//    ┌─────────────────────────────────────────────────────────────────┐
//    │  FASE 2: EXECUTION                                              │
//    │  L'agente esegue ogni step, verifica il risultato,              │
//    │  e passa al successivo.                                         │
//    └─────────────────────────────────────────────────────────────────┘
//
// 2. VANTAGGI:
//    - Trasparenza: l'utente vede esattamente cosa farà l'agente
//    - Controllo: possibilità di approvare il piano prima dell'esecuzione
//    - Retry: fallimenti isolati permettono di ritentare singoli step
//    - Monitoraggio: tracking del progresso in tempo reale
//
// ESEGUI CON: dotnet run --project core/06.TaskPlanner
// ============================================================================

using System.Text;
using Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using TaskPlanner.Planning;

namespace TaskPlanner;

public static class Program
{
    // ========================================================================
    // CONFIGURAZIONE
    // ========================================================================

    /// <summary>
    /// Modello da usare. GPT-4o-mini supporta bene il tool calling.
    /// </summary>
    private const string ChatModel = "gpt-4o-mini";

    // ========================================================================
    // ENTRY POINT
    // ========================================================================

    public static async Task Main()
    {
        // Importante per visualizzare correttamente emoji e caratteri speciali
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleHelper.WriteTitle("06. TaskPlanner");
        ConsoleHelper.WriteSubtitle("Pattern Plan-Execute per obiettivi complessi");

        // ====================================================================
        // STEP 1: SETUP OPENAI CLIENT
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 1: Setup");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);

        Console.WriteLine($"✅ Client OpenAI configurato");
        Console.WriteLine($"   Model: {ChatModel}");

        // ====================================================================
        // STEP 2: SETUP PLANNER TOOLS
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 2: Setup Planner Tools");

        // Creiamo i tools per il planning
        var plannerTools = new PlannerTools();

        // Registriamo il callback per i log
        plannerTools.OnLogMessage += message =>
        {
            // Mostra i log con colore grigio per distinguerli
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ResetColor();
        };

        Console.WriteLine("✅ Planner tools configurati:");
        Console.WriteLine("   - create_plan: Crea un piano con step");
        Console.WriteLine("   - execute_next_step: Esegue il prossimo step");
        Console.WriteLine("   - get_plan_status: Verifica lo stato");
        Console.WriteLine("   - mark_step_failed: Segna step fallito");
        Console.WriteLine("   - abort_plan: Annulla il piano");

        // ====================================================================
        // STEP 3: CREA L'AGENTE PLANNER
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 3: Creazione TaskPlanner Agent");

        // System prompt per il TaskPlanner
        const string systemPrompt = """
            Sei un TaskPlanner, un agente specializzato nel pianificare ed eseguire task complessi.

            COME LAVORI:

            1. QUANDO RICEVI UN OBIETTIVO:
               - Analizza cosa serve per raggiungerlo
               - Usa 'create_plan' per creare un piano con step chiari
               - Ogni step deve essere atomico (una singola azione)
               - Ogni step deve essere in forma imperativa ("Crea...", "Scrivi...", etc.)

            2. DOPO AVER CREATO IL PIANO:
               - Esegui ogni step chiamando 'execute_next_step'
               - Per ogni step, descrivi brevemente cosa hai fatto
               - Continua fino a completare tutti gli step

            3. REGOLE IMPORTANTI:
               - SEMPRE crea un piano PRIMA di iniziare qualsiasi lavoro
               - NON saltare step
               - Se uno step fallisce, usa 'mark_step_failed' e decidi se continuare
               - Quando tutti gli step sono completi, conferma all'utente

            4. FORMATO STEP:
               - Brevi e chiari (2-5 parole)
               - Azione specifica e verificabile
               - Esempio: "Creare cartella progetto|Inizializzare repository Git|Scrivere README"

            Rispondi sempre in italiano.
            """;

        // Creiamo l'agente con i tools
        var chatClient = openAiClient.GetChatClient(ChatModel);
        var tools = plannerTools.GetTools().ToList();

        ChatClientAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Name = "TaskPlanner",
            ChatOptions = new ChatOptions
            {
                Temperature = 0.3f, // Bassa temperatura per consistenza
                Tools = tools.Cast<AITool>().ToList()
            }
        });

        Console.WriteLine("✅ TaskPlanner Agent creato con tool calling");

        // ====================================================================
        // STEP 4: DEMO INTERATTIVA
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 4: Demo Task Planning");

        Console.WriteLine();
        Console.WriteLine("🎯 Dai al TaskPlanner un obiettivo complesso, ad esempio:");
        Console.WriteLine("   - \"Crea un progetto .NET con unit test\"");
        Console.WriteLine("   - \"Prepara una presentazione sul machine learning\"");
        Console.WriteLine("   - \"Organizza un meeting di team\"");
        Console.WriteLine();
        Console.WriteLine("L'agente creerà un piano e lo eseguirà step by step!");
        Console.WriteLine("Scrivi 'exit' per uscire, 'status' per vedere lo stato del piano.");
        Console.WriteLine();

        // Crea un thread di conversazione
        AgentThread thread = agent.GetNewThread();

        // Contatore messaggi
        int messageCount = 0;

        // Loop di conversazione
        while (true)
        {
            Console.Write("Tu: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("👋 Arrivederci!");
                break;
            }

            // Comando speciale per vedere lo stato
            if (userInput.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                if (plannerTools.CurrentPlan != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(plannerTools.CurrentPlan.GetFullReport());
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Nessun piano attivo.");
                }
                continue;
            }

            Console.WriteLine();

            try
            {
                // Per il primo messaggio, includiamo le istruzioni come contesto
                var promptWithContext = messageCount == 0
                    ? $"[Contesto sistema: {systemPrompt}]\n\n{userInput}"
                    : userInput;

                // Invoca l'agente
                ConsoleHelper.WriteAgentHeader();

                await foreach (var update in agent.RunStreamingAsync(promptWithContext, thread))
                {
                    ConsoleHelper.WriteStreamChunk(update.ToString());
                }

                ConsoleHelper.EndStreamLine();

                messageCount++;

                // Mostra lo stato del piano se esiste
                if (plannerTools.CurrentPlan != null &&
                    plannerTools.CurrentPlan.Status == TaskPlanStatus.Completed)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("═══════════════════════════════════════════════════");
                    Console.WriteLine(plannerTools.CurrentPlan.GetSummary());
                    Console.WriteLine("═══════════════════════════════════════════════════");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Errore: {ex.Message}");
            }
        }

        // ====================================================================
        // RIEPILOGO
        // ====================================================================
        ConsoleHelper.WriteSeparator("Riepilogo");

        Console.WriteLine("📚 In questo progetto hai imparato:");
        Console.WriteLine("   1. Pattern Plan-Execute per agenti goal-oriented");
        Console.WriteLine("   2. Decomposizione di task complessi in step atomici");
        Console.WriteLine("   3. Tool calling per esecuzione controllata");
        Console.WriteLine("   4. Tracking del progresso e gestione errori");
        Console.WriteLine("   5. Stato condiviso tra tools e agente");
        Console.WriteLine();
        Console.WriteLine("🔜 Nel prossimo progetto: Multi-Agent orchestration!");
    }
}
