// ============================================================================
// 07. DEV TEAM - MULTI-AGENT
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBJECTIVE OF THIS PROJECT:
// Learn multi-agent orchestration with specialized agents
// that collaborate to complete complex tasks.
//
// SCENARIO:
// An AI development team consisting of:
// - TeamLead: coordinates the work
// - Architect: designs the architecture
// - Developer: implements the code
// - Reviewer: reviews the result
//
// KEY CONCEPTS:
//
// 1. SPECIALIZATION:
//    Each agent has a specific role with dedicated competencies.
//    The system prompt defines personality and behavior.
//
// 2. ORCHESTRATION:
//    ┌─────────────────────────────────────────────────────────────────┐
//    │ SEQUENTIAL: A → B → C (pipeline)                                │
//    │ PARALLEL:   A ┬→ Aggregation                                    │
//    │              B ┘                                                │
//    │ ROUTING:     Analysis → Agent choice → Execution                │
//    └─────────────────────────────────────────────────────────────────┘
//
// 3. COMMUNICATION:
//    Agents communicate by passing context.
//    The output of one agent can become input for another.
//
// RUN WITH: dotnet run --project core/07.DevTeam.MultiAgent
// ============================================================================

using System.Text;
using Common;
using DevTeam.MultiAgent.Agents;
using DevTeam.MultiAgent.Orchestration;
using OpenAI;

namespace DevTeam.MultiAgent;

public static class Program
{
    // ========================================================================
    // CONFIGURAZIONE
    // ========================================================================

    private const string ChatModel = "gpt-4o-mini";

    // ========================================================================
    // ENTRY POINT
    // ========================================================================

    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleHelper.WriteTitle("07. DevTeam");
        ConsoleHelper.WriteSubtitle("Multi-Agent Orchestration");

        // ====================================================================
        // STEP 1: SETUP
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 1: Setup Team");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(ChatModel);

        // Create the orchestrator
        var orchestrator = new TeamOrchestrator(chatClient);

        // Register the callback for logs
        orchestrator.OnLog += message =>
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ResetColor();
        };

        // Initialize the full team
        orchestrator.InitializeFullTeam();

        Console.WriteLine();
        Console.WriteLine("Team inizializzato:");
        foreach (var member in orchestrator.GetAllMembers())
        {
            Console.WriteLine($"   {member}");
        }

        // ====================================================================
        // STEP 2: DEMO WORKFLOW
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 2: Scegli Workflow");

        Console.WriteLine();
        Console.WriteLine("Workflow disponibili:");
        Console.WriteLine("   [1] Sequenziale - Architect → Developer → Reviewer");
        Console.WriteLine("   [2] Parallelo - Tutti gli agenti in parallelo");
        Console.WriteLine("   [3] Routing - TeamLead decide chi coinvolgere");
        Console.WriteLine("   [4] Ciclo completo - Design → Implement → Review");
        Console.WriteLine("   [5] Chat libera - Parla con un membro specifico");
        Console.WriteLine();
        Console.Write("Scegli (1-5): ");

        var choice = Console.ReadLine()?.Trim();

        switch (choice)
        {
            case "1":
                await RunSequentialDemo(orchestrator);
                break;
            case "2":
                await RunParallelDemo(orchestrator);
                break;
            case "3":
                await RunRoutedDemo(orchestrator);
                break;
            case "4":
                await RunFullCycleDemo(orchestrator);
                break;
            case "5":
                await RunFreeChatDemo(orchestrator);
                break;
            default:
                Console.WriteLine("Invalid choice, running full cycle...");
                await RunFullCycleDemo(orchestrator);
                break;
        }

        // ====================================================================
        // SUMMARY
        // ====================================================================
        ConsoleHelper.WriteSeparator("Riepilogo");

        Console.WriteLine("📚 In questo progetto hai imparato:");
        Console.WriteLine("   1. Creare agenti specializzati con ruoli diversi");
        Console.WriteLine("   2. Orchestrazione sequenziale (pipeline)");
        Console.WriteLine("   3. Orchestrazione parallela (fan-out/fan-in)");
        Console.WriteLine("   4. Routing basato sul contenuto");
        Console.WriteLine("   5. Comunicazione tra agenti (passaggio contesto)");
        Console.WriteLine();
        Console.WriteLine("🎉 Hai completato il Learning Path!");
    }

    // ========================================================================
    // DEMO: WORKFLOW SEQUENZIALE
    // ========================================================================

    private static async Task RunSequentialDemo(TeamOrchestrator orchestrator)
    {
        ConsoleHelper.WriteSeparator("Demo: Workflow Sequenziale");

        Console.WriteLine();
        Console.WriteLine("In this workflow, agents work in sequence.");
        Console.WriteLine("The output of each becomes input for the next.");
        Console.WriteLine();
        Console.Write("Inserisci un requisito da elaborare: ");

        var requirement = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(requirement))
        {
            requirement = "Crea una classe per gestire una cache in memoria con TTL";
        }

        Console.WriteLine();

        var results = await orchestrator.RunSequentialAsync(
            requirement,
            TeamRole.Architect,
            TeamRole.Developer,
            TeamRole.Reviewer);

        // Show the results
        foreach (var result in results)
        {
            ConsoleHelper.WriteSeparator($"Risultato: {result.Member}");
            Console.WriteLine(result.Response);
            Console.WriteLine();
        }
    }

    // ========================================================================
    // DEMO: WORKFLOW PARALLELO
    // ========================================================================

    private static async Task RunParallelDemo(TeamOrchestrator orchestrator)
    {
        ConsoleHelper.WriteSeparator("Demo: Workflow Parallelo");

        Console.WriteLine();
        Console.WriteLine("In this workflow, all agents work in parallel.");
        Console.WriteLine("Useful for getting different perspectives on the same problem.");
        Console.WriteLine();
        Console.Write("Inserisci una domanda per il team: ");

        var question = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(question))
        {
            question = "Quali sono i pro e contro di usare microservizi vs monolite?";
        }

        Console.WriteLine();

        var results = await orchestrator.RunParallelAsync(
            question,
            TeamRole.Architect,
            TeamRole.Developer,
            TeamRole.Reviewer);

        // Show the results
        foreach (var result in results)
        {
            ConsoleHelper.WriteSeparator($"Prospettiva: {result.Member}");
            Console.WriteLine(result.Response);
            Console.WriteLine();
        }
    }

    // ========================================================================
    // DEMO: WORKFLOW CON ROUTING
    // ========================================================================

    private static async Task RunRoutedDemo(TeamOrchestrator orchestrator)
    {
        ConsoleHelper.WriteSeparator("Demo: Workflow con Routing");

        Console.WriteLine();
        Console.WriteLine("In this workflow, the TeamLead analyzes the request");
        Console.WriteLine("and decides which team member is most suitable.");
        Console.WriteLine();
        Console.Write("Inserisci una richiesta: ");

        var request = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(request))
        {
            request = "Ho bisogno di scrivere una funzione che calcoli il fattoriale";
        }

        Console.WriteLine();

        var result = await orchestrator.RunRoutedAsync(request);

        ConsoleHelper.WriteSeparator($"Risposta da: {result.Member}");
        Console.WriteLine(result.Response);
        Console.WriteLine();
    }

    // ========================================================================
    // DEMO: CICLO COMPLETO
    // ========================================================================

    private static async Task RunFullCycleDemo(TeamOrchestrator orchestrator)
    {
        ConsoleHelper.WriteSeparator("Demo: Ciclo di Sviluppo Completo");

        Console.WriteLine();
        Console.WriteLine("This workflow simulates a complete development cycle:");
        Console.WriteLine("   1. 🏗️ Architect designs the solution");
        Console.WriteLine("   2. 💻 Developer implements the code");
        Console.WriteLine("   3. 🔍 Reviewer reviews the result");
        Console.WriteLine();
        Console.Write("Inserisci un requisito da implementare: ");

        var requirement = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(requirement))
        {
            requirement = "Implementa un servizio di notifiche che supporti email e SMS";
        }

        Console.WriteLine();

        var results = await orchestrator.RunFullDevelopmentCycleAsync(requirement);

        // Show the results with formatting
        var phases = new[] { "DESIGN", "IMPLEMENTAZIONE", "CODE REVIEW" };

        for (int i = 0; i < results.Count; i++)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"   {results[i].Member} - {phases[i]}");
            Console.WriteLine($"   Durata: {results[i].Duration.TotalSeconds:F1}s");
            Console.WriteLine($"═══════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(results[i].Response);
        }
    }

    // ========================================================================
    // DEMO: CHAT LIBERA
    // ========================================================================

    private static async Task RunFreeChatDemo(TeamOrchestrator orchestrator)
    {
        ConsoleHelper.WriteSeparator("Demo: Chat Libera");

        Console.WriteLine();
        Console.WriteLine("Scegli con chi vuoi parlare:");
        Console.WriteLine("   [1] 👔 TeamLead");
        Console.WriteLine("   [2] 🏗️ Architect");
        Console.WriteLine("   [3] 💻 Developer");
        Console.WriteLine("   [4] 🔍 Reviewer");
        Console.WriteLine();
        Console.Write("Scegli (1-4): ");

        var roleChoice = Console.ReadLine()?.Trim();
        var role = roleChoice switch
        {
            "1" => TeamRole.TeamLead,
            "2" => TeamRole.Architect,
            "3" => TeamRole.Developer,
            "4" => TeamRole.Reviewer,
            _ => TeamRole.Developer
        };

        var member = orchestrator.GetMember(role);
        if (member == null)
        {
            Console.WriteLine("Member not found!");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"You are chatting with {member}");
        Console.WriteLine("Write 'exit' to quit.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Tu: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            Console.WriteLine();
            Console.Write($"{member}: ");

            await foreach (var chunk in member.AskStreamingAsync(input))
            {
                Console.Write(chunk);
            }

            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
