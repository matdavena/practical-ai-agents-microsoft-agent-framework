/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                      02. DEV ASSISTANT - TOOLS                                ║
 * ║                         Function Calling / Tools                              ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                               ║
 * ║  PROJECT GOAL:                                                                ║
 * ║  Give the agent the ability to ACT in the real world through Tools.           ║
 * ║                                                                               ║
 * ║  WHAT YOU'LL LEARN:                                                           ║
 * ║  1. How to define Tools using C# methods with [Description] attribute         ║
 * ║  2. How to use AIFunctionFactory to register tools                            ║
 * ║  3. The Function Calling pattern: LLM decides when to call tools              ║
 * ║  4. Static tools vs instance tools                                            ║
 * ║  5. Best practices for tool security                                          ║
 * ║                                                                               ║
 * ║  KEY CONCEPTS:                                                                ║
 * ║  - Tool/Function: A function that the agent can invoke                        ║
 * ║  - AIFunctionFactory: Factory to create AITool from .NET methods              ║
 * ║  - [Description]: Attribute that describes the tool to the LLM                ║
 * ║  - Function Calling: The pattern where the LLM decides to use a tool          ║
 * ║                                                                               ║
 * ║  FUNCTION CALLING FLOW:                                                       ║
 * ║  1. User makes a request (e.g., "what time is it?")                           ║
 * ║  2. LLM analyzes available tools                                              ║
 * ║  3. LLM decides to call a tool (e.g., GetCurrentDateTime)                     ║
 * ║  4. Framework executes the .NET function                                      ║
 * ║  5. Result is passed back to the LLM                                          ║
 * ║  6. LLM formulates the final response                                         ║
 * ║                                                                               ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.Text;
using Common;
using DevAssistant.Tools.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CONSOLE CONFIGURATION AND SETUP
 * ═══════════════════════════════════════════════════════════════════════════════
 */

Console.OutputEncoding = Encoding.UTF8;

ConsoleHelper.WriteTitle("DevAssistant");
ConsoleHelper.WriteSubtitle("An AI agent with Tools for developers");

ConsoleHelper.WriteSeparator("Configuration");
ConsoleHelper.WriteConfiguration(ConfigurationHelper.GetDisplayConfiguration());

string apiKey = ConfigurationHelper.GetOpenAiApiKey();
string model = ConfigurationHelper.GetOpenAiModel();

ConsoleHelper.WriteSuccess($"Configuration loaded. Model: {model}");
ConsoleHelper.WriteSeparator();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CREATING TOOL INSTANCES
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * STATIC TOOLS vs INSTANCE TOOLS:
 *
 * - DateTimeTools and CalculatorTools are STATIC
 *   → They have no state
 *   → Methods are static
 *   → Registered with: AIFunctionFactory.Create(ClassName.MethodName)
 *
 * - FileSystemTools has an INSTANCE
 *   → It has state (WorkingDirectory)
 *   → Methods are instance methods
 *   → Create instance first, then register methods
 *   → Registered with: AIFunctionFactory.Create(instance.MethodName)
 */

ConsoleHelper.WriteInfo("Initializing Tools...");

// FileSystemTools requires an instance to configure the WorkingDirectory
var fileTools = new FileSystemTools();

ConsoleHelper.WriteSystemMessage($"Workspace: {fileTools.WorkingDirectory}");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CREATING THE AGENT WITH TOOLS
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * AIFunctionFactory.Create() transforms a .NET method into an AITool:
 *
 * 1. Extracts the method name (or uses the specified one)
 * 2. Reads the [Description] attribute for the description
 * 3. Analyzes parameters and their [Description]
 * 4. Creates a JSON schema for the LLM
 *
 * The optional second parameter is the tool name (useful for clearer names).
 */

ConsoleHelper.WriteInfo("Creating AI Agent with Tools...");

// Create the list of tools
var tools = new List<AITool>
{
    /*
     * DATETIME TOOLS (static)
     * ────────────────────────
     * Note: for static methods, we pass the method directly
     */
    AIFunctionFactory.Create(DateTimeTools.GetCurrentDateTime, "get_current_datetime"),
    AIFunctionFactory.Create(DateTimeTools.GetCurrentTimezone, "get_timezone"),
    AIFunctionFactory.Create(DateTimeTools.CalculateDateDifference, "calculate_date_difference"),
    AIFunctionFactory.Create(DateTimeTools.GetDayOfWeek, "get_day_of_week"),

    /*
     * CALCULATOR TOOLS (static)
     * ──────────────────────────
     */
    AIFunctionFactory.Create(CalculatorTools.Calculate, "calculate"),
    AIFunctionFactory.Create(CalculatorTools.CalculatePercentage, "calculate_percentage"),
    AIFunctionFactory.Create(CalculatorTools.ConvertUnits, "convert_units"),
    AIFunctionFactory.Create(CalculatorTools.CalculateStatistics, "calculate_statistics"),

    /*
     * FILESYSTEM TOOLS (instance)
     * ──────────────────────────
     * Note: for instance methods, we pass instance.Method
     */
    AIFunctionFactory.Create(fileTools.GetWorkingDirectory, "get_working_directory"),
    AIFunctionFactory.Create(fileTools.ListFiles, "list_files"),
    AIFunctionFactory.Create(fileTools.ReadFile, "read_file"),
    AIFunctionFactory.Create(fileTools.WriteFile, "write_file"),
    AIFunctionFactory.Create(fileTools.CreateDirectory, "create_directory"),
    AIFunctionFactory.Create(fileTools.DeleteFile, "delete_file"),
};

ConsoleHelper.WriteSuccess($"Registered {tools.Count} tools");

/*
 * Create the agent passing tools in the 'tools' parameter
 *
 * NOTE ON SYSTEM PROMPT:
 * With tools, it's important to instruct the agent on:
 * - WHEN to use tools (e.g., "for calculations use the calculator")
 * - HOW to behave with results
 * - Any security limitations
 */

ChatClientAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(model)
    .CreateAIAgent(
        instructions: """
            You are an AI assistant for developers with access to various tools.

            BEHAVIOR:
            - Always respond in the user's language
            - Be concise but thorough
            - Use tools when appropriate instead of making up answers

            AVAILABLE TOOLS:
            - DateTime: for date, time, timezone and date calculations
            - Calculator: for mathematical calculations, percentages, conversions and statistics
            - FileSystem: for reading, writing and managing files (limited to workspace)

            IMPORTANT:
            - For any numerical calculation, ALWAYS USE the calculator tool
            - For file operations, always verify the result and communicate it to the user
            - Never make up data: use tools to get real information
            """,
        tools: tools,
        name: "DevAssistant"
    );

ConsoleHelper.WriteSeparator();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * TOOLS DEMO
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Show some examples of how the agent uses tools automatically.
 */

ConsoleHelper.WritePanel(
    "Tools Demo",
    """
    Let's try some requests that require using tools.
    The agent will autonomously decide which tool to use.
    """
);

// Demo 1: DateTime tool
await DemoToolCall("What time is it now?");

// Demo 2: Calculator tool
await DemoToolCall("What is 15% of 250?");

// Demo 3: FileSystem tool - create a file
await DemoToolCall("Create a file called 'test.txt' with the content 'Hello from AI Agent!'");

// Demo 4: FileSystem tool - read the file
await DemoToolCall("Read the content of the file test.txt");

// Demo 5: Multiple tools in one request
await DemoToolCall("What day of the week was December 25, 2024?");

ConsoleHelper.WriteSeparator();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * INTERACTIVE CHAT
 * ═══════════════════════════════════════════════════════════════════════════════
 */

ConsoleHelper.WritePanel(
    "Interactive Chat",
    """
    Now you can freely interact with the agent.
    Try asking:
    - Mathematical calculations ("what is 123 * 456?")
    - Dates ("how many days until New Year?")
    - Files ("show files in workspace")
    - Conversions ("convert 100 km to miles")

    Commands: 'exit' to quit, 'clear' for new conversation
    """
);

AgentThread thread = agent.GetNewThread();

ConsoleHelper.WriteSystemMessage("New conversation started!");
ConsoleHelper.WriteSeparator();

while (true)
{
    string userInput = ConsoleHelper.AskInput();

    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        ConsoleHelper.WriteSystemMessage("Goodbye! 👋");
        break;
    }

    if (userInput.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        thread = agent.GetNewThread();
        ConsoleHelper.WriteSystemMessage("Conversation reset!");
        ConsoleHelper.WriteSeparator();
        continue;
    }

    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    ConsoleHelper.WriteUserMessage(userInput);
    ConsoleHelper.WriteAgentHeader();

    await foreach (var update in agent.RunStreamingAsync(userInput, thread))
    {
        ConsoleHelper.WriteStreamChunk(update.ToString());
    }

    ConsoleHelper.EndStreamLine();
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * HELPER METHOD FOR DEMOS
 * ═══════════════════════════════════════════════════════════════════════════════
 */

async Task DemoToolCall(string prompt)
{
    ConsoleHelper.WriteUserMessage(prompt);

    /*
     * Note: for demos we use RunAsync (not streaming) for simplicity.
     * In production, streaming offers a better UX.
     */
    var response = await agent.RunAsync(prompt);

    ConsoleHelper.WriteAgentMessage(response.ToString());
    Console.WriteLine();
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * SUMMARY AND NEXT STEPS
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * IN THIS PROJECT WE LEARNED:
 * ✅ How to define Tools with [Description]
 * ✅ How to use AIFunctionFactory.Create()
 * ✅ Difference between static and instance tools
 * ✅ How the LLM autonomously decides which tool to use
 * ✅ Best practices for security (sandboxing)
 *
 * IN THE NEXT PROJECT (03.DevAssistant.Memory):
 * - Short-term memory (already seen with AgentThread)
 * - LONG-TERM memory (persists between sessions)
 * - How the agent can remember preferences and context
 *
 * ═══════════════════════════════════════════════════════════════════════════════
 */
