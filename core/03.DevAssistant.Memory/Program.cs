/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                  03. DEV ASSISTANT - MEMORY                                  ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  OBJECTIVE: Understand the different types of memory for an AI agent         ║
 * ║                                                                              ║
 * ║  MEMORY TYPES:                                                               ║
 * ║                                                                              ║
 * ║  1. IMPLICIT MEMORY (Thread)                                                 ║
 * ║     - The AgentThread automatically maintains the history                    ║
 * ║     - The agent "remembers" everything said in the session                   ║
 * ║     - Lost on application restart (unless persisted)                         ║
 * ║                                                                              ║
 * ║  2. SHORT-TERM MEMORY (Thread Persistence)                                   ║
 * ║     - Thread serialization to disk                                           ║
 * ║     - Allows resuming conversations after restart                            ║
 * ║     - Maintains the ENTIRE conversation                                      ║
 * ║                                                                              ║
 * ║  3. LONG-TERM MEMORY (AIContextProvider)                                     ║
 * ║     - Information extracted and persisted between sessions                   ║
 * ║     - Not the entire chat, but important FACTS (preferences, state, etc.)    ║
 * ║     - Injected into context before each request                              ║
 * ║                                                                              ║
 * ║  4. SEMANTIC MEMORY (Vector Store) - Preview in next project                 ║
 * ║     - Semantic similarity search                                             ║
 * ║     - Uses embeddings to find relevant info                                  ║
 * ║     - Ideal for large amounts of information                                 ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.Text;
using Common;
using DevAssistant.Memory.Memory;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;  // Required for the CreateAIAgent extension method with ChatClientAgentOptions

// ═══════════════════════════════════════════════════════════════════════════════
// INITIAL CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════════

// Important for correctly displaying emojis and special characters
Console.OutputEncoding = Encoding.UTF8;

ConsoleHelper.WriteTitle("03. DevAssistant Memory");
ConsoleHelper.WriteSubtitle("Exploring agent memory");

// Get the configuration
var apiKey = ConfigurationHelper.GetOpenAiApiKey();
var model = ConfigurationHelper.GetOpenAiModel();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * PREFERENCES MEMORY SETUP
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * UserPreferencesMemory is a custom AIContextProvider that:
 * - Loads user preferences saved from previous sessions
 * - Extracts new preferences from user messages
 * - Injects preferences into the agent's context
 */

ConsoleHelper.WriteSeparator("Step 1: Memory system initialization");

// Create memory for user preferences
var preferencesMemory = new UserPreferencesMemory();

// Show loaded preferences (if they exist)
if (!string.IsNullOrEmpty(preferencesMemory.Preferences.Nome))
{
    ConsoleHelper.WriteInfo($"Preferences loaded - Welcome back, {preferencesMemory.Preferences.Nome}!");

    Console.WriteLine();
    Console.WriteLine("Stored preferences:");
    Console.WriteLine($"  Name: {preferencesMemory.Preferences.Nome}");
    Console.WriteLine($"  Style: {preferencesMemory.Preferences.StileComunicazione ?? "not specified"}");
    Console.WriteLine($"  Project: {preferencesMemory.Preferences.ProgettoCorrente ?? "none"}");
    if (preferencesMemory.Preferences.ArgomentiInteresse.Count > 0)
    {
        Console.WriteLine($"  Interests: {string.Join(", ", preferencesMemory.Preferences.ArgomentiInteresse)}");
    }
    Console.WriteLine($"  Last access: {preferencesMemory.Preferences.UltimoAccesso:g}");
    Console.WriteLine();
}
else
{
    ConsoleHelper.WriteInfo("No preferences found - First run");
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * AGENT CREATION WITH MEMORY
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * KEY POINT: How to connect AIContextProvider to the agent
 *
 * The AIContextProvider is passed via ChatClientAgentOptions.
 * We use AIContextProviderFactory to create the provider for each thread.
 */

ConsoleHelper.WriteSeparator("Step 2: Agent creation with memory");

/*
 * AGENT INSTRUCTIONS:
 *
 * Note that the instructions DO NOT include user info.
 * These will be injected AUTOMATICALLY by UserPreferencesMemory!
 *
 * The flow is:
 * 1. User writes message
 * 2. Framework calls preferencesMemory.InvokingAsync()
 * 3. Context is ADDED to the instructions
 * 4. The LLM receives instructions + context + messages
 * 5. After the response, framework calls preferencesMemory.InvokedAsync()
 * 6. The provider extracts and saves any new preferences
 */
string agentInstructions = """
    You are DevAssistant, an assistant for software developers.

    Your task is to help developers with:
    - Answering questions about programming
    - Suggesting best practices
    - Helping with debugging
    - Explaining technical concepts

    IMPORTANT ABOUT MEMORY:
    - Remember what the user tells you during the conversation
    - If the user introduces themselves or shares preferences, remember it
    - Use information from previous sessions (if available in context)
    - Personalize responses based on what you know about the user

    Be concise but helpful. Use code examples when appropriate.
    """;

/*
 * AGENT CREATION WITH ChatClientAgentOptions:
 *
 * We use ChatClientAgentOptions for advanced configuration:
 * - ChatOptions.Instructions: the agent's instructions
 * - AIContextProviderFactory: factory to create AIContextProvider for each thread
 *
 * NOTE ABOUT THE FACTORY:
 * The factory is called when creating a new thread.
 * Returns an instance of AIContextProvider for that thread.
 * Here we always use the same instance (preferencesMemory) to share
 * preferences across all threads.
 */
ChatClientAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(model)
    .CreateAIAgent(new ChatClientAgentOptions
    {
        Name = "DevAssistant",
        ChatOptions = new ChatOptions
        {
            // Instructions can be passed here
            // But for simplicity we pass them in the first message
        },
        // Factory that returns our AIContextProvider
        // ctx contains the serialized state (if the thread was deserialized)
        AIContextProviderFactory = ctx => preferencesMemory
    });

// Add instructions as system message
// (the framework supports ChatOptions.Instructions as well, but for compatibility we use this approach)

ConsoleHelper.WriteInfo("Agent created with UserPreferencesMemory connected");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * SAVED CONVERSATIONS MANAGEMENT
 * ═══════════════════════════════════════════════════════════════════════════════
 */

ConsoleHelper.WriteSeparator("Step 3: Checking saved conversations");

var savedConversations = ThreadPersistence.ListSavedConversations().ToList();
AgentThread? existingThread = null;
string conversationId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";

if (savedConversations.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Saved conversations found:");
    for (int i = 0; i < savedConversations.Count; i++)
    {
        var info = ThreadPersistence.GetConversationInfo(savedConversations[i]);
        if (info != null)
        {
            Console.WriteLine($"  [{i + 1}] {info.ConversationId}");
            Console.WriteLine($"      Last updated: {info.LastModified:g}");
            Console.WriteLine($"      Size: {info.FileSizeBytes / 1024.0:F1} KB");
        }
    }
    Console.WriteLine($"  [N] New conversation");
    Console.WriteLine();

    Console.Write("Choose (number or N): ");
    var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

    if (int.TryParse(choice, out int index) && index > 0 && index <= savedConversations.Count)
    {
        conversationId = savedConversations[index - 1];

        /*
         * THREAD LOADING:
         *
         * We use agent.DeserializeThread() through ThreadPersistence.
         * The thread is recreated with:
         * - All previous messages
         * - The AIContextProvider state (the preferences)
         */
        existingThread = await ThreadPersistence.LoadThreadAsync(agent, conversationId);

        if (existingThread != null)
        {
            ConsoleHelper.WriteSuccess($"Conversation '{conversationId}' loaded!");
        }
        else
        {
            ConsoleHelper.WriteError("Unable to load conversation");
            existingThread = null;
        }
    }
    else
    {
        ConsoleHelper.WriteInfo("Starting new conversation");
    }
}
else
{
    ConsoleHelper.WriteInfo("No saved conversations - Starting new session");
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * THREAD SETUP
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * If we loaded an existing thread, we use it.
 * Otherwise we create a new one.
 */

AgentThread thread = existingThread ?? agent.GetNewThread();

Console.WriteLine();
ConsoleHelper.WriteInfo(existingThread != null
    ? "Using loaded thread - conversation continues from where it left off"
    : "New thread created - starting a new conversation");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CONVERSATION LOOP
 * ═══════════════════════════════════════════════════════════════════════════════
 */

ConsoleHelper.WriteSeparator("Step 4: Starting conversation");

Console.WriteLine();
Console.WriteLine("SPECIAL COMMANDS:");
Console.WriteLine("  'esci' or 'quit'   - Exit and save the conversation");
Console.WriteLine("  'reset'            - Clear preferences and conversations");
Console.WriteLine("  'memoria'          - Show memory status");
Console.WriteLine("  'salva'            - Force save conversation");
Console.WriteLine();

// Suggestions for testing memory
if (string.IsNullOrEmpty(preferencesMemory.Preferences.Nome))
{
    Console.WriteLine("SUGGESTION: Try saying 'My name is [your name]' to test the memory!");
    Console.WriteLine("            Or: 'I'm working on the [project name] project'");
    Console.WriteLine();
}

// Send instructions as first system message
// This is a workaround because ChatCompletionOptions.Instructions is not always supported
var systemMessage = agentInstructions;

int messageCount = 0;

while (true)
{
    // User input
    ConsoleHelper.WriteUserMessage("");
    Console.Write("> ");
    var userInput = Console.ReadLine();

    // Handle empty input
    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    // Special commands
    var command = userInput.Trim().ToLowerInvariant();

    if (command is "esci" or "quit" or "exit")
    {
        // Save the conversation before exiting
        await ThreadPersistence.SaveThreadAsync(thread, conversationId);
        preferencesMemory.SavePreferences();

        Console.WriteLine();
        ConsoleHelper.WriteSuccess($"Conversation saved as '{conversationId}'");
        ConsoleHelper.WriteSuccess("Preferences saved");
        ConsoleHelper.WriteInfo("See you next time!");
        break;
    }

    if (command == "reset")
    {
        Console.Write("Are you sure you want to delete ALL preferences and conversations? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
        {
            preferencesMemory.ClearPreferences();

            // Delete all conversations
            foreach (var convId in ThreadPersistence.ListSavedConversations())
            {
                ThreadPersistence.DeleteThread(convId);
            }

            // Create a new thread
            thread = agent.GetNewThread();
            conversationId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
            messageCount = 0;

            ConsoleHelper.WriteSuccess("Memory completely reset!");
        }
        continue;
    }

    if (command == "memoria")
    {
        Console.WriteLine();
        Console.WriteLine("=== MEMORY STATUS ===");
        Console.WriteLine();
        Console.WriteLine("USER PREFERENCES:");
        Console.WriteLine($"  Name: {preferencesMemory.Preferences.Nome ?? "(not set)"}");
        Console.WriteLine($"  Style: {preferencesMemory.Preferences.StileComunicazione ?? "(not set)"}");
        Console.WriteLine($"  Project: {preferencesMemory.Preferences.ProgettoCorrente ?? "(not set)"}");
        Console.WriteLine($"  Interests: {(preferencesMemory.Preferences.ArgomentiInteresse.Count > 0 ? string.Join(", ", preferencesMemory.Preferences.ArgomentiInteresse) : "(none)")}");
        Console.WriteLine();
        Console.WriteLine("CONVERSATION:");
        Console.WriteLine($"  ID: {conversationId}");
        Console.WriteLine($"  Messages in session: {messageCount}");
        Console.WriteLine();
        Console.WriteLine("SAVED CONVERSATIONS:");
        var saved = ThreadPersistence.ListSavedConversations().ToList();
        if (saved.Count > 0)
        {
            foreach (var id in saved)
            {
                Console.WriteLine($"  - {id}");
            }
        }
        else
        {
            Console.WriteLine("  (none)");
        }
        Console.WriteLine();
        continue;
    }

    if (command == "salva")
    {
        await ThreadPersistence.SaveThreadAsync(thread, conversationId);
        preferencesMemory.SavePreferences();
        ConsoleHelper.WriteSuccess($"Conversation saved as '{conversationId}'");
        continue;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Agent invocation
    // ─────────────────────────────────────────────────────────────────────────

    try
    {
        /*
         * WHAT HAPPENS DURING RunStreamingAsync:
         *
         * 1. User message is added to the thread
         * 2. Framework calls ALL registered contextProviders
         *    → preferencesMemory.InvokingAsync() is called
         *    → Generates context from saved preferences
         * 3. Context is added to the instructions
         * 4. Everything is sent to the LLM
         * 5. The response is streamed
         * 6. Framework calls preferencesMemory.InvokedAsync()
         *    → Extracts new preferences from messages
         * 7. The response is added to the thread
         */

        ConsoleHelper.WriteAgentMessage("");

        // For the first message, include instructions as context
        var promptWithContext = messageCount == 0
            ? $"[System context: {systemMessage}]\n\n{userInput}"
            : userInput;

        await foreach (var update in agent.RunStreamingAsync(promptWithContext, thread))
        {
            // Stream the response
            ConsoleHelper.WriteStreamChunk(update.ToString());
        }

        ConsoleHelper.EndStreamLine();

        messageCount++;

        // Save periodically (every 5 exchanges)
        if (messageCount % 5 == 0)
        {
            await ThreadPersistence.SaveThreadAsync(thread, conversationId);
            ConsoleHelper.WriteInfo("[Auto-save conversation]");
        }
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error during processing: {ex.Message}");

        // In case of error, show more details for debugging
        if (ex.InnerException != null)
        {
            Console.WriteLine($"  Detail: {ex.InnerException.Message}");
        }
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * SUMMARY OF LEARNED CONCEPTS
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * 1. IMPLICIT MEMORY (Thread):
 *    - AgentThread maintains all messages in memory
 *    - The agent "remembers" the current conversation automatically
 *
 * 2. THREAD PERSISTENCE:
 *    - thread.Serialize() → saves the thread as JsonElement
 *    - agent.DeserializeThread() → reloads the thread
 *    - Allows resuming conversations between sessions
 *
 * 3. AIContextProvider:
 *    - Abstract class for injecting dynamic context
 *    - InvokingAsync: called BEFORE the LLM invocation
 *    - InvokedAsync: called AFTER the LLM invocation
 *    - Returns AIContext with Instructions, Messages, Tools
 *
 * 4. LONG-TERM MEMORY:
 *    - We extract important FACTS from conversations
 *    - We save them in structured format (JSON, DB, etc.)
 *    - We inject them into context when relevant
 *
 * IN THE NEXT PROJECT:
 * - Vector Store for semantic memory
 * - Embeddings for similarity search
 * - RAG (Retrieval Augmented Generation)
 */
