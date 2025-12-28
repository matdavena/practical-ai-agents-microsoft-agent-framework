/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                    USER PREFERENCES MEMORY                                   ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Implementazione custom di AIContextProvider per memorizzare le              ║
 * ║  preferenze dell'utente tra le sessioni.                                     ║
 * ║                                                                              ║
 * ║  COSA FA UN AIContextProvider:                                               ║
 * ║  - Viene chiamato PRIMA di ogni invocazione dell'agente (InvokingAsync)      ║
 * ║  - Viene chiamato DOPO ogni invocazione dell'agente (InvokedAsync)           ║
 * ║  - Può aggiungere "contesto" (istruzioni, messaggi, tools)                   ║
 * ║  - Perfetto per: preferenze utente, stato applicazione, dati esterni         ║
 * ║                                                                              ║
 * ║  PATTERN "EXTRACT → STORE → INJECT":                                         ║
 * ║  1. EXTRACT: In InvokedAsync, estrae info rilevanti dalla chat               ║
 * ║  2. STORE: Salva le info in un formato persistente (JSON)                    ║
 * ║  3. INJECT: In InvokingAsync, inietta le info nel contesto                   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace DevAssistant.Memory.Memory;

/// <summary>
/// Memoria per le preferenze utente.
///
/// CONCETTO CHIAVE: AIContextProvider
/// - È una classe astratta del framework che permette di "iniettare" contesto
/// - Viene chiamato automaticamente durante l'esecuzione dell'agente
/// - Può modificare il contesto prima che arrivi all'LLM
///
/// CICLO DI VITA:
/// 1. InvokingAsync - PRIMA dell'invocazione LLM → aggiungi contesto
/// 2. InvokedAsync - DOPO l'invocazione LLM → estrai info dalle risposte
/// </summary>
public class UserPreferencesMemory : AIContextProvider
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * STRUTTURA DATI PER LE PREFERENZE
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Record per memorizzare le preferenze dell'utente.
    /// Usiamo una classe per permettere la deserializzazione JSON.
    /// </summary>
    public class UserPreferences
    {
        public string? Nome { get; set; }
        public string? LinguaPreferita { get; set; }
        public string? StileComunicazione { get; set; }  // "formale", "informale", "tecnico"
        public List<string> ArgomentiInteresse { get; set; } = [];
        public string? ProgettoCorrente { get; set; }
        public DateTime UltimoAccesso { get; set; } = DateTime.Now;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * STATO E CONFIGURAZIONE
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Le preferenze correnti dell'utente.
    /// </summary>
    public UserPreferences Preferences { get; private set; } = new();

    /// <summary>
    /// Path del file dove persistere le preferenze.
    /// </summary>
    private readonly string _preferencesFilePath;

    /// <summary>
    /// Crea una nuova istanza di UserPreferencesMemory.
    /// </summary>
    /// <param name="preferencesFilePath">Path dove salvare le preferenze (JSON)</param>
    public UserPreferencesMemory(string? preferencesFilePath = null)
    {
        // Default: file nella directory corrente
        _preferencesFilePath = preferencesFilePath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "user_preferences.json");

        // Carichiamo le preferenze esistenti se ci sono
        LoadPreferences();
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * IMPLEMENTAZIONE DI AIContextProvider - InvokingAsync
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * METODO CHIAVE: InvokingAsync
     * - Viene chiamato AUTOMATICAMENTE PRIMA di ogni invocazione LLM
     * - Riceve i messaggi della richiesta corrente
     * - Restituisce un AIContext con:
     *   - Instructions: istruzioni aggiuntive da aggiungere al system prompt
     *   - Messages: messaggi da aggiungere alla conversazione
     *   - Tools: tools aggiuntivi da rendere disponibili
     */

    /// <summary>
    /// Genera il contesto da iniettare nel prompt dell'agente.
    ///
    /// QUANDO VIENE CHIAMATO:
    /// - Prima di ogni chiamata a RunAsync/RunStreamingAsync
    /// - Dopo che l'utente ha inviato un messaggio
    ///
    /// COSA RESTITUISCE:
    /// - Un AIContext con Instructions che vengono aggiunte al system prompt
    /// </summary>
    public override ValueTask<AIContext> InvokingAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        /*
         * STRATEGIA:
         * Generiamo istruzioni aggiuntive basate sulle preferenze salvate.
         * Queste istruzioni verranno AGGIUNTE (non sostituite) alle istruzioni
         * principali dell'agente.
         */

        var instructions = GenerateContextFromPreferences();

        // Restituiamo un AIContext con le istruzioni aggiuntive
        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = instructions
        });
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * IMPLEMENTAZIONE DI AIContextProvider - InvokedAsync
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * METODO CHIAVE: InvokedAsync
     * - Viene chiamato AUTOMATICAMENTE DOPO ogni invocazione LLM
     * - Riceve i messaggi della richiesta E della risposta
     * - Perfetto per estrarre informazioni dalla conversazione
     */

    /// <summary>
    /// Elabora la risposta dell'agente per estrarre informazioni.
    ///
    /// QUANDO VIENE CHIAMATO:
    /// - Dopo che l'LLM ha risposto
    /// - Riceve sia i messaggi dell'utente che le risposte
    /// </summary>
    public override ValueTask InvokedAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        /*
         * STRATEGIA:
         * Analizziamo i messaggi dell'utente per estrarre preferenze.
         * In un sistema reale, potresti usare l'LLM stesso per l'estrazione!
         */

        // Estraiamo info dai messaggi dell'utente
        foreach (var message in context.RequestMessages)
        {
            // Solo messaggi dell'utente
            if (message.Role != ChatRole.User) continue;

            ExtractPreferencesFromMessage(message);
        }

        return default;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * SERIALIZZAZIONE (per persistenza con il thread)
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * Il metodo Serialize permette di salvare lo stato del provider
     * insieme al thread. Quando il thread viene deserializzato,
     * il provider può recuperare il suo stato.
     */

    /// <summary>
    /// Serializza lo stato del provider per la persistenza.
    /// </summary>
    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return JsonSerializer.SerializeToElement(Preferences, jsonSerializerOptions);
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * ESTRAZIONE PREFERENZE DAI MESSAGGI
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * APPROCCIO SEMPLIFICATO:
     * In un sistema reale, potresti usare l'LLM stesso per estrarre info!
     * Qui usiamo pattern matching semplice per scopi didattici.
     */

    private void ExtractPreferencesFromMessage(ChatMessage message)
    {
        // Estraiamo il testo dal messaggio
        var text = GetMessageText(message)?.ToLowerInvariant() ?? "";

        if (string.IsNullOrEmpty(text)) return;

        bool changed = false;

        // Nome
        if (text.Contains("mi chiamo") || text.Contains("sono "))
        {
            var nome = ExtractAfterPhrase(text, "mi chiamo")
                    ?? ExtractAfterPhrase(text, "sono ");
            if (!string.IsNullOrEmpty(nome) && nome.Length < 30)
            {
                Preferences.Nome = CapitalizeFirst(nome);
                changed = true;
            }
        }

        // Progetto corrente
        if (text.Contains("sto lavorando su") || text.Contains("sul progetto"))
        {
            var progetto = ExtractAfterPhrase(text, "sto lavorando su")
                        ?? ExtractAfterPhrase(text, "sul progetto");
            if (!string.IsNullOrEmpty(progetto))
            {
                Preferences.ProgettoCorrente = progetto;
                changed = true;
            }
        }

        // Stile comunicazione
        if (text.Contains("preferisco") && text.Contains("formale"))
        {
            Preferences.StileComunicazione = "formale";
            changed = true;
        }
        else if (text.Contains("dammi del tu") || text.Contains("informale"))
        {
            Preferences.StileComunicazione = "informale";
            changed = true;
        }

        // Interessi tecnici
        var techKeywords = new[] { "c#", ".net", "python", "javascript", "react", "azure", "docker", "kubernetes" };
        foreach (var keyword in techKeywords)
        {
            if (text.Contains(keyword) && !Preferences.ArgomentiInteresse.Contains(keyword))
            {
                Preferences.ArgomentiInteresse.Add(keyword);
                changed = true;
            }
        }

        // Salviamo se qualcosa è cambiato
        if (changed)
        {
            Preferences.UltimoAccesso = DateTime.Now;
            SavePreferences();
        }
    }

    /// <summary>
    /// Estrae il testo da un ChatMessage.
    /// I messaggi possono avere contenuto complesso (testo, immagini, etc.)
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
     * GENERAZIONE DEL CONTESTO
     * ═══════════════════════════════════════════════════════════════════════════
     */

    private string GenerateContextFromPreferences()
    {
        var parts = new List<string>();

        // Aggiungiamo info solo se le abbiamo
        if (!string.IsNullOrEmpty(Preferences.Nome))
        {
            parts.Add($"L'utente si chiama {Preferences.Nome}.");
        }

        if (!string.IsNullOrEmpty(Preferences.StileComunicazione))
        {
            parts.Add($"Preferisce uno stile di comunicazione {Preferences.StileComunicazione}.");
        }

        if (!string.IsNullOrEmpty(Preferences.ProgettoCorrente))
        {
            parts.Add($"Sta lavorando sul progetto: {Preferences.ProgettoCorrente}.");
        }

        if (Preferences.ArgomentiInteresse.Count > 0)
        {
            parts.Add($"È interessato a: {string.Join(", ", Preferences.ArgomentiInteresse)}.");
        }

        // Se non abbiamo info, non aggiungiamo contesto
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        // Costruiamo il contesto completo
        return $"""

            === INFORMAZIONI SULL'UTENTE (dalla memoria) ===
            {string.Join(" ", parts)}
            Usa queste informazioni per personalizzare le tue risposte.
            """;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * PERSISTENZA PREFERENZE
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
            // In caso di errore, partiamo con preferenze vuote
            Console.WriteLine($"[Memory] Impossibile caricare preferenze: {ex.Message}");
        }
    }

    /// <summary>
    /// Salva le preferenze su file.
    /// Chiamato automaticamente quando le preferenze cambiano.
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
            Console.WriteLine($"[Memory] Impossibile salvare preferenze: {ex.Message}");
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

        // Prendiamo solo la prima parola/frase significativa
        var endIndex = remaining.IndexOfAny(['.', ',', '!', '?', '\n']);
        if (endIndex > 0)
        {
            remaining = remaining[..endIndex];
        }

        // Limitiamo la lunghezza
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
    /// Resetta tutte le preferenze (per testing).
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
