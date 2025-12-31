// ============================================================================
// 05. CODE REVIEWER - RAG
// FILE: TextSearchStore.cs
// ============================================================================
// This file manages loading, chunking, and searching
// of knowledge base documents.
//
// COMPLETE RAG PIPELINE:
//
// 1. INGESTION (once at startup):
//    ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
//    │  Load MD     │ ──► │   Chunking   │ ──► │  Upsert in   │
//    │  files       │     │  documents   │     │ Vector Store │
//    └──────────────┘     └──────────────┘     └──────────────┘
//                                                     │
//                                                     ▼
//                                              ┌──────────────┐
//                                              │ Embedding    │
//                                              │ (automatic)  │
//                                              └──────────────┘
//
// 2. RETRIEVAL (on each query):
//    ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
//    │   User       │ ──► │   Semantic   │ ──► │ Top-K        │
//    │   query      │     │   search     │     │ relevant     │
//    └──────────────┘     └──────────────┘     └──────────────┘
//
// NOTE: Embeddings are generated AUTOMATICALLY by the vector store
// thanks to the EmbeddingGenerator configuration. You don't need to
// manually handle the float[] vectors!
// ============================================================================

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeReviewer.RAG.Rag;

/// <summary>
/// Store for semantic search in knowledge base documents.
///
/// RESPONSIBILITIES:
/// 1. Load markdown documents from the KnowledgeBase folder
/// 2. Split documents into manageable chunks
/// 3. Store in vector store (embeddings generated automatically!)
/// 4. Execute semantic searches
///
/// IMPORTANT PATTERN:
/// The vector store is configured with an EmbeddingGenerator.
/// When you do Upsert or Search, the embedding is automatically generated
/// from the VectorStoreVector property of the record.
/// </summary>
public sealed class TextSearchStore
{
    // ========================================================================
    // DEPENDENCIES
    // ========================================================================

    /// <summary>
    /// Collection in the vector store where we store chunks.
    /// InMemoryCollection automatically handles embeddings
    /// thanks to the EmbeddingGenerator configured in the vector store.
    /// </summary>
    private readonly InMemoryCollection<string, TextSearchDocument> _collection;

    // ========================================================================
    // CHUNKING CONFIGURATION
    // ========================================================================

    /// <summary>
    /// Maximum size of each chunk in characters.
    ///
    /// TRADE-OFF:
    /// - Small chunks (300-500): more precise search, but loses context
    /// - Large chunks (1000-2000): more context, but less precise
    ///
    /// 800 is a good compromise for technical documentation.
    /// </summary>
    private const int MaxChunkSize = 800;

    /// <summary>
    /// Overlap between consecutive chunks in characters.
    ///
    /// The overlap serves to:
    /// - Not lose information at chunk boundaries
    /// - Improve context continuity
    ///
    /// 100 characters = approximately 2-3 lines of code
    /// </summary>
    private const int ChunkOverlap = 100;

    /// <summary>
    /// Name of the collection in the vector store.
    /// </summary>
    private const string CollectionName = "code_review_knowledge";

    // ========================================================================
    // CONSTRUCTOR
    // ========================================================================

    /// <summary>
    /// Creates a new TextSearchStore.
    /// </summary>
    /// <param name="vectorStore">Vector store with configured EmbeddingGenerator</param>
    public TextSearchStore(InMemoryVectorStore vectorStore)
    {
        // Gets (or creates) the collection for our documents
        // TextSearchDocument is the type of record we store
        // string is the type of the key (ChunkId)
        _collection = vectorStore.GetCollection<string, TextSearchDocument>(CollectionName);
    }

    // ========================================================================
    // INGESTION: LOADING KNOWLEDGE BASE
    // ========================================================================

    /// <summary>
    /// Loads all markdown documents from the KnowledgeBase folder.
    ///
    /// PROCESS:
    /// 1. Find all .md files in the folder
    /// 2. For each file: read, chunk, store
    /// 3. Embeddings are generated automatically!
    /// </summary>
    /// <param name="knowledgeBasePath">Path to the KnowledgeBase folder</param>
    /// <returns>Number of chunks loaded</returns>
    public async Task<int> LoadKnowledgeBaseAsync(string knowledgeBasePath)
    {
        // Make sure the collection exists
        // CreateIfNotExists: creates only if it doesn't exist already
        await _collection.EnsureCollectionExistsAsync();

        // Find all markdown files
        var markdownFiles = Directory.GetFiles(knowledgeBasePath, "*.md");

        if (markdownFiles.Length == 0)
        {
            Console.WriteLine($"⚠️  No .md files found in {knowledgeBasePath}");
            return 0;
        }

        Console.WriteLine($"📚 Found {markdownFiles.Length} documents in the knowledge base");

        var totalChunks = 0;

        foreach (var filePath in markdownFiles)
        {
            var chunksLoaded = await LoadDocumentAsync(filePath);
            totalChunks += chunksLoaded;
        }

        Console.WriteLine($"✅ Loaded {totalChunks} total chunks in the vector store");

        return totalChunks;
    }

    /// <summary>
    /// Loads a single markdown document.
    /// </summary>
    /// <param name="filePath">Path to the .md file</param>
    /// <returns>Number of chunks created</returns>
    private async Task<int> LoadDocumentAsync(string filePath)
    {
        // Extract information from the file
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var content = await File.ReadAllTextAsync(filePath);

        // Extract the title from the first markdown heading (# Title)
        var title = ExtractTitle(content) ?? fileName;

        Console.WriteLine($"  📄 Loading: {title}");

        // Split the document into chunks
        var chunks = ChunkDocument(content);

        Console.WriteLine($"     → {chunks.Count} chunks created");

        // Create and store documents for each chunk
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

            // UpsertAsync: inserts or updates if it already exists
            // The embedding is generated AUTOMATICALLY by the vector store
            // using the EmbeddingText property of the document!
            await _collection.UpsertAsync(document);
            chunkCount++;

            // Show progress
            Console.Write($"\r     → Embedding chunk {chunkCount}/{chunks.Count}");
        }

        Console.WriteLine(); // New line after progress

        return chunkCount;
    }

    // ========================================================================
    // CHUNKING: SPLITTING DOCUMENTS
    // ========================================================================

    /// <summary>
    /// Splits a document into manageable-sized chunks.
    ///
    /// CHUNKING STRATEGY:
    /// 1. Split by markdown sections (## heading)
    /// 2. If a section is too large, split by paragraphs
    /// 3. Maintain overlap between consecutive chunks
    ///
    /// This preserves the logical structure of the document.
    /// </summary>
    /// <param name="content">Document content</param>
    /// <returns>List of text chunks</returns>
    private List<string> ChunkDocument(string content)
    {
        var chunks = new List<string>();

        // First we split by markdown sections (## heading)
        // This preserves the logical structure of the document
        var sections = SplitByMarkdownSections(content);

        foreach (var section in sections)
        {
            // If the section is small enough, use it as is
            if (section.Length <= MaxChunkSize)
            {
                if (!string.IsNullOrWhiteSpace(section))
                {
                    chunks.Add(section.Trim());
                }
            }
            else
            {
                // Section too large: split further
                var subChunks = SplitLargeSection(section);
                chunks.AddRange(subChunks);
            }
        }

        return chunks;
    }

    /// <summary>
    /// Splits content by markdown sections (## heading).
    /// </summary>
    private List<string> SplitByMarkdownSections(string content)
    {
        var sections = new List<string>();

        // Pattern for markdown headings: # or ## or ### etc.
        // (?=^#{1,3}\s) = lookahead to find heading without consuming it
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
    /// Splits a large section into smaller chunks with overlap.
    /// </summary>
    private List<string> SplitLargeSection(string section)
    {
        var chunks = new List<string>();
        var currentPosition = 0;

        while (currentPosition < section.Length)
        {
            // Calculate the end of the chunk
            var endPosition = Math.Min(currentPosition + MaxChunkSize, section.Length);

            // If we're not at the end, look for a natural break point
            // (end of paragraph, end of sentence, space)
            if (endPosition < section.Length)
            {
                // Look for end of paragraph (double newline)
                var breakPoint = section.LastIndexOf("\n\n", endPosition, endPosition - currentPosition);

                if (breakPoint <= currentPosition)
                {
                    // Look for end of sentence (. ! ?)
                    breakPoint = section.LastIndexOfAny(new[] { '.', '!', '?' }, endPosition - 1, endPosition - currentPosition);
                }

                if (breakPoint <= currentPosition)
                {
                    // Look for space
                    breakPoint = section.LastIndexOf(' ', endPosition - 1, endPosition - currentPosition);
                }

                if (breakPoint > currentPosition)
                {
                    endPosition = breakPoint + 1;
                }
            }

            // Extract the chunk
            var chunk = section.Substring(currentPosition, endPosition - currentPosition).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            // Advance with overlap
            currentPosition = endPosition - ChunkOverlap;

            // Avoid infinite loops
            if (currentPosition <= 0 || currentPosition >= section.Length)
            {
                break;
            }
        }

        return chunks;
    }

    // ========================================================================
    // RETRIEVAL: SEMANTIC SEARCH
    // ========================================================================

    /// <summary>
    /// Searches for the most relevant chunks for a query.
    ///
    /// PROCESS (all automatic!):
    /// 1. The vector store generates the query embedding
    /// 2. Searches by cosine similarity in the vector store
    /// 3. Returns the top-K most similar chunks
    ///
    /// Semantic search finds relevant documents even if
    /// they don't contain the exact words from the query.
    /// E.g.: "exception handling" finds documents about "error handling"
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="topK">Maximum number of results</param>
    /// <param name="minScore">Minimum similarity score (0-1)</param>
    /// <returns>List of relevant chunks with scores</returns>
    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int topK = 3,
        float minScore = 0.3f)
    {
        // Configure the search
        // IncludeVectors = false because we don't need the raw vectors
        var searchOptions = new VectorSearchOptions<TextSearchDocument>
        {
            IncludeVectors = false
        };

        // Execute semantic search
        // SearchAsync(query, topK, options):
        // - query: search string
        // - topK: number of results to return
        // - options: search options
        // The vector store automatically generates the query embedding!
        //
        // NOTE: SearchAsync returns IAsyncEnumerable<VectorSearchResult<T>>,
        // not a Task! So we DON'T use await here, but await foreach to iterate.
        var searchResults = _collection.SearchAsync(query, topK, searchOptions);

        // Filter by minimum score and convert to our format
        var results = new List<SearchResult>();

        await foreach (var result in searchResults)
        {
            // Score is cosine similarity (0-1, where 1 = identical)
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
    /// Generates the context to inject to the agent based on the query.
    ///
    /// This method:
    /// 1. Searches for relevant chunks
    /// 2. Formats them as context for the LLM
    /// 3. Includes source citations
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="topK">Number of chunks to include</param>
    /// <returns>Formatted context for the agent</returns>
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
    /// Extracts the title from the first markdown heading.
    /// E.g.: "# SOLID Principles" → "SOLID Principles"
    /// </summary>
    private string? ExtractTitle(string content)
    {
        // Search for pattern: # Title
        var match = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}

/// <summary>
/// Result of a search in the vector store.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The document (chunk) found.
    /// </summary>
    public required TextSearchDocument Document { get; init; }

    /// <summary>
    /// Similarity score (0-1, where 1 = perfectly similar).
    /// </summary>
    public required float Score { get; init; }
}
