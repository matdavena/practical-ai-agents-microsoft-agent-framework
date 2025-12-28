/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                       THREAD PERSISTENCE                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Helper per salvare e caricare conversazioni (AgentThread) su disco.         ║
 * ║                                                                              ║
 * ║  MEMORIA A BREVE TERMINE:                                                    ║
 * ║  - L'AgentThread contiene TUTTI i messaggi della conversazione               ║
 * ║  - Di default vive solo in memoria (perso al riavvio)                        ║
 * ║  - Con la serializzazione, possiamo salvarlo e ripristinarlo                 ║
 * ║                                                                              ║
 * ║  COME FUNZIONA:                                                              ║
 * ║  1. thread.Serialize() → JsonElement con lo stato del thread                 ║
 * ║  2. JsonSerializer.Serialize(jsonElement) → stringa JSON per il file         ║
 * ║  3. JsonElement.Parse(jsonString) → ricostruisce il JsonElement              ║
 * ║  4. agent.DeserializeThread(jsonElement) → ricrea l'AgentThread              ║
 * ║                                                                              ║
 * ║  NOTA IMPORTANTE:                                                            ║
 * ║  La deserializzazione richiede l'AGENTE perché il thread può contenere       ║
 * ║  riferimenti a AIContextProvider che devono essere ricreati.                 ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using Microsoft.Agents.AI;
using System.Text.Json;

namespace DevAssistant.Memory.Memory;

/// <summary>
/// Helper statico per la persistenza delle conversazioni.
///
/// USA IL PATTERN BUILT-IN DEL FRAMEWORK:
/// - thread.Serialize() → converte il thread in JsonElement
/// - agent.DeserializeThread() → ricrea il thread dal JsonElement
///
/// NOTA: La serializzazione include:
/// - Tutti i messaggi (user, assistant, tool calls)
/// - Lo stato dell'AIContextProvider (se presente)
/// - I metadata del thread
/// </summary>
public static class ThreadPersistence
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * COSTANTI
     * ═══════════════════════════════════════════════════════════════════════════
     */

    private const string ThreadFileExtension = ".thread.json";
    private const string ThreadsFolder = "conversations";

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * SALVATAGGIO THREAD
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Salva un thread su disco.
    ///
    /// IL METODO Serialize() DEL FRAMEWORK:
    /// - Converte l'intero thread in un JsonElement
    /// - Include tutti i messaggi e metadata
    /// - Include lo stato dell'AIContextProvider
    /// </summary>
    /// <param name="thread">Il thread da salvare</param>
    /// <param name="conversationId">ID univoco della conversazione</param>
    /// <param name="basePath">Directory base (default: directory corrente)</param>
    public static async Task SaveThreadAsync(
        AgentThread thread,
        string conversationId,
        string? basePath = null)
    {
        var filePath = GetThreadFilePath(conversationId, basePath);

        // Assicuriamoci che la directory esista
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        /*
         * STEP 1: Serializza il thread in JsonElement
         *
         * thread.Serialize() restituisce un JsonElement che contiene:
         * - I messaggi della conversazione (se usa InMemoryChatMessageStore)
         * - Lo stato dell'AIContextProvider (se presente e implementa Serialize)
         * - L'ID della conversazione
         */
        JsonElement serializedThread = thread.Serialize();

        /*
         * STEP 2: Converti JsonElement in stringa JSON
         *
         * Usiamo JsonSerializer.Serialize per ottenere una stringa
         * che possiamo salvare su file.
         */
        var jsonString = JsonSerializer.Serialize(serializedThread, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // STEP 3: Salviamo su file
        await File.WriteAllTextAsync(filePath, jsonString);
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CARICAMENTO THREAD
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Carica un thread salvato in precedenza.
    ///
    /// NOTA: Richiede l'agente per la deserializzazione perché:
    /// - L'agente conosce come ricreare l'AIContextProvider
    /// - L'agente configura correttamente il thread
    /// </summary>
    /// <param name="agent">L'agente che userà il thread</param>
    /// <param name="conversationId">ID della conversazione da caricare</param>
    /// <param name="basePath">Directory base</param>
    /// <returns>Il thread caricato, o null se non esiste</returns>
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
             * STEP 1: Leggi il file JSON
             */
            var jsonString = await File.ReadAllTextAsync(filePath);

            /*
             * STEP 2: Parse in JsonElement
             *
             * JsonElement.Parse converte la stringa JSON in un JsonElement
             * che il framework può usare per ricostruire il thread.
             */
            JsonElement serializedThread = JsonElement.Parse(jsonString);

            /*
             * STEP 3: Deserializza usando l'agente
             *
             * agent.DeserializeThread() ricrea il thread completo:
             * - Ricostruisce la cronologia dei messaggi
             * - Ricrea l'AIContextProvider (se configurato nell'agente)
             * - Ripristina lo stato del provider serializzato
             */
            return agent.DeserializeThread(serializedThread);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThreadPersistence] Errore caricamento: {ex.Message}");
            return null;
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * GESTIONE CONVERSAZIONI
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Verifica se esiste un thread salvato.
    /// </summary>
    public static bool ThreadExists(string conversationId, string? basePath = null)
    {
        var filePath = GetThreadFilePath(conversationId, basePath);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Elimina un thread salvato.
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
    /// Elenca tutte le conversazioni salvate.
    /// </summary>
    /// <returns>Lista di ID conversazione</returns>
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
    /// Ottiene informazioni base su una conversazione salvata.
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
        // Sanifichiamo l'ID per evitare path injection
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
/// Informazioni su una conversazione salvata.
/// </summary>
public record ConversationInfo
{
    public required string ConversationId { get; init; }
    public DateTime LastModified { get; init; }
    public long FileSizeBytes { get; init; }
}
