// ============================================================================
// 05. CODE REVIEWER - RAG
// FILE: TextSearchStore.cs
// ============================================================================
// Questo file gestisce il caricamento, chunking e ricerca
// dei documenti della knowledge base.
//
// PIPELINE RAG COMPLETA:
//
// 1. INGESTION (una volta all'avvio):
//    ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
//    │  Carica MD   │ ──► │   Chunking   │ ──► │  Upsert nel  │
//    │  files       │     │  documenti   │     │ Vector Store │
//    └──────────────┘     └──────────────┘     └──────────────┘
//                                                     │
//                                                     ▼
//                                              ┌──────────────┐
//                                              │ Embedding    │
//                                              │ (automatico) │
//                                              └──────────────┘
//
// 2. RETRIEVAL (ad ogni query):
//    ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
//    │   Query      │ ──► │   Ricerca    │ ──► │ Top-K chunk  │
//    │   utente     │     │  semantica   │     │ rilevanti    │
//    └──────────────┘     └──────────────┘     └──────────────┘
//
// NOTA: Gli embeddings vengono generati AUTOMATICAMENTE dal vector store
// grazie alla configurazione EmbeddingGenerator. Non devi gestire
// manualmente i vettori float[]!
// ============================================================================

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeReviewer.RAG.Rag;

/// <summary>
/// Store per la ricerca semantica nei documenti della knowledge base.
///
/// RESPONSABILITÀ:
/// 1. Caricare documenti markdown dalla cartella KnowledgeBase
/// 2. Dividere i documenti in chunk gestibili
/// 3. Memorizzare nel vector store (embeddings generati automaticamente!)
/// 4. Eseguire ricerche semantiche
///
/// PATTERN IMPORTANTE:
/// Il vector store è configurato con un EmbeddingGenerator.
/// Quando fai Upsert o Search, l'embedding viene generato automaticamente
/// dalla proprietà VectorStoreVector del record.
/// </summary>
public sealed class TextSearchStore
{
    // ========================================================================
    // DIPENDENZE
    // ========================================================================

    /// <summary>
    /// Collezione nel vector store dove memorizziamo i chunk.
    /// InMemoryCollection gestisce automaticamente gli embeddings
    /// grazie all'EmbeddingGenerator configurato nel vector store.
    /// </summary>
    private readonly InMemoryCollection<string, TextSearchDocument> _collection;

    // ========================================================================
    // CONFIGURAZIONE CHUNKING
    // ========================================================================

    /// <summary>
    /// Dimensione massima di ogni chunk in caratteri.
    ///
    /// TRADE-OFF:
    /// - Chunk piccoli (300-500): ricerca più precisa, ma perde contesto
    /// - Chunk grandi (1000-2000): più contesto, ma meno preciso
    ///
    /// 800 è un buon compromesso per documentazione tecnica.
    /// </summary>
    private const int MaxChunkSize = 800;

    /// <summary>
    /// Overlap tra chunk consecutivi in caratteri.
    ///
    /// L'overlap serve a:
    /// - Non perdere informazioni ai confini dei chunk
    /// - Migliorare la continuità del contesto
    ///
    /// 100 caratteri = circa 2-3 righe di codice
    /// </summary>
    private const int ChunkOverlap = 100;

    /// <summary>
    /// Nome della collezione nel vector store.
    /// </summary>
    private const string CollectionName = "code_review_knowledge";

    // ========================================================================
    // COSTRUTTORE
    // ========================================================================

    /// <summary>
    /// Crea un nuovo TextSearchStore.
    /// </summary>
    /// <param name="vectorStore">Vector store con EmbeddingGenerator configurato</param>
    public TextSearchStore(InMemoryVectorStore vectorStore)
    {
        // Ottiene (o crea) la collezione per i nostri documenti
        // TextSearchDocument è il tipo di record che memorizziamo
        // string è il tipo della chiave (ChunkId)
        _collection = vectorStore.GetCollection<string, TextSearchDocument>(CollectionName);
    }

    // ========================================================================
    // INGESTION: CARICAMENTO KNOWLEDGE BASE
    // ========================================================================

    /// <summary>
    /// Carica tutti i documenti markdown dalla cartella KnowledgeBase.
    ///
    /// PROCESSO:
    /// 1. Trova tutti i file .md nella cartella
    /// 2. Per ogni file: leggi, chunka, memorizza
    /// 3. Gli embeddings vengono generati automaticamente!
    /// </summary>
    /// <param name="knowledgeBasePath">Percorso della cartella KnowledgeBase</param>
    /// <returns>Numero di chunk caricati</returns>
    public async Task<int> LoadKnowledgeBaseAsync(string knowledgeBasePath)
    {
        // Assicuriamoci che la collezione esista
        // CreateIfNotExists: crea solo se non esiste già
        await _collection.EnsureCollectionExistsAsync();

        // Trova tutti i file markdown
        var markdownFiles = Directory.GetFiles(knowledgeBasePath, "*.md");

        if (markdownFiles.Length == 0)
        {
            Console.WriteLine($"⚠️  Nessun file .md trovato in {knowledgeBasePath}");
            return 0;
        }

        Console.WriteLine($"📚 Trovati {markdownFiles.Length} documenti nella knowledge base");

        var totalChunks = 0;

        foreach (var filePath in markdownFiles)
        {
            var chunksLoaded = await LoadDocumentAsync(filePath);
            totalChunks += chunksLoaded;
        }

        Console.WriteLine($"✅ Caricati {totalChunks} chunk totali nel vector store");

        return totalChunks;
    }

    /// <summary>
    /// Carica un singolo documento markdown.
    /// </summary>
    /// <param name="filePath">Percorso del file .md</param>
    /// <returns>Numero di chunk creati</returns>
    private async Task<int> LoadDocumentAsync(string filePath)
    {
        // Estrai informazioni dal file
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var content = await File.ReadAllTextAsync(filePath);

        // Estrai il titolo dal primo heading markdown (# Titolo)
        var title = ExtractTitle(content) ?? fileName;

        Console.WriteLine($"  📄 Caricando: {title}");

        // Dividi il documento in chunk
        var chunks = ChunkDocument(content);

        Console.WriteLine($"     → {chunks.Count} chunk creati");

        // Crea e memorizza i documenti per ogni chunk
        var chunkCount = 0;
        foreach (var (text, index) in chunks.Select((t, i) => (t, i)))
        {
            var document = new TextSearchDocument
            {
                ChunkId = $"{fileName}_{index}",
                DocumentId = fileName,
                Title = title,
                FilePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath),
                ChunkIndex = index,
                Text = text
            };

            // UpsertAsync: inserisce o aggiorna se esiste già
            // L'embedding viene generato AUTOMATICAMENTE dal vector store
            // usando la proprietà EmbeddingText del documento!
            await _collection.UpsertAsync(document);
            chunkCount++;

            // Mostra progresso
            Console.Write($"\r     → Embedding chunk {chunkCount}/{chunks.Count}");
        }

        Console.WriteLine(); // Nuova linea dopo il progresso

        return chunkCount;
    }

    // ========================================================================
    // CHUNKING: DIVISIONE DOCUMENTI
    // ========================================================================

    /// <summary>
    /// Divide un documento in chunk di dimensione gestibile.
    ///
    /// STRATEGIA DI CHUNKING:
    /// 1. Dividi per sezioni markdown (## heading)
    /// 2. Se una sezione è troppo grande, dividi per paragrafi
    /// 3. Mantieni overlap tra chunk consecutivi
    ///
    /// Questo preserva la struttura logica del documento.
    /// </summary>
    /// <param name="content">Contenuto del documento</param>
    /// <returns>Lista di chunk testuali</returns>
    private List<string> ChunkDocument(string content)
    {
        var chunks = new List<string>();

        // Prima dividiamo per sezioni markdown (## heading)
        // Questo preserva la struttura logica del documento
        var sections = SplitByMarkdownSections(content);

        foreach (var section in sections)
        {
            // Se la sezione è piccola abbastanza, usala così com'è
            if (section.Length <= MaxChunkSize)
            {
                if (!string.IsNullOrWhiteSpace(section))
                {
                    chunks.Add(section.Trim());
                }
            }
            else
            {
                // Sezione troppo grande: dividi ulteriormente
                var subChunks = SplitLargeSection(section);
                chunks.AddRange(subChunks);
            }
        }

        return chunks;
    }

    /// <summary>
    /// Divide il contenuto per sezioni markdown (## heading).
    /// </summary>
    private List<string> SplitByMarkdownSections(string content)
    {
        var sections = new List<string>();

        // Pattern per heading markdown: # o ## o ### etc.
        // (?=^#{1,3}\s) = lookahead per trovare heading senza consumarlo
        var pattern = @"(?=^#{1,3}\s)";
        var parts = Regex.Split(content, pattern, RegexOptions.Multiline);

        foreach (var part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                sections.Add(part.Trim());
            }
        }

        return sections;
    }

    /// <summary>
    /// Divide una sezione grande in chunk più piccoli con overlap.
    /// </summary>
    private List<string> SplitLargeSection(string section)
    {
        var chunks = new List<string>();
        var currentPosition = 0;

        while (currentPosition < section.Length)
        {
            // Calcola la fine del chunk
            var endPosition = Math.Min(currentPosition + MaxChunkSize, section.Length);

            // Se non siamo alla fine, cerca un punto di rottura naturale
            // (fine paragrafo, fine frase, spazio)
            if (endPosition < section.Length)
            {
                // Cerca fine paragrafo (doppio newline)
                var breakPoint = section.LastIndexOf("\n\n", endPosition, endPosition - currentPosition);

                if (breakPoint <= currentPosition)
                {
                    // Cerca fine frase (. ! ?)
                    breakPoint = section.LastIndexOfAny(new[] { '.', '!', '?' }, endPosition - 1, endPosition - currentPosition);
                }

                if (breakPoint <= currentPosition)
                {
                    // Cerca spazio
                    breakPoint = section.LastIndexOf(' ', endPosition - 1, endPosition - currentPosition);
                }

                if (breakPoint > currentPosition)
                {
                    endPosition = breakPoint + 1;
                }
            }

            // Estrai il chunk
            var chunk = section.Substring(currentPosition, endPosition - currentPosition).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            // Avanza con overlap
            currentPosition = endPosition - ChunkOverlap;

            // Evita loop infiniti
            if (currentPosition <= 0 || currentPosition >= section.Length)
            {
                break;
            }
        }

        return chunks;
    }

    // ========================================================================
    // RETRIEVAL: RICERCA SEMANTICA
    // ========================================================================

    /// <summary>
    /// Cerca i chunk più rilevanti per una query.
    ///
    /// PROCESSO (tutto automatico!):
    /// 1. Il vector store genera l'embedding della query
    /// 2. Cerca per similarità coseno nel vector store
    /// 3. Ritorna i top-K chunk più simili
    ///
    /// La ricerca semantica trova documenti rilevanti anche se
    /// non contengono le parole esatte della query.
    /// Es: "gestione eccezioni" trova documenti su "error handling"
    /// </summary>
    /// <param name="query">Query di ricerca</param>
    /// <param name="topK">Numero massimo di risultati</param>
    /// <param name="minScore">Score minimo di similarità (0-1)</param>
    /// <returns>Lista di chunk rilevanti con score</returns>
    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int topK = 3,
        float minScore = 0.3f)
    {
        // Configura la ricerca
        // IncludeVectors = false perché non ci servono i vettori raw
        var searchOptions = new VectorSearchOptions<TextSearchDocument>
        {
            IncludeVectors = false
        };

        // Esegui ricerca semantica
        // SearchAsync(query, topK, options):
        // - query: stringa di ricerca
        // - topK: numero di risultati da restituire
        // - options: opzioni di ricerca
        // Il vector store genera automaticamente l'embedding della query!
        //
        // NOTA: SearchAsync ritorna IAsyncEnumerable<VectorSearchResult<T>>,
        // non un Task! Quindi NON si usa await qui, ma await foreach per iterare.
        var searchResults = _collection.SearchAsync(query, topK, searchOptions);

        // Filtra per score minimo e converti in nostro formato
        var results = new List<SearchResult>();

        await foreach (var result in searchResults)
        {
            // Score è la similarità coseno (0-1, dove 1 = identico)
            if (result.Score >= minScore)
            {
                results.Add(new SearchResult
                {
                    Document = result.Record,
                    Score = (float)result.Score!
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Genera il contesto da iniettare all'agente basato sulla query.
    ///
    /// Questo metodo:
    /// 1. Cerca i chunk rilevanti
    /// 2. Li formatta come contesto per l'LLM
    /// 3. Include citazioni delle fonti
    /// </summary>
    /// <param name="query">Query dell'utente</param>
    /// <param name="topK">Numero di chunk da includere</param>
    /// <returns>Contesto formattato per l'agente</returns>
    public async Task<string> GetContextForQueryAsync(string query, int topK = 3)
    {
        var results = await SearchAsync(query, topK);

        if (results.Count == 0)
        {
            return string.Empty;
        }

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("=== KNOWLEDGE BASE CONTEXT ===");
        contextBuilder.AppendLine();

        foreach (var result in results)
        {
            contextBuilder.AppendLine($"[Source: {result.Document.Title}]");
            contextBuilder.AppendLine($"[File: {result.Document.FilePath}]");
            contextBuilder.AppendLine($"[Relevance: {result.Score:P0}]");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine(result.Document.Text);
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("---");
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    // ========================================================================
    // UTILITY METHODS
    // ========================================================================

    /// <summary>
    /// Estrae il titolo dal primo heading markdown.
    /// Es: "# SOLID Principles" → "SOLID Principles"
    /// </summary>
    private string? ExtractTitle(string content)
    {
        // Cerca pattern: # Titolo
        var match = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}

/// <summary>
/// Risultato di una ricerca nel vector store.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Il documento (chunk) trovato.
    /// </summary>
    public required TextSearchDocument Document { get; init; }

    /// <summary>
    /// Score di similarità (0-1, dove 1 = perfettamente simile).
    /// </summary>
    public required float Score { get; init; }
}
