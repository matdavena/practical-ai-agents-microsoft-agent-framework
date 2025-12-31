/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                    USER PREFERENCES MEMORY                                   ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Custom implementation of AIContextProvider to store                         ║
 * ║  user preferences between sessions.                                          ║
 * ║                                                                              ║
 * ║  WHAT AN AIContextProvider DOES:                                             ║
 * ║  - Called BEFORE each agent invocation (InvokingAsync)                       ║
 * ║  - Called AFTER each agent invocation (InvokedAsync)                         ║
 * ║  - Can add "context" (instructions, messages, tools)                         ║
 * ║  - Perfect for: user preferences, application state, external data           ║
 * ║                                                                              ║
 * ║  "EXTRACT → STORE → INJECT" PATTERN:                                         ║
 * ║  1. EXTRACT: In InvokedAsync, extracts relevant info from chat               ║
 * ║  2. STORE: Saves info in a persistent format (JSON)                          ║
 * ║  3. INJECT: In InvokingAsync, injects info into the context                  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace DevAssistant.Memory.Memory;

/// <summary>
/// Memory for user preferences.
///
/// KEY CONCEPT: AIContextProvider
/// - It's a framework abstract class that allows "injecting" context
/// - Called automatically during agent execution
/// - Can modify the context before it reaches the LLM
///
/// LIFECYCLE:
/// 1. InvokingAsync - BEFORE LLM invocation → add context
/// 2. InvokedAsync - AFTER LLM invocation → extract info from responses
/// </summary>
public class UserPreferencesMemory : AIContextProvider
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * DATA STRUCTURE FOR PREFERENCES
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Record to store user preferences.
    /// We use a class to enable JSON deserialization.
    /// </summary>
    public class UserPreferences
    {
        public string? Nome { get; set; }
        public string? LinguaPreferita { get; set; }
        public string? StileComunicazione { get; set; }  // "formal", "informal", "technical"
        public List<string> ArgomentiInteresse { get; set; } = [];
        public string? ProgettoCorrente { get; set; }
        public DateTime UltimoAccesso { get; set; } = DateTime.Now;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * STATE AND CONFIGURATION
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Current user preferences.
    /// </summary>
    public UserPreferences Preferences { get; private set; } = new();

    /// <summary>
    /// Path of the file where preferences are persisted.
    /// </summary>
    private readonly string _preferencesFilePath;

    /// <summary>
    /// Creates a new instance of UserPreferencesMemory.
    /// </summary>
    /// <param name="preferencesFilePath">Path where to save preferences (JSON)</param>
    public UserPreferencesMemory(string? preferencesFilePath = null)
    {
        // Default: file in current directory
        _preferencesFilePath = preferencesFilePath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "user_preferences.json");

        // Load existing preferences if they exist
        LoadPreferences();
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * AIContextProvider IMPLEMENTATION - InvokingAsync
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * KEY METHOD: InvokingAsync
     * - Called AUTOMATICALLY BEFORE each LLM invocation
     * - Receives the messages from the current request
     * - Returns an AIContext with:
     *   - Instructions: additional instructions to add to the system prompt
     *   - Messages: messages to add to the conversation
     *   - Tools: additional tools to make available
     */

    /// <summary>
    /// Generates the context to inject into the agent's prompt.
    ///
    /// WHEN IT'S CALLED:
    /// - Before each RunAsync/RunStreamingAsync call
    /// - After the user has sent a message
    ///
    /// WHAT IT RETURNS:
    /// - An AIContext with Instructions that are added to the system prompt
    /// </summary>
    public override ValueTask<AIContext> InvokingAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        /*
         * STRATEGY:
         * We generate additional instructions based on saved preferences.
         * These instructions will be ADDED (not replaced) to the agent's
         * main instructions.
         */

        var instructions = GenerateContextFromPreferences();

        // Return an AIContext with the additional instructions
        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = instructions
        });
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * AIContextProvider IMPLEMENTATION - InvokedAsync
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * KEY METHOD: InvokedAsync
     * - Called AUTOMATICALLY AFTER each LLM invocation
     * - Receives both request and response messages
     * - Perfect for extracting information from the conversation
     */

    /// <summary>
    /// Processes the agent's response to extract information.
    ///
    /// WHEN IT'S CALLED:
    /// - After the LLM has responded
    /// - Receives both user messages and responses
    /// </summary>
    public override ValueTask InvokedAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        /*
         * STRATEGY:
         * We analyze user messages to extract preferences.
         * In a real system, you might use the LLM itself for extraction!
         */

        // Extract info from user messages
        foreach (var message in context.RequestMessages)
        {
            // Only user messages
            if (message.Role != ChatRole.User) continue;

            ExtractPreferencesFromMessage(message);
        }

        return default;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * SERIALIZATION (for persistence with the thread)
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * The Serialize method allows saving the provider's state
     * along with the thread. When the thread is deserialized,
     * the provider can recover its state.
     */

    /// <summary>
    /// Serializes the provider's state for persistence.
    /// </summary>
    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return JsonSerializer.SerializeToElement(Preferences, jsonSerializerOptions);
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * PREFERENCE EXTRACTION FROM MESSAGES
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * SIMPLIFIED APPROACH:
     * In a real system, you might use the LLM itself to extract info!
     * Here we use simple pattern matching for educational purposes.
     */

    private void ExtractPreferencesFromMessage(ChatMessage message)
    {
        // Extract text from the message
        var text = GetMessageText(message)?.ToLowerInvariant() ?? "";

        if (string.IsNullOrEmpty(text)) return;

        bool changed = false;

        // Name
        if (text.Contains("my name is") || text.Contains("i am ") || text.Contains("i'm "))
        {
            var nome = ExtractAfterPhrase(text, "my name is")
                    ?? ExtractAfterPhrase(text, "i am ")
                    ?? ExtractAfterPhrase(text, "i'm ");
            if (!string.IsNullOrEmpty(nome) && nome.Length < 30)
            {
                Preferences.Nome = CapitalizeFirst(nome);
                changed = true;
            }
        }

        // Current project
        if (text.Contains("working on") || text.Contains("on the project"))
        {
            var progetto = ExtractAfterPhrase(text, "working on")
                        ?? ExtractAfterPhrase(text, "on the project");
            if (!string.IsNullOrEmpty(progetto))
            {
                Preferences.ProgettoCorrente = progetto;
                changed = true;
            }
        }

        // Communication style
        if (text.Contains("prefer") && text.Contains("formal"))
        {
            Preferences.StileComunicazione = "formal";
            changed = true;
        }
        else if (text.Contains("informal") || text.Contains("casual"))
        {
            Preferences.StileComunicazione = "informal";
            changed = true;
        }

        // Technical interests
        var techKeywords = new[] { "c#", ".net", "python", "javascript", "react", "azure", "docker", "kubernetes" };
        foreach (var keyword in techKeywords)
        {
            if (text.Contains(keyword) && !Preferences.ArgomentiInteresse.Contains(keyword))
            {
                Preferences.ArgomentiInteresse.Add(keyword);
                changed = true;
            }
        }

        // Save if something changed
        if (changed)
        {
            Preferences.UltimoAccesso = DateTime.Now;
            SavePreferences();
        }
    }

    /// <summary>
    /// Extracts text from a ChatMessage.
    /// Messages can have complex content (text, images, etc.)
    /// </summary>
    private static string? GetMessageText(ChatMessage message)
    {
        if (message.Contents == null) return null;

        var sb = new StringBuilder();
        foreach (var content in message.Contents)
        {
            if (content is TextContent textContent)
            {
                sb.Append(textContent.Text);
            }
        }
        return sb.ToString();
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CONTEXT GENERATION
     * ═══════════════════════════════════════════════════════════════════════════
     */

    private string GenerateContextFromPreferences()
    {
        var parts = new List<string>();

        // Add info only if we have it
        if (!string.IsNullOrEmpty(Preferences.Nome))
        {
            parts.Add($"The user's name is {Preferences.Nome}.");
        }

        if (!string.IsNullOrEmpty(Preferences.StileComunicazione))
        {
            parts.Add($"They prefer a {Preferences.StileComunicazione} communication style.");
        }

        if (!string.IsNullOrEmpty(Preferences.ProgettoCorrente))
        {
            parts.Add($"They are working on the project: {Preferences.ProgettoCorrente}.");
        }

        if (Preferences.ArgomentiInteresse.Count > 0)
        {
            parts.Add($"They are interested in: {string.Join(", ", Preferences.ArgomentiInteresse)}.");
        }

        // If we have no info, don't add context
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        // Build the complete context
        return $"""

            === USER INFORMATION (from memory) ===
            {string.Join(" ", parts)}
            Use this information to personalize your responses.
            """;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * PREFERENCES PERSISTENCE
     * ═══════════════════════════════════════════════════════════════════════════
     */

    private void LoadPreferences()
    {
        try
        {
            if (File.Exists(_preferencesFilePath))
            {
                var json = File.ReadAllText(_preferencesFilePath);
                var loaded = JsonSerializer.Deserialize<UserPreferences>(json);
                if (loaded != null)
                {
                    Preferences = loaded;
                    Preferences.UltimoAccesso = DateTime.Now;
                }
            }
        }
        catch (Exception ex)
        {
            // In case of error, start with empty preferences
            Console.WriteLine($"[Memory] Unable to load preferences: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves preferences to file.
    /// Called automatically when preferences change.
    /// </summary>
    public void SavePreferences()
    {
        try
        {
            var json = JsonSerializer.Serialize(Preferences, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_preferencesFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Memory] Unable to save preferences: {ex.Message}");
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * UTILITY METHODS
     * ═══════════════════════════════════════════════════════════════════════════
     */

    private static string? ExtractAfterPhrase(string text, string phrase)
    {
        var index = text.IndexOf(phrase);
        if (index < 0) return null;

        var start = index + phrase.Length;
        var remaining = text[start..].Trim();

        // Take only the first significant word/phrase
        var endIndex = remaining.IndexOfAny(['.', ',', '!', '?', '\n']);
        if (endIndex > 0)
        {
            remaining = remaining[..endIndex];
        }

        // Limit the length
        if (remaining.Length > 50)
        {
            remaining = remaining[..50];
        }

        return string.IsNullOrWhiteSpace(remaining) ? null : remaining.Trim();
    }

    private static string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToUpper(text[0]) + text[1..];
    }

    /// <summary>
    /// Resets all preferences (for testing).
    /// </summary>
    public void ClearPreferences()
    {
        Preferences = new UserPreferences();
        if (File.Exists(_preferencesFilePath))
        {
            File.Delete(_preferencesFilePath);
        }
    }
}
