// ============================================================================
// 08. WORKFLOWS - NATIVE API
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBJECTIVE OF THIS PROJECT:
// Learn the NATIVE APIs of the framework for multi-agent orchestration.
// Direct comparison with project 07 (custom implementation).
//
// ============================================================================
// COMPARISON: CUSTOM vs NATIVE
// ============================================================================
//
// PROJECT 07 (Custom):                    PROJECT 08 (Native):
// ─────────────────────────────────────────────────────────────────────────────
// foreach + manual await                  AgentWorkflowBuilder.BuildSequential()
// Manual Task.WhenAll                     AgentWorkflowBuilder.BuildConcurrent()
// if/switch for routing                   HandoffsWorkflowBuilder with AIFunction
// Not implemented                         GroupChatWorkflowBuilder + Manager
//
// ADVANTAGES OF NATIVE APIs:
// - More concise and declarative code
// - Built-in error handling
// - Event streaming
// - Checkpointing and resume
// - Extensibility via custom Manager
//
// RUN WITH: dotnet run --project core/08.Workflows.Native
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
    // CONFIGURATION
    // ========================================================================

    private const string ChatModel = "gpt-4o-mini";

    // ========================================================================
    // ENTRY POINT
    // ========================================================================

    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleHelper.WriteTitle("08. Workflows Native");
        ConsoleHelper.WriteSubtitle("Framework Native APIs");

        // ====================================================================
        // SETUP
        // ====================================================================
        ConsoleHelper.WriteSeparator("Setup");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(ChatModel).AsIChatClient();

        Console.WriteLine("Client initialized.");
        Console.WriteLine();

        // ====================================================================
        // WORKFLOW MENU
        // ====================================================================
        ConsoleHelper.WriteSeparator("Choose Native Workflow");

        Console.WriteLine();
        Console.WriteLine("Available workflows (Native API):");
        Console.WriteLine("   [1] Sequential  - BuildSequential(): agent pipeline");
        Console.WriteLine("   [2] Concurrent  - BuildConcurrent(): agents in parallel");
        Console.WriteLine("   [3] Handoffs    - HandoffsBuilder: routing with function call");
        Console.WriteLine("   [4] GroupChat   - GroupChatBuilder: group conversation");
        Console.WriteLine();
        Console.Write("Choose (1-4): ");

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
                Console.WriteLine("Invalid choice, running Sequential...");
                await RunSequentialWorkflow(chatClient);
                break;
        }

        // ====================================================================
        // SUMMARY
        // ====================================================================
        ConsoleHelper.WriteSeparator("Summary");

        Console.WriteLine("In this project you learned:");
        Console.WriteLine("   1. AgentWorkflowBuilder - Factory for common workflows");
        Console.WriteLine("   2. BuildSequential() - Agent pipeline in series");
        Console.WriteLine("   3. BuildConcurrent() - Parallel fan-out/fan-in");
        Console.WriteLine("   4. HandoffsBuilder - Dynamic routing with AIFunction");
        Console.WriteLine("   5. GroupChatBuilder - Multi-agent chat with Manager");
        Console.WriteLine("   6. InProcessExecution.StreamAsync() - Streaming execution");
        Console.WriteLine("   7. WorkflowEvent - Events to monitor execution");
        Console.WriteLine();
        Console.WriteLine("Compare with project 07 to see the difference!");
    }

    // ========================================================================
    // 1. SEQUENTIAL WORKFLOW (NATIVE)
    // ========================================================================
    //
    // COMPARISON WITH PROJECT 07:
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
    // The framework automatically handles context passing!
    // ========================================================================

    private static async Task RunSequentialWorkflow(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Workflow: Sequential (Native)");

        Console.WriteLine();
        Console.WriteLine("HOW BuildSequential() WORKS:");
        Console.WriteLine("   1. Creates an agent pipeline");
        Console.WriteLine("   2. Each agent's output becomes the next one's input");
        Console.WriteLine("   3. The framework handles automatic context passing");
        Console.WriteLine();

        // We create specialized agents for a translation pipeline
        // This example translates: Italian -> English -> French -> Spanish
        var agents = new[]
        {
            CreateTranslationAgent("English", chatClient),
            CreateTranslationAgent("French", chatClient),
            CreateTranslationAgent("Spanish", chatClient)
        };

        // ================================================================
        // NATIVE API: BuildSequential
        // ================================================================
        // One line to create the workflow!
        // Compare with the manual foreach loop from project 07
        var workflow = AgentWorkflowBuilder.BuildSequential(agents);

        Console.Write("Enter a sentence in Italian: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            input = "Ciao mondo, come stai oggi?";
        }

        Console.WriteLine();
        Console.WriteLine($"Input: {input}");
        Console.WriteLine();

        // Execute with event streaming
        var messages = new List<ChatMessage> { new(ChatRole.User, input) };
        await RunWorkflowWithEvents(workflow, messages);
    }

    // ========================================================================
    // 2. CONCURRENT WORKFLOW (NATIVE)
    // ========================================================================
    //
    // COMPARISON WITH PROJECT 07:
    //
    // CUSTOM (07):                          NATIVE (08):
    // ─────────────────────────────────────────────────────────────────────
    // var tasks = new List<Task>();          var workflow = AgentWorkflowBuilder
    // foreach (var role in roles)                .BuildConcurrent(agents);
    // {
    //     tasks.Add(Task.Run(async () =>     // The framework handles:
    //         await member.AskAsync(prompt))); // - Automatic fan-out
    // }                                       // - Result aggregation
    // await Task.WhenAll(tasks);              // - Error handling
    //
    // ========================================================================

    private static async Task RunConcurrentWorkflow(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Workflow: Concurrent (Native)");

        Console.WriteLine();
        Console.WriteLine("HOW BuildConcurrent() WORKS:");
        Console.WriteLine("   1. All agents receive the same input");
        Console.WriteLine("   2. They work in parallel (fan-out)");
        Console.WriteLine("   3. Results are aggregated (fan-in)");
        Console.WriteLine();

        // We create 3 agents that translate in parallel
        var agents = new[]
        {
            CreateTranslationAgent("French", chatClient),
            CreateTranslationAgent("Spanish", chatClient),
            CreateTranslationAgent("German", chatClient)
        };

        // ================================================================
        // NATIVE API: BuildConcurrent
        // ================================================================
        // Automatic fan-out to all agents!
        var workflow = AgentWorkflowBuilder.BuildConcurrent(agents);

        Console.Write("Enter a sentence to translate into 3 languages: ");
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
    // 3. HANDOFFS WORKFLOW (NATIVE)
    // ========================================================================
    //
    // COMPARISON WITH PROJECT 07:
    //
    // CUSTOM (07):                          NATIVE (08):
    // ─────────────────────────────────────────────────────────────────────
    // var analysis = await teamLead          var workflow = AgentWorkflowBuilder
    //     .AskAsync(analysisPrompt);             .CreateHandoffBuilderWith(triage)
    //                                            .WithHandoffs(triage, specialists)
    // if (analysis.Contains("ARCHITECT"))        .WithHandoffs(specialists, triage)
    //     return Architect;                      .Build();
    // else if (analysis.Contains("DEVELOPER"))
    //     return Developer;                  // The framework:
    // ...                                    // - Injects AIFunction for handoff
    //                                        // - Intercepts calls
    // var result = await selected            // - Routes automatically
    //     .AskAsync(request);
    //
    // ADVANTAGE: Routing is declarative and the agent decides via function call!
    // ========================================================================

    private static async Task RunHandoffsWorkflow(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Workflow: Handoffs (Native)");

        Console.WriteLine();
        Console.WriteLine("HOW HandoffsBuilder WORKS:");
        Console.WriteLine("   1. A 'triage' agent receives the request");
        Console.WriteLine("   2. The framework injects AIFunction for each possible handoff");
        Console.WriteLine("   3. The agent calls handoff_to_<agent_id> to transfer");
        Console.WriteLine("   4. The framework automatically routes to the right agent");
        Console.WriteLine();

        // We create specialist agents
        var mathTutor = new ChatClientAgent(
            chatClient,
            """
            You are an expert math tutor.
            You explain mathematical concepts clearly with examples.
            You ONLY answer math questions.
            If the question is not about math, ask to be transferred.
            """,
            "math_tutor",
            "Math specialist");

        var historyTutor = new ChatClientAgent(
            chatClient,
            """
            You are an expert history tutor.
            You explain historical events with context and important dates.
            You ONLY answer history questions.
            If the question is not about history, ask to be transferred.
            """,
            "history_tutor",
            "History specialist");

        var codingTutor = new ChatClientAgent(
            chatClient,
            """
            You are an expert programming tutor in C# and .NET.
            You explain coding concepts with practical examples.
            You ONLY answer programming questions.
            If the question is not about coding, ask to be transferred.
            """,
            "coding_tutor",
            "Programming specialist");

        var triageAgent = new ChatClientAgent(
            chatClient,
            """
            You are a triage agent for a tutoring service.
            Analyze the user's question and transfer it to the appropriate specialist.

            Available specialists:
            - math_tutor: for math questions
            - history_tutor: for history questions
            - coding_tutor: for programming questions

            You MUST ALWAYS transfer the question to a specialist.
            Do not answer directly, always use handoff.
            """,
            "triage_agent",
            "Routes questions to specialists");

        // ================================================================
        // NATIVE API: HandoffsBuilder
        // ================================================================
        // Declarative definition of handoff relationships!
        var specialists = new[] { mathTutor, historyTutor, codingTutor };

        var workflow = AgentWorkflowBuilder
            .CreateHandoffBuilderWith(triageAgent)
            // Triage can pass to any specialist
            .WithHandoffs(triageAgent, specialists)
            // Specialists can return to triage
            .WithHandoffs(specialists, triageAgent)
            .Build();

        Console.WriteLine("Ask a question (math, history, or programming):");
        Console.WriteLine("Examples:");
        Console.WriteLine("   - How do you calculate the area of a circle?");
        Console.WriteLine("   - Who was Julius Caesar?");
        Console.WriteLine("   - How does async/await work in C#?");
        Console.WriteLine();

        // Conversation loop to see handoffs in action
        var messages = new List<ChatMessage>();

        while (true)
        {
            Console.Write("You: ");
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
    // 4. GROUPCHAT WORKFLOW (NATIVE)
    // ========================================================================
    //
    // NOT PRESENT IN PROJECT 07!
    //
    // GroupChat is an advanced pattern where:
    // - Multiple agents participate in a conversation
    // - A GroupChatManager decides who speaks
    // - Built-in: RoundRobinGroupChatManager (takes turns)
    // - Custom: you can create your own Manager for advanced logic
    //
    // ========================================================================

    private static async Task RunGroupChatWorkflow(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Workflow: GroupChat (Native)");

        Console.WriteLine();
        Console.WriteLine("HOW GroupChatBuilder WORKS:");
        Console.WriteLine("   1. Define chat participants");
        Console.WriteLine("   2. Choose a GroupChatManager (e.g.: RoundRobin)");
        Console.WriteLine("   3. Agents speak in turns or according to strategy");
        Console.WriteLine("   4. MaximumIterationCount limits turns");
        Console.WriteLine();

        // We create agents with different perspectives on the same topic
        var optimist = new ChatClientAgent(
            chatClient,
            """
            You are a convinced optimist. You always see the positive side of things.
            In discussions, you highlight opportunities and advantages.
            Answer concisely (2-3 sentences).
            """,
            "optimist",
            "Sees the positive side");

        var pessimist = new ChatClientAgent(
            chatClient,
            """
            You are a pragmatic pessimist. You always see risks and problems.
            In discussions, you highlight challenges and potential failures.
            Answer concisely (2-3 sentences).
            """,
            "pessimist",
            "Sees the risks");

        var realist = new ChatClientAgent(
            chatClient,
            """
            You are a balanced realist. You balance pros and cons objectively.
            In discussions, you try to synthesize different viewpoints.
            Answer concisely (2-3 sentences).
            If the others have already discussed enough, conclude with a summary.
            """,
            "realist",
            "Balances perspectives");

        // ================================================================
        // NATIVE API: GroupChatBuilder + RoundRobinGroupChatManager
        // ================================================================
        // The manager decides who speaks and when to stop
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
                new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 6  // Each agent speaks 2 times
                })
            .AddParticipants(optimist, pessimist, realist)
            .Build();

        Console.Write("Enter a topic to discuss: ");
        var topic = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = "Will artificial intelligence replace programmers?";
        }

        Console.WriteLine();
        Console.WriteLine($"Topic: {topic}");
        Console.WriteLine();
        Console.WriteLine("--- Discussion start ---");
        Console.WriteLine();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, $"Let's discuss this topic: {topic}")
        };

        await RunWorkflowWithEvents(workflow, messages);

        Console.WriteLine();
        Console.WriteLine("--- Discussion end ---");
    }

    // ========================================================================
    // HELPER: Workflow Execution with Events
    // ========================================================================
    //
    // InProcessExecution.StreamAsync() allows you to:
    // - Execute the workflow in-process
    // - Receive events in streaming (AgentRunUpdateEvent, etc.)
    // - Monitor which agent is speaking
    // - Get the final output (WorkflowOutputEvent)
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
        // Executes the workflow and returns an event stream
        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);

        // TurnToken starts execution and enables events
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        // Process events in streaming
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                // Event: update from an agent (text streaming)
                case AgentRunUpdateEvent updateEvent:
                    // Show agent ID when it changes
                    if (updateEvent.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = updateEvent.ExecutorId;
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[{updateEvent.ExecutorId}]");
                        Console.ResetColor();
                    }

                    // Show text in streaming
                    Console.Write(updateEvent.Update.Text);

                    // Show any function calls (for handoffs)
                    var functionCall = updateEvent.Update.Contents
                        .OfType<FunctionCallContent>()
                        .FirstOrDefault();

                    if (functionCall != null)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"   -> Call: {functionCall.Name}");
                        Console.ResetColor();
                    }
                    break;

                // Event: final workflow output
                case WorkflowOutputEvent outputEvent:
                    Console.WriteLine();
                    return outputEvent.As<List<ChatMessage>>() ?? new List<ChatMessage>();
            }
        }

        return new List<ChatMessage>();
    }

    // ========================================================================
    // HELPER: Translation Agent Creation
    // ========================================================================

    private static ChatClientAgent CreateTranslationAgent(
        string targetLanguage,
        IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            $"""
            You are a professional translator.
            You ALWAYS translate received text into {targetLanguage}.
            Answer ONLY with the translation, without explanations.
            If the text is already in {targetLanguage}, rewrite it anyway.
            """,
            $"translator_{targetLanguage.ToLower()}",
            $"Translates to {targetLanguage}");
    }
}
