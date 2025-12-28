// ============================================================================
// 08. WORKFLOWS - NATIVE API
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBIETTIVO DI QUESTO PROGETTO:
// Imparare le API NATIVE del framework per l'orchestrazione multi-agente.
// Confronto diretto con il progetto 07 (implementazione custom).
//
// ============================================================================
// CONFRONTO: CUSTOM vs NATIVE
// ============================================================================
//
// PROGETTO 07 (Custom):                    PROGETTO 08 (Native):
// ─────────────────────────────────────────────────────────────────────────────
// foreach + await manuale                  AgentWorkflowBuilder.BuildSequential()
// Task.WhenAll manuale                     AgentWorkflowBuilder.BuildConcurrent()
// if/switch per routing                    HandoffsWorkflowBuilder con AIFunction
// Non implementato                         GroupChatWorkflowBuilder + Manager
//
// VANTAGGI DELLE API NATIVE:
// - Codice più conciso e dichiarativo
// - Gestione errori built-in
// - Streaming degli eventi
// - Checkpointing e resume
// - Estensibilità via custom Manager
//
// ESEGUI CON: dotnet run --project core/08.Workflows.Native
// ============================================================================

using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Workflows.Native;

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

        ConsoleHelper.WriteTitle("08. Workflows Native");
        ConsoleHelper.WriteSubtitle("API Native del Framework");

        // ====================================================================
        // SETUP
        // ====================================================================
        ConsoleHelper.WriteSeparator("Setup");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(ChatModel).AsIChatClient();

        Console.WriteLine("Client inizializzato.");
        Console.WriteLine();

        // ====================================================================
        // MENU WORKFLOW
        // ====================================================================
        ConsoleHelper.WriteSeparator("Scegli Workflow Nativo");

        Console.WriteLine();
        Console.WriteLine("Workflow disponibili (API Native):");
        Console.WriteLine("   [1] Sequential  - BuildSequential(): pipeline di agenti");
        Console.WriteLine("   [2] Concurrent  - BuildConcurrent(): agenti in parallelo");
        Console.WriteLine("   [3] Handoffs    - HandoffsBuilder: routing con function call");
        Console.WriteLine("   [4] GroupChat   - GroupChatBuilder: conversazione di gruppo");
        Console.WriteLine();
        Console.Write("Scegli (1-4): ");

        var choice = Console.ReadLine()?.Trim();

        switch (choice)
        {
            case "1":
                await RunSequentialWorkflow(chatClient);
                break;
            case "2":
                await RunConcurrentWorkflow(chatClient);
                break;
            case "3":
                await RunHandoffsWorkflow(chatClient);
                break;
            case "4":
                await RunGroupChatWorkflow(chatClient);
                break;
            default:
                Console.WriteLine("Scelta non valida, eseguo Sequential...");
                await RunSequentialWorkflow(chatClient);
                break;
        }

        // ====================================================================
        // RIEPILOGO
        // ====================================================================
        ConsoleHelper.WriteSeparator("Riepilogo");

        Console.WriteLine("In questo progetto hai imparato:");
        Console.WriteLine("   1. AgentWorkflowBuilder - Factory per workflow comuni");
        Console.WriteLine("   2. BuildSequential() - Pipeline di agenti in serie");
        Console.WriteLine("   3. BuildConcurrent() - Fan-out/fan-in parallelo");
        Console.WriteLine("   4. HandoffsBuilder - Routing dinamico con AIFunction");
        Console.WriteLine("   5. GroupChatBuilder - Chat multi-agente con Manager");
        Console.WriteLine("   6. InProcessExecution.StreamAsync() - Esecuzione streaming");
        Console.WriteLine("   7. WorkflowEvent - Eventi per monitorare l'esecuzione");
        Console.WriteLine();
        Console.WriteLine("Confronta con il progetto 07 per vedere la differenza!");
    }

    // ========================================================================
    // 1. SEQUENTIAL WORKFLOW (NATIVO)
    // ========================================================================
    //
    // CONFRONTO CON PROGETTO 07:
    //
    // CUSTOM (07):                          NATIVE (08):
    // ─────────────────────────────────────────────────────────────────────
    // var results = new List<TeamResult>();  var workflow = AgentWorkflowBuilder
    // var currentInput = initialPrompt;          .BuildSequential(agents);
    // foreach (var role in roles)
    // {                                      await using var run = await
    //     var response = await                   InProcessExecution.StreamAsync(
    //         member.AskAsync(currentInput);         workflow, messages);
    //     currentInput = response;
    // }
    //
    // Il framework gestisce automaticamente il passaggio del contesto!
    // ========================================================================

    private static async Task RunSequentialWorkflow(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Workflow: Sequential (Nativo)");

        Console.WriteLine();
        Console.WriteLine("COME FUNZIONA BuildSequential():");
        Console.WriteLine("   1. Crea una pipeline di agenti");
        Console.WriteLine("   2. L'output di ogni agente diventa input del successivo");
        Console.WriteLine("   3. Il framework gestisce il passaggio automatico del contesto");
        Console.WriteLine();

        // Creiamo agenti specializzati per una pipeline di traduzione
        // Questo esempio traduce: Italiano -> Inglese -> Francese -> Spagnolo
        var agents = new[]
        {
            CreateTranslationAgent("English", chatClient),
            CreateTranslationAgent("French", chatClient),
            CreateTranslationAgent("Spanish", chatClient)
        };

        // ================================================================
        // API NATIVA: BuildSequential
        // ================================================================
        // Una sola riga per creare il workflow!
        // Confronta con il ciclo foreach manuale del progetto 07
        var workflow = AgentWorkflowBuilder.BuildSequential(agents);

        Console.Write("Inserisci una frase in italiano: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            input = "Ciao mondo, come stai oggi?";
        }

        Console.WriteLine();
        Console.WriteLine($"Input: {input}");
        Console.WriteLine();

        // Esegui con streaming degli eventi
        var messages = new List<ChatMessage> { new(ChatRole.User, input) };
        await RunWorkflowWithEvents(workflow, messages);
    }

    // ========================================================================
    // 2. CONCURRENT WORKFLOW (NATIVO)
    // ========================================================================
    //
    // CONFRONTO CON PROGETTO 07:
    //
    // CUSTOM (07):                          NATIVE (08):
    // ─────────────────────────────────────────────────────────────────────
    // var tasks = new List<Task>();          var workflow = AgentWorkflowBuilder
    // foreach (var role in roles)                .BuildConcurrent(agents);
    // {
    //     tasks.Add(Task.Run(async () =>     // Il framework gestisce:
    //         await member.AskAsync(prompt))); // - Fan-out automatico
    // }                                       // - Aggregazione risultati
    // await Task.WhenAll(tasks);              // - Gestione errori
    //
    // ========================================================================

    private static async Task RunConcurrentWorkflow(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Workflow: Concurrent (Nativo)");

        Console.WriteLine();
        Console.WriteLine("COME FUNZIONA BuildConcurrent():");
        Console.WriteLine("   1. Tutti gli agenti ricevono lo stesso input");
        Console.WriteLine("   2. Lavorano in parallelo (fan-out)");
        Console.WriteLine("   3. I risultati vengono aggregati (fan-in)");
        Console.WriteLine();

        // Creiamo 3 agenti che traducono in parallelo
        var agents = new[]
        {
            CreateTranslationAgent("French", chatClient),
            CreateTranslationAgent("Spanish", chatClient),
            CreateTranslationAgent("German", chatClient)
        };

        // ================================================================
        // API NATIVA: BuildConcurrent
        // ================================================================
        // Fan-out automatico a tutti gli agenti!
        var workflow = AgentWorkflowBuilder.BuildConcurrent(agents);

        Console.Write("Inserisci una frase da tradurre in 3 lingue: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            input = "Hello, how are you today?";
        }

        Console.WriteLine();
        Console.WriteLine($"Input: {input}");
        Console.WriteLine();

        var messages = new List<ChatMessage> { new(ChatRole.User, input) };
        await RunWorkflowWithEvents(workflow, messages);
    }

    // ========================================================================
    // 3. HANDOFFS WORKFLOW (NATIVO)
    // ========================================================================
    //
    // CONFRONTO CON PROGETTO 07:
    //
    // CUSTOM (07):                          NATIVE (08):
    // ─────────────────────────────────────────────────────────────────────
    // var analysis = await teamLead          var workflow = AgentWorkflowBuilder
    //     .AskAsync(analysisPrompt);             .CreateHandoffBuilderWith(triage)
    //                                            .WithHandoffs(triage, specialists)
    // if (analysis.Contains("ARCHITECT"))        .WithHandoffs(specialists, triage)
    //     return Architect;                      .Build();
    // else if (analysis.Contains("DEVELOPER"))
    //     return Developer;                  // Il framework:
    // ...                                    // - Inietta AIFunction per handoff
    //                                        // - Intercetta le chiamate
    // var result = await selected            // - Ruota automaticamente
    //     .AskAsync(request);
    //
    // VANTAGGIO: Il routing è dichiarativo e l'agente decide via function call!
    // ========================================================================

    private static async Task RunHandoffsWorkflow(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Workflow: Handoffs (Nativo)");

        Console.WriteLine();
        Console.WriteLine("COME FUNZIONA HandoffsBuilder:");
        Console.WriteLine("   1. Un agente 'triage' riceve la richiesta");
        Console.WriteLine("   2. Il framework inietta AIFunction per ogni handoff possibile");
        Console.WriteLine("   3. L'agente chiama handoff_to_<agent_id> per trasferire");
        Console.WriteLine("   4. Il framework ruota automaticamente all'agente giusto");
        Console.WriteLine();

        // Creiamo agenti specialisti
        var mathTutor = new ChatClientAgent(
            chatClient,
            """
            Sei un tutor di matematica esperto.
            Spieghi concetti matematici in modo chiaro con esempi.
            Rispondi SOLO a domande di matematica.
            Se la domanda non è di matematica, chiedi di essere trasferito.
            """,
            "math_tutor",
            "Specialista in matematica");

        var historyTutor = new ChatClientAgent(
            chatClient,
            """
            Sei un tutor di storia esperto.
            Spieghi eventi storici con contesto e date importanti.
            Rispondi SOLO a domande di storia.
            Se la domanda non è di storia, chiedi di essere trasferito.
            """,
            "history_tutor",
            "Specialista in storia");

        var codingTutor = new ChatClientAgent(
            chatClient,
            """
            Sei un tutor di programmazione esperto in C# e .NET.
            Spieghi concetti di coding con esempi pratici.
            Rispondi SOLO a domande di programmazione.
            Se la domanda non è di coding, chiedi di essere trasferito.
            """,
            "coding_tutor",
            "Specialista in programmazione");

        var triageAgent = new ChatClientAgent(
            chatClient,
            """
            Sei un agente di smistamento per un servizio di tutoring.
            Analizza la domanda dell'utente e trasferiscila allo specialista appropriato.

            Specialisti disponibili:
            - math_tutor: per domande di matematica
            - history_tutor: per domande di storia
            - coding_tutor: per domande di programmazione

            DEVI SEMPRE trasferire la domanda a uno specialista.
            Non rispondere direttamente, usa sempre handoff.
            """,
            "triage_agent",
            "Smista le domande agli specialisti");

        // ================================================================
        // API NATIVA: HandoffsBuilder
        // ================================================================
        // Definizione dichiarativa delle relazioni di handoff!
        var specialists = new[] { mathTutor, historyTutor, codingTutor };

        var workflow = AgentWorkflowBuilder
            .CreateHandoffBuilderWith(triageAgent)
            // Triage può passare a qualsiasi specialista
            .WithHandoffs(triageAgent, specialists)
            // Gli specialisti possono tornare al triage
            .WithHandoffs(specialists, triageAgent)
            .Build();

        Console.WriteLine("Fai una domanda (matematica, storia, o programmazione):");
        Console.WriteLine("Esempi:");
        Console.WriteLine("   - Come si calcola l'area di un cerchio?");
        Console.WriteLine("   - Chi era Giulio Cesare?");
        Console.WriteLine("   - Come funziona async/await in C#?");
        Console.WriteLine();

        // Loop di conversazione per vedere i handoff in azione
        var messages = new List<ChatMessage>();

        while (true)
        {
            Console.Write("Tu: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) ||
                input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            messages.Add(new ChatMessage(ChatRole.User, input));

            Console.WriteLine();
            var outputMessages = await RunWorkflowWithEvents(workflow, messages);
            messages.AddRange(outputMessages);
            Console.WriteLine();
        }
    }

    // ========================================================================
    // 4. GROUPCHAT WORKFLOW (NATIVO)
    // ========================================================================
    //
    // NON PRESENTE NEL PROGETTO 07!
    //
    // GroupChat è un pattern avanzato dove:
    // - Più agenti partecipano a una conversazione
    // - Un GroupChatManager decide chi parla
    // - Built-in: RoundRobinGroupChatManager (a turno)
    // - Custom: puoi creare il tuo Manager per logiche avanzate
    //
    // ========================================================================

    private static async Task RunGroupChatWorkflow(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Workflow: GroupChat (Nativo)");

        Console.WriteLine();
        Console.WriteLine("COME FUNZIONA GroupChatBuilder:");
        Console.WriteLine("   1. Definisci i partecipanti alla chat");
        Console.WriteLine("   2. Scegli un GroupChatManager (es: RoundRobin)");
        Console.WriteLine("   3. Gli agenti parlano a turno o secondo la strategia");
        Console.WriteLine("   4. MaximumIterationCount limita i turni");
        Console.WriteLine();

        // Creiamo agenti con prospettive diverse sullo stesso tema
        var optimist = new ChatClientAgent(
            chatClient,
            """
            Sei un ottimista convinto. Vedi sempre il lato positivo delle cose.
            Nelle discussioni, evidenzi opportunità e vantaggi.
            Rispondi in modo conciso (2-3 frasi).
            """,
            "optimist",
            "Vede il lato positivo");

        var pessimist = new ChatClientAgent(
            chatClient,
            """
            Sei un pessimista pragmatico. Vedi sempre i rischi e i problemi.
            Nelle discussioni, evidenzi sfide e potenziali fallimenti.
            Rispondi in modo conciso (2-3 frasi).
            """,
            "pessimist",
            "Vede i rischi");

        var realist = new ChatClientAgent(
            chatClient,
            """
            Sei un realista equilibrato. Bilanci pro e contro oggettivamente.
            Nelle discussioni, cerchi di sintetizzare i diversi punti di vista.
            Rispondi in modo conciso (2-3 frasi).
            Se gli altri hanno già discusso abbastanza, concludi con un riassunto.
            """,
            "realist",
            "Bilancia le prospettive");

        // ================================================================
        // API NATIVA: GroupChatBuilder + RoundRobinGroupChatManager
        // ================================================================
        // Il manager decide chi parla e quando fermarsi
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
                new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 6  // Ogni agente parla 2 volte
                })
            .AddParticipants(optimist, pessimist, realist)
            .Build();

        Console.Write("Inserisci un tema da discutere: ");
        var topic = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = "L'intelligenza artificiale sostituirà i programmatori?";
        }

        Console.WriteLine();
        Console.WriteLine($"Tema: {topic}");
        Console.WriteLine();
        Console.WriteLine("--- Inizio discussione ---");
        Console.WriteLine();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, $"Discutiamo questo tema: {topic}")
        };

        await RunWorkflowWithEvents(workflow, messages);

        Console.WriteLine();
        Console.WriteLine("--- Fine discussione ---");
    }

    // ========================================================================
    // HELPER: Esecuzione Workflow con Eventi
    // ========================================================================
    //
    // InProcessExecution.StreamAsync() permette di:
    // - Eseguire il workflow in-process
    // - Ricevere eventi in streaming (AgentRunUpdateEvent, etc.)
    // - Monitorare quale agente sta parlando
    // - Ottenere l'output finale (WorkflowOutputEvent)
    //
    // ========================================================================

    private static async Task<List<ChatMessage>> RunWorkflowWithEvents(
        Workflow workflow,
        List<ChatMessage> messages)
    {
        string? lastExecutorId = null;

        // ================================================================
        // InProcessExecution.StreamAsync
        // ================================================================
        // Esegue il workflow e restituisce uno stream di eventi
        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);

        // TurnToken avvia l'esecuzione e abilita gli eventi
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        // Processiamo gli eventi in streaming
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                // Evento: aggiornamento da un agente (streaming del testo)
                case AgentRunUpdateEvent updateEvent:
                    // Mostra l'ID dell'agente quando cambia
                    if (updateEvent.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = updateEvent.ExecutorId;
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[{updateEvent.ExecutorId}]");
                        Console.ResetColor();
                    }

                    // Mostra il testo in streaming
                    Console.Write(updateEvent.Update.Text);

                    // Mostra eventuali function call (per handoffs)
                    var functionCall = updateEvent.Update.Contents
                        .OfType<FunctionCallContent>()
                        .FirstOrDefault();

                    if (functionCall != null)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"   -> Chiamata: {functionCall.Name}");
                        Console.ResetColor();
                    }
                    break;

                // Evento: output finale del workflow
                case WorkflowOutputEvent outputEvent:
                    Console.WriteLine();
                    return outputEvent.As<List<ChatMessage>>() ?? new List<ChatMessage>();
            }
        }

        return new List<ChatMessage>();
    }

    // ========================================================================
    // HELPER: Creazione Agente di Traduzione
    // ========================================================================

    private static ChatClientAgent CreateTranslationAgent(
        string targetLanguage,
        IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            $"""
            Sei un traduttore professionale.
            Traduci SEMPRE il testo ricevuto in {targetLanguage}.
            Rispondi SOLO con la traduzione, senza spiegazioni.
            Se il testo è già in {targetLanguage}, riscrivilo comunque.
            """,
            $"translator_{targetLanguage.ToLower()}",
            $"Traduce in {targetLanguage}");
    }
}
