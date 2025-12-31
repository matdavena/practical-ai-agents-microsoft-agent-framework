// ============================================================================
// 05. CODE REVIEWER - RAG
// FILE: KnowledgeBaseProvider.cs
// ============================================================================
// This file implements the AIContextProvider for RAG.
//
// RAG + AGENT INTEGRATION:
//
// ┌─────────────────────────────────────────────────────────────────────────┐
// │                          RAG FLOW                                       │
// └─────────────────────────────────────────────────────────────────────────┘
//
//    1. User asks question         2. Provider intercepts
//    ┌────────────────┐            ┌────────────────┐
//    │ "How to handle │ ────────►  │ InvokingAsync  │
//    │  exceptions?"  │            │ (before LLM)   │
//    └────────────────┘            └───────┬────────┘
//                                          │
//    3. Semantic search                    ▼
//    ┌────────────────────────────────────────────────────┐
//    │ TextSearchStore.SearchAsync("exceptions")          │
//    │ → Found: error-handling.md (score: 0.89)           │
//    │ → Found: async-best-practices.md (score: 0.72)     │
//    └───────────────────────────┬────────────────────────┘
//                                │
//    4. Inject in context        ▼
//    ┌────────────────────────────────────────────────────┐
//    │ AIContext.Instructions += knowledge base context   │
//    └───────────────────────────┬────────────────────────┘
//                                │
//    5. LLM responds             ▼
//    ┌────────────────────────────────────────────────────┐
//    │ "According to best practices (error-handling.md),  │
//    │  you should always catch specific exceptions..."   │
//    └────────────────────────────────────────────────────┘
//
// ============================================================================

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CodeReviewer.RAG.Rag;

/// <summary>
/// AIContextProvider that implements RAG (Retrieval-Augmented Generation).
///
/// This provider:
/// 1. Intercepts each user request (InvokingAsync)
/// 2. Searches for relevant content in the knowledge base
/// 3. Injects the found context into the LLM instructions
///
/// RAG BENEFITS:
/// - The agent has access to updated knowledge (not just training data)
/// - Responses are based on specific and citable sources
/// - No fine-tuning needed to add new knowledge
/// </summary>
public class KnowledgeBaseProvider : AIContextProvider
{
    // ========================================================================
    // DEPENDENCIES
    // ========================================================================

    /// <summary>
    /// Store for searching the knowledge base.
    /// </summary>
    private readonly TextSearchStore _searchStore;

    /// <summary>
    /// Number of chunks to retrieve per query.
    /// </summary>
    private readonly int _topK;

    /// <summary>
    /// Minimum similarity score to include a chunk.
    /// </summary>
    private readonly float _minScore;

    // ========================================================================
    // CONSTRUCTOR
    // ========================================================================

    /// <summary>
    /// Creates a new KnowledgeBaseProvider.
    /// </summary>
    /// <param name="searchStore">Store for semantic search</param>
    /// <param name="topK">Number of results to include (default: 3)</param>
    /// <param name="minScore">Minimum similarity score (default: 0.5)</param>
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
    /// Called BEFORE each request to the LLM.
    ///
    /// PROCESS:
    /// 1. Extract the latest user question
    /// 2. Search for relevant content in the knowledge base
    /// 3. Build the context to inject
    /// 4. Return AIContext with additional instructions
    /// </summary>
    public override async ValueTask<AIContext> InvokingAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        // Extract the latest user question from the conversation
        var userQuery = ExtractLatestUserQuery(context);

        if (string.IsNullOrEmpty(userQuery))
        {
            // No query found, we don't add context
            return new AIContext();
        }

        // Search for relevant content in the knowledge base
        var searchResults = await _searchStore.SearchAsync(
            userQuery,
            _topK,
            _minScore);

        if (searchResults.Count == 0)
        {
            // No relevant results found
            Console.WriteLine("  🔍 No relevant content found in the knowledge base");
            return new AIContext();
        }

        // Log the found results
        Console.WriteLine($"  🔍 Found {searchResults.Count} relevant chunks:");
        foreach (var result in searchResults)
        {
            Console.WriteLine($"     - {result.Document.Title} (score: {result.Score:F2})");
        }

        // Build the context to inject
        var knowledgeContext = BuildKnowledgeContext(searchResults);

        // Return the context as additional instructions
        // These instructions are added to the system prompt
        return new AIContext
        {
            Instructions = knowledgeContext
        };
    }

    // ========================================================================
    // LIFECYCLE: AFTER LLM CALL
    // ========================================================================

    /// <summary>
    /// Called AFTER each LLM response.
    ///
    /// For RAG we don't need to do anything after the response.
    /// We could use this hook for:
    /// - Response logging
    /// - Analytics on knowledge base usage
    /// - Feedback learning (which chunks were useful)
    /// </summary>
    public override ValueTask InvokedAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        // No action needed after the response
        return default;
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Extracts the latest user question from the conversation.
    /// </summary>
    private string? ExtractLatestUserQuery(InvokingContext context)
    {
        // Look for the last user message
        var lastUserMessage = context.RequestMessages
            .LastOrDefault(m => m.Role == ChatRole.User);

        if (lastUserMessage == null)
        {
            return null;
        }

        // Extract the text from the message
        // A message can contain multiple parts (text, images, etc.)
        var textParts = lastUserMessage.Contents
            .OfType<TextContent>()
            .Select(tc => tc.Text);

        return string.Join(" ", textParts);
    }

    /// <summary>
    /// Builds the context to inject to the agent.
    ///
    /// The format is important:
    /// - Clear for the LLM to interpret
    /// - Includes metadata for citations
    /// - Visual separators between chunks
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
