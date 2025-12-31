/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                       THREAD PERSISTENCE                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Helper for saving and loading conversations (AgentThread) to disk.          ║
 * ║                                                                              ║
 * ║  SHORT-TERM MEMORY:                                                          ║
 * ║  - The AgentThread contains ALL messages from the conversation               ║
 * ║  - By default it lives only in memory (lost on restart)                      ║
 * ║  - With serialization, we can save and restore it                            ║
 * ║                                                                              ║
 * ║  HOW IT WORKS:                                                               ║
 * ║  1. thread.Serialize() → JsonElement with the thread state                   ║
 * ║  2. JsonSerializer.Serialize(jsonElement) → JSON string for the file         ║
 * ║  3. JsonElement.Parse(jsonString) → reconstructs the JsonElement             ║
 * ║  4. agent.DeserializeThread(jsonElement) → recreates the AgentThread         ║
 * ║                                                                              ║
 * ║  IMPORTANT NOTE:                                                             ║
 * ║  Deserialization requires the AGENT because the thread may contain           ║
 * ║  references to AIContextProvider that need to be recreated.                  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using Microsoft.Agents.AI;
using System.Text.Json;

namespace DevAssistant.Memory.Memory;

/// <summary>
/// Static helper for conversation persistence.
///
/// USES THE FRAMEWORK'S BUILT-IN PATTERN:
/// - thread.Serialize() → converts the thread to JsonElement
/// - agent.DeserializeThread() → recreates the thread from JsonElement
///
/// NOTE: Serialization includes:
/// - All messages (user, assistant, tool calls)
/// - The AIContextProvider state (if present)
/// - Thread metadata
/// </summary>
public static class ThreadPersistence
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CONSTANTS
     * ═══════════════════════════════════════════════════════════════════════════
     */

    private const string ThreadFileExtension = ".thread.json";
    private const string ThreadsFolder = "conversations";

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * THREAD SAVING
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Saves a thread to disk.
    ///
    /// THE FRAMEWORK'S Serialize() METHOD:
    /// - Converts the entire thread into a JsonElement
    /// - Includes all messages and metadata
    /// - Includes the AIContextProvider state
    /// </summary>
    /// <param name="thread">The thread to save</param>
    /// <param name="conversationId">Unique conversation ID</param>
    /// <param name="basePath">Base directory (default: current directory)</param>
    public static async Task SaveThreadAsync(
        AgentThread thread,
        string conversationId,
        string? basePath = null)
    {
        var filePath = GetThreadFilePath(conversationId, basePath);

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        /*
         * STEP 1: Serialize the thread to JsonElement
         *
         * thread.Serialize() returns a JsonElement containing:
         * - The conversation messages (if using InMemoryChatMessageStore)
         * - The AIContextProvider state (if present and implements Serialize)
         * - The conversation ID
         */
        JsonElement serializedThread = thread.Serialize();

        /*
         * STEP 2: Convert JsonElement to JSON string
         *
         * We use JsonSerializer.Serialize to get a string
         * that we can save to file.
         */
        var jsonString = JsonSerializer.Serialize(serializedThread, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // STEP 3: Save to file
        await File.WriteAllTextAsync(filePath, jsonString);
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * THREAD LOADING
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Loads a previously saved thread.
    ///
    /// NOTE: Requires the agent for deserialization because:
    /// - The agent knows how to recreate the AIContextProvider
    /// - The agent configures the thread correctly
    /// </summary>
    /// <param name="agent">The agent that will use the thread</param>
    /// <param name="conversationId">ID of the conversation to load</param>
    /// <param name="basePath">Base directory</param>
    /// <returns>The loaded thread, or null if it doesn't exist</returns>
    public static async Task<AgentThread?> LoadThreadAsync(
        AIAgent agent,
        string conversationId,
        string? basePath = null)
    {
        var filePath = GetThreadFilePath(conversationId, basePath);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            /*
             * STEP 1: Read the JSON file
             */
            var jsonString = await File.ReadAllTextAsync(filePath);

            /*
             * STEP 2: Parse into JsonElement
             *
             * JsonElement.Parse converts the JSON string into a JsonElement
             * that the framework can use to reconstruct the thread.
             */
            JsonElement serializedThread = JsonElement.Parse(jsonString);

            /*
             * STEP 3: Deserialize using the agent
             *
             * agent.DeserializeThread() recreates the complete thread:
             * - Reconstructs the message history
             * - Recreates the AIContextProvider (if configured in the agent)
             * - Restores the serialized provider state
             */
            return agent.DeserializeThread(serializedThread);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThreadPersistence] Loading error: {ex.Message}");
            return null;
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CONVERSATION MANAGEMENT
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Checks if a saved thread exists.
    /// </summary>
    public static bool ThreadExists(string conversationId, string? basePath = null)
    {
        var filePath = GetThreadFilePath(conversationId, basePath);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Deletes a saved thread.
    /// </summary>
    public static void DeleteThread(string conversationId, string? basePath = null)
    {
        var filePath = GetThreadFilePath(conversationId, basePath);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Lists all saved conversations.
    /// </summary>
    /// <returns>List of conversation IDs</returns>
    public static IEnumerable<string> ListSavedConversations(string? basePath = null)
    {
        var folder = GetThreadsFolder(basePath);

        if (!Directory.Exists(folder))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(folder, $"*{ThreadFileExtension}")
            .Select(f => Path.GetFileName(f).Replace(ThreadFileExtension, ""))
            .OrderBy(id => id);
    }

    /// <summary>
    /// Gets basic information about a saved conversation.
    /// </summary>
    public static ConversationInfo? GetConversationInfo(string conversationId, string? basePath = null)
    {
        var filePath = GetThreadFilePath(conversationId, basePath);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var fileInfo = new FileInfo(filePath);

        return new ConversationInfo
        {
            ConversationId = conversationId,
            LastModified = fileInfo.LastWriteTime,
            FileSizeBytes = fileInfo.Length
        };
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * UTILITY
     * ═══════════════════════════════════════════════════════════════════════════
     */

    private static string GetThreadsFolder(string? basePath)
    {
        var root = basePath ?? Directory.GetCurrentDirectory();
        return Path.Combine(root, ThreadsFolder);
    }

    private static string GetThreadFilePath(string conversationId, string? basePath)
    {
        // Sanitize the ID to avoid path injection
        var safeId = SanitizeFileName(conversationId);
        return Path.Combine(GetThreadsFolder(basePath), $"{safeId}{ThreadFileExtension}");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalid.Contains(c)));
    }
}

/// <summary>
/// Information about a saved conversation.
/// </summary>
public record ConversationInfo
{
    public required string ConversationId { get; init; }
    public DateTime LastModified { get; init; }
    public long FileSizeBytes { get; init; }
}
