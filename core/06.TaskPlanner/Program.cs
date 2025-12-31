// ============================================================================
// 06. TASK PLANNER
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBJECTIVE OF THIS PROJECT:
// Learn the Plan-Execute pattern for goal-oriented agents.
// The agent decomposes complex objectives into atomic and executable steps.
//
// SCENARIO:
// A TaskPlanner that receives objectives from the user, breaks them down
// into a structured plan, and executes them step by step with feedback.
//
// KEY CONCEPTS:
//
// 1. PLAN-EXECUTE PATTERN:
//    ┌─────────────────────────────────────────────────────────────────┐
//    │  PHASE 1: PLANNING                                              │
//    │  The agent analyzes the objective and creates a structured plan │
//    │  with atomic and verifiable steps.                              │
//    └─────────────────────────────────────────────────────────────────┘
//                              │
//                              ▼
//    ┌─────────────────────────────────────────────────────────────────┐
//    │  PHASE 2: EXECUTION                                             │
//    │  The agent executes each step, verifies the result,             │
//    │  and moves to the next one.                                     │
//    └─────────────────────────────────────────────────────────────────┘
//
// 2. ADVANTAGES:
//    - Transparency: the user sees exactly what the agent will do
//    - Control: ability to approve the plan before execution
//    - Retry: isolated failures allow retrying individual steps
//    - Monitoring: real-time progress tracking
//
// RUN WITH: dotnet run --project core/06.TaskPlanner
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
    // CONFIGURATION
    // ========================================================================

    /// <summary>
    /// Model to use. GPT-4o-mini supports tool calling well.
    /// </summary>
    private const string ChatModel = "gpt-4o-mini";

    // ========================================================================
    // ENTRY POINT
    // ========================================================================

    public static async Task Main()
    {
        // Important for correctly displaying emojis and special characters
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

        // Create the tools for planning
        var plannerTools = new PlannerTools();

        // Register the callback for logs
        plannerTools.OnLogMessage += message =>
        {
            // Show logs with gray color to distinguish them
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
        // STEP 3: CREATE THE PLANNER AGENT
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 3: Creazione TaskPlanner Agent");

        // System prompt for the TaskPlanner
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

        // Create the agent with the tools
        var chatClient = openAiClient.GetChatClient(ChatModel);
        var tools = plannerTools.GetTools().ToList();

        ChatClientAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Name = "TaskPlanner",
            ChatOptions = new ChatOptions
            {
                Temperature = 0.3f, // Low temperature for consistency
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

        // Create a conversation thread
        AgentThread thread = agent.GetNewThread();

        // Message counter
        int messageCount = 0;

        // Conversation loop
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

            // Special command to view status
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
                // For the first message, include the instructions as context
                var promptWithContext = messageCount == 0
                    ? $"[Contesto sistema: {systemPrompt}]\n\n{userInput}"
                    : userInput;

                // Invoke the agent
                ConsoleHelper.WriteAgentHeader();

                await foreach (var update in agent.RunStreamingAsync(promptWithContext, thread))
                {
                    ConsoleHelper.WriteStreamChunk(update.ToString());
                }

                ConsoleHelper.EndStreamLine();

                messageCount++;

                // Show the plan status if it exists
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
        // SUMMARY
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
