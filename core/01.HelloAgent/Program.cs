/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                          01. HELLO AGENT                                      ║
 * ║                 Introduction to Microsoft Agent Framework                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                               ║
 * ║  PROJECT GOAL:                                                                ║
 * ║  Create our first AI agent using Microsoft Agent Framework.                   ║
 * ║  Understand the basic cycle: Input → LLM → Output                             ║
 * ║                                                                               ║
 * ║  WHAT YOU'LL LEARN:                                                           ║
 * ║  1. How to create an OpenAIClient to communicate with OpenAI APIs             ║
 * ║  2. How to create a ChatClientAgent (the actual agent)                        ║
 * ║  3. How to execute a single request (RunAsync)                                ║
 * ║  4. How to handle response streaming (RunStreamingAsync)                      ║
 * ║  5. How to maintain a conversation with AgentThread                           ║
 * ║                                                                               ║
 * ║  KEY CONCEPTS:                                                                ║
 * ║  - OpenAIClient: The HTTP client to communicate with OpenAI                   ║
 * ║  - ChatClient: The chat-specific client (from OpenAI SDK)                     ║
 * ║  - ChatClientAgent: The framework agent that wraps the ChatClient             ║
 * ║  - AgentThread: Maintains conversation state (short-term memory)              ║
 * ║  - AgentRunResponse: The agent's response                                     ║
 * ║                                                                               ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.Text;
using Common;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;  // Required for the CreateAIAgent() extension method

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * STEP 0: CONSOLE CONFIGURATION
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * On Windows, the default console doesn't support UTF-8.
 * This line enables support for emojis and special characters.
 */
Console.OutputEncoding = Encoding.UTF8;

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * STEP 1: SETUP AND CONFIGURATION
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * First, we configure the application and verify that
 * the OpenAI API key is available.
 *
 * BEST PRACTICES:
 * - Never hardcode API keys in code
 * - Use environment variables or secret manager
 * - Validate configuration at startup
 */

// Display a welcome title
ConsoleHelper.WriteTitle("Hello Agent");
ConsoleHelper.WriteSubtitle("Your first AI agent with Microsoft Agent Framework");

// Show current configuration (for debugging)
ConsoleHelper.WriteSeparator("Configuration");
ConsoleHelper.WriteConfiguration(ConfigurationHelper.GetDisplayConfiguration());

// Get the API key and model from configuration
string apiKey = ConfigurationHelper.GetOpenAiApiKey();
string model = ConfigurationHelper.GetOpenAiModel();

ConsoleHelper.WriteSuccess($"Configuration loaded. Model: {model}");
ConsoleHelper.WriteSeparator();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * STEP 2: CREATING THE OPENAI CLIENT
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * OpenAIClient is the class from the official OpenAI SDK for .NET.
 * It's the entry point for all communications with OpenAI APIs.
 *
 * ARCHITECTURE:
 * ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
 * │  OpenAIClient   │────▶│   ChatClient    │────▶│ChatClientAgent  │
 * │  (HTTP Client)  │     │   (Chat API)    │     │  (AI Agent)     │
 * └─────────────────┘     └─────────────────┘     └─────────────────┘
 *
 * OpenAIClient: Handles authentication and HTTP connection
 * ChatClient: Specialized for the /chat/completions endpoint
 * ChatClientAgent: Adds agentic capabilities (threads, tools, etc.)
 */

ConsoleHelper.WriteInfo("Creating OpenAI Client...");

// Create the OpenAI client with our API key
OpenAIClient openAiClient = new(apiKey);

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * STEP 3: CREATING THE AGENT
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * The CreateAIAgent() method is an extension method provided by Microsoft Agent Framework.
 * It transforms a simple ChatClient into a ChatClientAgent with advanced capabilities.
 *
 * OPTIONAL PARAMETERS:
 * - instructions: The system prompt that defines the agent's behavior
 * - name: An identifier name for the agent
 *
 * THE SYSTEM PROMPT (instructions):
 * It's fundamental for defining the agent's "character":
 * - Who the agent is
 * - How it should behave
 * - What its limitations are
 * - What tone to use
 */

ConsoleHelper.WriteInfo("Creating AI Agent...");

/*
 * First, we get the ChatClient for the specified model.
 * Then we transform it into a ChatClientAgent with CreateAIAgent().
 *
 * The system prompt (instructions) defines the agent's behavior.
 * It's like giving instructions to a new employee on their first day.
 */
ChatClientAgent agent = openAiClient
    .GetChatClient(model)        // Get the chat client
    .CreateAIAgent(              // Transform it into an agent
        instructions: """
            You are a friendly and knowledgeable AI assistant.

            BEHAVIOR:
            - Always respond in the user's language
            - Be concise but thorough
            - Use a professional but accessible tone
            - If you don't know something, admit it honestly

            GOAL:
            Help the user understand Microsoft Agent Framework
            and AI agent development.
            """,
        name: "HelloAgent"       // Agent identifier name
    );

ConsoleHelper.WriteSuccess("Agent created successfully!");
ConsoleHelper.WriteSeparator();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * STEP 4: FIRST INTERACTION - SINGLE REQUEST (RunAsync)
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * RunAsync() is the simplest method to interact with the agent.
 * It sends a message and waits for the complete response.
 *
 * FLOW:
 * 1. The message is sent to the OpenAI APIs
 * 2. The LLM processes the message considering the instructions
 * 3. The response is returned as AgentRunResponse
 *
 * WHEN TO USE RunAsync:
 * - When you need the complete response before proceeding
 * - For short tasks where streaming isn't necessary
 * - When you need to process the response programmatically
 */

ConsoleHelper.WritePanel(
    "Demo 1: Single Request",
    "Using RunAsync() to send a question and receive the complete response."
);

ConsoleHelper.WriteUserMessage("What is Microsoft Agent Framework in 2 sentences?");

// RunAsync returns the complete response when the LLM has finished
AgentRunResponse response = await agent.RunAsync("What is Microsoft Agent Framework in 2 sentences?");

/*
 * AgentRunResponse contains:
 * - The response message (accessible via ToString() or .Message)
 * - Execution metadata (tokens used, etc.)
 * - Any tool calls (we'll see these in the next project)
 */
ConsoleHelper.WriteAgentMessage(response.ToString());

ConsoleHelper.WriteSeparator();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * STEP 5: RESPONSE STREAMING (RunStreamingAsync)
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * RunStreamingAsync() returns the response token by token.
 * It's fundamental for good UX in chat applications.
 *
 * STREAMING ADVANTAGES:
 * - User sees the response as it's being generated
 * - Perception of greater speed
 * - Ability to interrupt responses that are too long
 *
 * HOW IT WORKS:
 * - Returns an IAsyncEnumerable<AgentRunResponseUpdate>
 * - Each update contains a small chunk of text
 * - await foreach processes each chunk as soon as it's available
 */

ConsoleHelper.WritePanel(
    "Demo 2: Streaming Response",
    "Using RunStreamingAsync() to see the response token by token."
);

ConsoleHelper.WriteUserMessage("What are the 3 main components of an AI agent?");

// Agent header (without newline)
ConsoleHelper.WriteAgentHeader();

/*
 * await foreach is the pattern for consuming IAsyncEnumerable.
 * Each iteration waits for the next chunk from the server.
 *
 * AgentRunResponseUpdate:
 * - Contains a small piece of the response
 * - ToString() returns the chunk text
 * - Can also contain other data (partial tool calls, etc.)
 */
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(
    "What are the 3 main components of an AI agent?"))
{
    // Print each chunk without newline
    ConsoleHelper.WriteStreamChunk(update.ToString());
}

// End of streaming
ConsoleHelper.EndStreamLine();
ConsoleHelper.WriteSeparator();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * STEP 6: CONVERSATION WITH THREAD (Short-Term Memory)
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Until now, each request was independent: the agent didn't remember anything.
 * To maintain conversation context, we use AgentThread.
 *
 * AgentThread:
 * - Maintains the history of exchanged messages
 * - Is passed to each RunAsync/RunStreamingAsync call
 * - Is the agent's "short-term memory"
 *
 * IMPORTANT:
 * - Without a thread, each message is a new conversation
 * - With a thread, the agent can refer to previous messages
 * - The thread consumes tokens: the longer the history, the more tokens used
 */

ConsoleHelper.WritePanel(
    "Demo 3: Conversation with Thread",
    """
    Using AgentThread to maintain context.
    The agent will remember previous messages.

    Commands:
    - Type 'exit' to quit
    - Type 'clear' to start a new conversation
    """
);

/*
 * GetNewThread() creates a new empty thread.
 * Each thread maintains message history and can be serialized/deserialized.
 */
AgentThread conversationThread = agent.GetNewThread();

ConsoleHelper.WriteSystemMessage("New conversation started!");
ConsoleHelper.WriteSeparator();

/*
 * CONVERSATION LOOP
 * This is the typical pattern for a chatbot:
 * 1. Read user input
 * 2. Send to agent with thread
 * 3. Display response
 * 4. Repeat
 */
while (true)
{
    // Read user input
    string userInput = ConsoleHelper.AskInput();

    // Handle special commands
    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        ConsoleHelper.WriteSystemMessage("Goodbye! 👋");
        break;
    }

    if (userInput.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        // Create a new thread (new conversation)
        conversationThread = agent.GetNewThread();
        ConsoleHelper.WriteSystemMessage("Conversation reset! New session started.");
        ConsoleHelper.WriteSeparator();
        continue;
    }

    // Ignore empty input
    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    // Display user message
    ConsoleHelper.WriteUserMessage(userInput);

    // Agent response header
    ConsoleHelper.WriteAgentHeader();

    /*
     * IMPORTANT NOTE:
     * We pass the thread as the second parameter.
     * This allows the agent to see all the previous history.
     *
     * The thread is automatically updated with:
     * - The user's message
     * - The agent's response
     */
    await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(
        userInput,
        conversationThread))  // <-- The thread maintains context
    {
        ConsoleHelper.WriteStreamChunk(update.ToString());
    }

    ConsoleHelper.EndStreamLine();
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * SUMMARY AND NEXT STEPS
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * IN THIS PROJECT WE LEARNED:
 * ✅ Create an OpenAIClient and configure it
 * ✅ Create a ChatClientAgent with custom instructions
 * ✅ Use RunAsync for single requests
 * ✅ Use RunStreamingAsync for streaming
 * ✅ Use AgentThread to maintain context
 *
 * IN THE NEXT PROJECT (02.ChatWithHistory):
 * - We'll add TOOLS to the agent
 * - The agent will be able to perform actions in the real world
 * - We'll see the Tool Calling pattern
 *
 * ═══════════════════════════════════════════════════════════════════════════════
 */
