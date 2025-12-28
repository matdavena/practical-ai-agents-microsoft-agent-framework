// ============================================================================
// 05. CODE REVIEWER - RAG
// FILE: KnowledgeBaseProvider.cs
// ============================================================================
// Questo file implementa l'AIContextProvider per RAG.
//
// INTEGRAZIONE RAG + AGENTE:
//
// ┌─────────────────────────────────────────────────────────────────────────┐
// │                          FLUSSO RAG                                     │
// └─────────────────────────────────────────────────────────────────────────┘
//
//    1. Utente fa domanda          2. Provider intercetta
//    ┌────────────────┐            ┌────────────────┐
//    │ "Come gestire  │ ────────►  │ InvokingAsync  │
//    │  le eccezioni?"│            │ (prima di LLM) │
//    └────────────────┘            └───────┬────────┘
//                                          │
//    3. Ricerca semantica                  ▼
//    ┌────────────────────────────────────────────────────┐
//    │ TextSearchStore.SearchAsync("eccezioni")           │
//    │ → Trova: error-handling.md (score: 0.89)           │
//    │ → Trova: async-best-practices.md (score: 0.72)     │
//    └───────────────────────────┬────────────────────────┘
//                                │
//    4. Inietta nel contesto     ▼
//    ┌────────────────────────────────────────────────────┐
//    │ AIContext.Instructions += knowledge base context   │
//    └───────────────────────────┬────────────────────────┘
//                                │
//    5. LLM risponde             ▼
//    ┌────────────────────────────────────────────────────┐
//    │ "Secondo le best practices (error-handling.md),    │
//    │  dovresti sempre catch eccezioni specifiche..."    │
//    └────────────────────────────────────────────────────┘
//
// ============================================================================

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CodeReviewer.RAG.Rag;

/// <summary>
/// AIContextProvider che implementa RAG (Retrieval-Augmented Generation).
///
/// Questo provider:
/// 1. Intercetta ogni richiesta dell'utente (InvokingAsync)
/// 2. Cerca contenuti rilevanti nella knowledge base
/// 3. Inietta il contesto trovato nelle istruzioni per l'LLM
///
/// BENEFICI RAG:
/// - L'agente ha accesso a knowledge aggiornata (non solo training data)
/// - Le risposte sono basate su fonti specifiche e citabili
/// - Nessun fine-tuning necessario per aggiungere nuova knowledge
/// </summary>
public class KnowledgeBaseProvider : AIContextProvider
{
    // ========================================================================
    // DIPENDENZE
    // ========================================================================

    /// <summary>
    /// Store per la ricerca nella knowledge base.
    /// </summary>
    private readonly TextSearchStore _searchStore;

    /// <summary>
    /// Numero di chunk da recuperare per ogni query.
    /// </summary>
    private readonly int _topK;

    /// <summary>
    /// Score minimo di similarità per includere un chunk.
    /// </summary>
    private readonly float _minScore;

    // ========================================================================
    // COSTRUTTORE
    // ========================================================================

    /// <summary>
    /// Crea un nuovo KnowledgeBaseProvider.
    /// </summary>
    /// <param name="searchStore">Store per la ricerca semantica</param>
    /// <param name="topK">Numero di risultati da includere (default: 3)</param>
    /// <param name="minScore">Score minimo di similarità (default: 0.5)</param>
    public KnowledgeBaseProvider(
        TextSearchStore searchStore,
        int topK = 3,
        float minScore = 0.5f)
    {
        _searchStore = searchStore;
        _topK = topK;
        _minScore = minScore;
    }

    // ========================================================================
    // LIFECYCLE: BEFORE LLM CALL
    // ========================================================================

    /// <summary>
    /// Chiamato PRIMA di ogni richiesta all'LLM.
    ///
    /// PROCESSO:
    /// 1. Estrai l'ultima domanda dell'utente
    /// 2. Cerca contenuti rilevanti nella knowledge base
    /// 3. Costruisci il contesto da iniettare
    /// 4. Ritorna AIContext con le istruzioni aggiuntive
    /// </summary>
    public override async ValueTask<AIContext> InvokingAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        // Estrai l'ultima domanda dell'utente dalla conversazione
        var userQuery = ExtractLatestUserQuery(context);

        if (string.IsNullOrEmpty(userQuery))
        {
            // Nessuna query trovata, non aggiungiamo contesto
            return new AIContext();
        }

        // Cerca contenuti rilevanti nella knowledge base
        var searchResults = await _searchStore.SearchAsync(
            userQuery,
            _topK,
            _minScore);

        if (searchResults.Count == 0)
        {
            // Nessun risultato rilevante trovato
            Console.WriteLine("  🔍 Nessun contenuto rilevante trovato nella knowledge base");
            return new AIContext();
        }

        // Log dei risultati trovati
        Console.WriteLine($"  🔍 Trovati {searchResults.Count} chunk rilevanti:");
        foreach (var result in searchResults)
        {
            Console.WriteLine($"     - {result.Document.Title} (score: {result.Score:F2})");
        }

        // Costruisci il contesto da iniettare
        var knowledgeContext = BuildKnowledgeContext(searchResults);

        // Ritorna il contesto come istruzioni aggiuntive
        // Queste istruzioni vengono aggiunte al system prompt
        return new AIContext
        {
            Instructions = knowledgeContext
        };
    }

    // ========================================================================
    // LIFECYCLE: AFTER LLM CALL
    // ========================================================================

    /// <summary>
    /// Chiamato DOPO ogni risposta dell'LLM.
    ///
    /// Per il RAG non abbiamo bisogno di fare nulla dopo la risposta.
    /// Potremmo usare questo hook per:
    /// - Logging delle risposte
    /// - Analytics sull'uso della knowledge base
    /// - Feedback learning (quali chunk sono stati utili)
    /// </summary>
    public override ValueTask InvokedAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        // Nessuna azione necessaria dopo la risposta
        return default;
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Estrae l'ultima domanda dell'utente dalla conversazione.
    /// </summary>
    private string? ExtractLatestUserQuery(InvokingContext context)
    {
        // Cerca l'ultimo messaggio dell'utente
        var lastUserMessage = context.RequestMessages
            .LastOrDefault(m => m.Role == ChatRole.User);

        if (lastUserMessage == null)
        {
            return null;
        }

        // Estrai il testo dal messaggio
        // Un messaggio può contenere più parti (testo, immagini, etc.)
        var textParts = lastUserMessage.Contents
            .OfType<TextContent>()
            .Select(tc => tc.Text);

        return string.Join(" ", textParts);
    }

    /// <summary>
    /// Costruisce il contesto da iniettare all'agente.
    ///
    /// Il formato è importante:
    /// - Chiaro per l'LLM da interpretare
    /// - Include metadati per citazioni
    /// - Separatori visivi tra chunk
    /// </summary>
    private string BuildKnowledgeContext(List<SearchResult> results)
    {
        var lines = new List<string>
        {
            "",
            "=== RELEVANT KNOWLEDGE FROM CODE REVIEW BEST PRACTICES ===",
            "",
            "Use the following information to provide accurate, well-sourced answers.",
            "Always cite the source document when using this information.",
            ""
        };

        foreach (var result in results)
        {
            lines.Add($"--- Source: {result.Document.Title} ---");
            lines.Add($"File: {result.Document.FilePath}");
            lines.Add($"Relevance: {result.Score:P0}");
            lines.Add("");
            lines.Add(result.Document.Text);
            lines.Add("");
        }

        lines.Add("=== END OF KNOWLEDGE BASE CONTEXT ===");
        lines.Add("");

        return string.Join(Environment.NewLine, lines);
    }
}
