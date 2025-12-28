// ============================================================================
// 01c. HELLO AGENT - OPENAI RESPONSE API
// ============================================================================
// This project demonstrates OpenAI's Response API, an evolution of Chat
// Completions that provides stateful conversations, built-in hosted tools,
// and better performance for agentic applications.
//
// KEY DIFFERENCES FROM CHAT COMPLETIONS:
// - Stateful: Use previous_response_id instead of full conversation history
// - Built-in Tools: Web Search, Code Interpreter, File Search, MCP servers
// - Better Caching: 40-80% improved cache utilization
// - Reasoning Access: Get reasoning summaries from reasoning models
// ============================================================================

using System.ClientModel;
using System.Text;
using Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

// Suppress preview warnings for Response API
#pragma warning disable OPENAI001

// ============================================================================
// CONFIGURATION
// ============================================================================
Console.OutputEncoding = Encoding.UTF8;

string apiKey;
try
{
    apiKey = ConfigurationHelper.GetOpenAiApiKey();
}
catch (Exception ex)
{
    ConsoleHelper.WriteError("OpenAI API key not configured!");
    ConsoleHelper.WriteInfo("Set OPENAI_API_KEY environment variable or configure in user secrets.");
    Console.WriteLine(ex.Message);
    return;
}

string model = ConfigurationHelper.GetOpenAiModel();

// ============================================================================
// MAIN MENU
// ============================================================================
while (true)
{
    Console.Clear();
    ConsoleHelper.WriteTitle("Response API");
    ConsoleHelper.WriteSubtitle("OpenAI's evolved API for agentic applications");

    Console.WriteLine("Select a demo:");
    Console.WriteLine();
    Console.WriteLine("  [1] Basic Response API - Simple agent with Response API");
    Console.WriteLine("  [2] Stateful Conversation - Resume with previous_response_id");
    Console.WriteLine("  [3] Web Search Tool - Built-in web search capability");
    Console.WriteLine("  [4] Code Interpreter - Execute code in sandbox");
    Console.WriteLine("  [5] Compare: Response API vs Chat Completions");
    Console.WriteLine();
    Console.WriteLine("  [0] Exit");
    Console.WriteLine();

    Console.Write("Your choice: ");
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            await DemoBasicResponseApi(apiKey, model);
            break;
        case "2":
            await DemoStatefulConversation(apiKey, model);
            break;
        case "3":
            await DemoWebSearch(apiKey, model);
            break;
        case "4":
            await DemoCodeInterpreter(apiKey, model);
            break;
        case "5":
            await DemoCompareApis(apiKey, model);
            break;
        case "0":
            ConsoleHelper.WriteSuccess("Goodbye!");
            return;
        default:
            ConsoleHelper.WriteWarning("Invalid choice. Press any key to continue...");
            Console.ReadKey();
            break;
    }
}

// ============================================================================
// DEMO 1: BASIC RESPONSE API
// ============================================================================
async Task DemoBasicResponseApi(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 1: Basic Response API");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Response API uses GetResponsesClient() instead of GetChatClient().");
    ConsoleHelper.WriteInfo("The same ChatClientAgent works with both APIs!");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);

    // KEY DIFFERENCE: Use GetResponsesClient() instead of GetChatClient()
    ResponsesClient responsesClient = client.GetResponsesClient(model);

    // CreateAIAgent() extension works the same way
    AIAgent agent = responsesClient.CreateAIAgent(
        instructions: "You are a helpful assistant. Be concise and friendly."
    );

    ConsoleHelper.WriteInfo($"Agent created using Response API with model: {model}");
    Console.WriteLine();

    // Interactive chat
    await RunInteractiveChat(agent, "Basic Response API");
}

// ============================================================================
// DEMO 2: STATEFUL CONVERSATION (Resume with previous_response_id)
// ============================================================================
async Task DemoStatefulConversation(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 2: Stateful Conversation");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Response API is STATEFUL - OpenAI stores conversation state.");
    ConsoleHelper.WriteInfo("You can resume conversations using previous_response_id.");
    ConsoleHelper.WriteInfo("No need to send full conversation history each time!");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);
    ResponsesClient responsesClient = client.GetResponsesClient(model);

    AIAgent agent = responsesClient.CreateAIAgent(
        instructions: "You are a helpful assistant. Remember what the user tells you."
    );

    // Create a thread for the conversation
    AgentThread thread = agent.GetNewThread();

    // First message
    ConsoleHelper.WriteUserMessage("My name is Marco and I live in Milan.");
    AgentRunResponse response1 = await agent.RunAsync("My name is Marco and I live in Milan.", thread);
    ConsoleHelper.WriteAgentMessage(response1.ToString());

    // Store the response ID (this would be saved to database in real app)
    string? responseId = response1.ResponseId;
    ConsoleHelper.WriteInfo($"Response ID saved: {responseId?[..Math.Min(20, responseId?.Length ?? 0)]}...");
    Console.WriteLine();

    // Second message (continuing conversation)
    ConsoleHelper.WriteUserMessage("What's my name?");
    AgentRunResponse response2 = await agent.RunAsync("What's my name?", thread);
    ConsoleHelper.WriteAgentMessage(response2.ToString());
    Console.WriteLine();

    // Simulate "later" - user comes back
    Console.WriteLine("--- Simulating user returning later ---");
    Console.WriteLine();

    // Resume conversation using just the response ID (no history needed!)
    ConsoleHelper.WriteUserMessage("What city do I live in?");

    // Use ConversationId to continue from where we left off
    var runOptions = new ChatClientAgentRunOptions
    {
        ChatOptions = new ChatOptions
        {
            ConversationId = response2.ResponseId  // Just pass the ID!
        }
    };

    AgentRunResponse response3 = await agent.RunAsync(
        "What city do I live in?",
        options: runOptions
    );
    ConsoleHelper.WriteAgentMessage(response3.ToString());

    Console.WriteLine();
    ConsoleHelper.WriteSuccess("The agent remembered context using only response_id!");
    ConsoleHelper.WriteInfo("No need to send full conversation history.");
    Console.WriteLine();

    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

// ============================================================================
// DEMO 3: WEB SEARCH TOOL (Built-in)
// ============================================================================
async Task DemoWebSearch(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 3: Web Search Tool");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Response API has BUILT-IN hosted tools.");
    ConsoleHelper.WriteInfo("Web Search is executed by OpenAI - no API needed on your side!");
    ConsoleHelper.WriteWarning("Note: Web Search requires OpenAI API (not Azure OpenAI).");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);
    ResponsesClient responsesClient = client.GetResponsesClient(model);

    // Add the built-in HostedWebSearchTool
    AIAgent agent = responsesClient.CreateAIAgent(
        instructions: "You are a helpful assistant that can search the web for current information. Always cite your sources.",
        tools: [new HostedWebSearchTool()]  // Built-in tool!
    );

    ConsoleHelper.WriteInfo("Agent created with HostedWebSearchTool.");
    Console.WriteLine();

    Console.Write("Enter a question requiring web search (or press Enter for default): ");
    string? question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        question = "What are today's top technology news headlines?";
    }

    Console.WriteLine();
    ConsoleHelper.WriteUserMessage(question);
    Console.WriteLine();

    ConsoleHelper.WriteInfo("Searching the web...");
    Console.WriteLine();

    ConsoleHelper.WriteAgentHeader();

    // Stream the response
    await foreach (var update in agent.RunStreamingAsync(question))
    {
        ConsoleHelper.WriteStreamChunk(update.ToString());
    }

    ConsoleHelper.EndStreamLine();
    ConsoleHelper.WriteSuccess("Web search completed using OpenAI's built-in tool!");
    Console.WriteLine();

    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

// ============================================================================
// DEMO 4: CODE INTERPRETER (Built-in)
// ============================================================================
async Task DemoCodeInterpreter(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 4: Code Interpreter");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Code Interpreter executes Python code in a sandboxed environment.");
    ConsoleHelper.WriteInfo("The LLM can write and run code, create files, charts, etc.");
    ConsoleHelper.WriteWarning("Note: Code Interpreter requires OpenAI API (not Azure OpenAI).");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);
    ResponsesClient responsesClient = client.GetResponsesClient(model);

    // Add the built-in HostedCodeInterpreterTool
    AIAgent agent = responsesClient.CreateAIAgent(
        instructions: "You are a data analyst. Use code to solve problems and create visualizations.",
        tools: [new HostedCodeInterpreterTool()]  // Built-in tool!
    );

    ConsoleHelper.WriteInfo("Agent created with HostedCodeInterpreterTool.");
    Console.WriteLine();

    Console.Write("Enter a task requiring code (or press Enter for default): ");
    string? task = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(task))
    {
        task = "Calculate the first 20 Fibonacci numbers and show them in a formatted table.";
    }

    Console.WriteLine();
    ConsoleHelper.WriteUserMessage(task);
    Console.WriteLine();

    ConsoleHelper.WriteInfo("Running code interpreter...");
    Console.WriteLine();

    // Run and get full response
    AgentRunResponse response = await agent.RunAsync(task);

    ConsoleHelper.WriteAgentMessage(response.ToString());
    Console.WriteLine();

    // Check for any generated files/content
    foreach (var message in response.Messages)
    {
        foreach (var content in message.Contents)
        {
            if (content.Annotations != null)
            {
                foreach (var annotation in content.Annotations)
                {
                    ConsoleHelper.WriteInfo($"Generated content: {annotation.GetType().Name}");
                }
            }
        }
    }

    ConsoleHelper.WriteSuccess("Code interpreter completed!");
    Console.WriteLine();

    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

// ============================================================================
// DEMO 5: COMPARE RESPONSE API VS CHAT COMPLETIONS
// ============================================================================
async Task DemoCompareApis(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 5: Response API vs Chat Completions");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);

    // Same question for both APIs
    string question = "What is 2 + 2? Reply with just the number.";

    // ----- CHAT COMPLETIONS API -----
    Console.WriteLine("=== CHAT COMPLETIONS API ===");
    ConsoleHelper.WriteInfo("Using GetChatClient() - the traditional approach");
    Console.WriteLine();

    ChatClient chatClient = client.GetChatClient(model);
    AIAgent chatAgent = chatClient.CreateAIAgent(
        instructions: "You are a helpful assistant.",
        name: "ChatCompletions-Agent"
    );

    ConsoleHelper.WriteUserMessage(question);
    var chatResponse = await chatAgent.RunAsync(question);
    ConsoleHelper.WriteAgentMessage(chatResponse.ToString());

    Console.WriteLine();
    ConsoleHelper.WriteInfo($"Response ID: {chatResponse.ResponseId ?? "N/A (not supported)"}");
    Console.WriteLine();

    // ----- RESPONSE API -----
    Console.WriteLine("=== RESPONSE API ===");
    ConsoleHelper.WriteInfo("Using GetResponsesClient() - the new approach");
    Console.WriteLine();

    ResponsesClient responsesClient = client.GetResponsesClient(model);
    AIAgent responseAgent = responsesClient.CreateAIAgent(
        instructions: "You are a helpful assistant.",
        name: "Response-Agent"
    );

    ConsoleHelper.WriteUserMessage(question);
    var responseApiResponse = await responseAgent.RunAsync(question);
    ConsoleHelper.WriteAgentMessage(responseApiResponse.ToString());

    Console.WriteLine();
    if (responseApiResponse.ResponseId != null && responseApiResponse.ResponseId.Length > 30)
    {
        ConsoleHelper.WriteInfo($"Response ID: {responseApiResponse.ResponseId[..30]}...");
    }
    else
    {
        ConsoleHelper.WriteInfo($"Response ID: {responseApiResponse.ResponseId}");
    }

    // Summary comparison
    Console.WriteLine();
    Console.WriteLine("=== COMPARISON SUMMARY ===");
    Console.WriteLine();
    Console.WriteLine("| Feature                  | Chat Completions | Response API |");
    Console.WriteLine("|--------------------------|------------------|--------------|");
    Console.WriteLine("| Stateful                 | No               | Yes          |");
    Console.WriteLine("| Response ID              | No               | Yes          |");
    Console.WriteLine("| Built-in Web Search      | No               | Yes          |");
    Console.WriteLine("| Built-in Code Interpreter| No               | Yes          |");
    Console.WriteLine("| Built-in File Search     | No               | Yes          |");
    Console.WriteLine("| MCP Server Support       | No               | Yes          |");
    Console.WriteLine("| Cache Utilization        | Standard         | 40-80% better|");
    Console.WriteLine("| Reasoning Summaries      | No               | Yes          |");
    Console.WriteLine();

    ConsoleHelper.WriteSuccess("Both APIs work with the same ChatClientAgent!");
    ConsoleHelper.WriteInfo("Response API is recommended for new agentic applications.");
    Console.WriteLine();

    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

// ============================================================================
// HELPER: Interactive Chat Loop
// ============================================================================
async Task RunInteractiveChat(AIAgent agent, string demoName)
{
    AgentThread thread = agent.GetNewThread();

    ConsoleHelper.WriteInfo("Type your messages. Type 'exit' to return to menu.");
    Console.WriteLine();

    while (true)
    {
        Console.Write("You: ");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            continue;

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        ConsoleHelper.WriteAgentHeader();

        await foreach (var update in agent.RunStreamingAsync(input, thread))
        {
            ConsoleHelper.WriteStreamChunk(update.ToString());
        }

        ConsoleHelper.EndStreamLine();
    }
}
