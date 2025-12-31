// ============================================================================
// 05. CODE REVIEWER - RAG
// FILE: TextSearchDocument.cs
// ============================================================================
// This file defines the model for knowledge base documents.
//
// KEY CONCEPTS:
// - VectorStoreKey: indicates the unique key field
// - VectorStoreData: indicates additional fields to store
// - VectorStoreVector: indicates the field that generates the embedding
//
// IMPORTANT: The VectorStoreVector is a string property!
// The embedding is generated automatically by the vector store
// when it has an EmbeddingGenerator configured.
//
// The document is "chunked" (split into pieces) to:
// 1. Respect the embedding model limits
// 2. Have more precise searches (smaller chunks = more relevant)
// 3. Allow accurate source citations
// ============================================================================

using Microsoft.Extensions.VectorData;

namespace CodeReviewer.RAG.Rag;

/// <summary>
/// Represents a document chunk (piece) in the knowledge base.
///
/// RAG ARCHITECTURE:
/// ┌─────────────────┐
/// │ Document.md     │ ← Original file (e.g.: solid-principles.md)
/// └────────┬────────┘
///          │ Chunking
///          ▼
/// ┌─────────────────┐
/// │ Chunk 1         │ ← First piece of the document
/// │ Chunk 2         │ ← Second piece
/// │ ...             │
/// └────────┬────────┘
///          │ Embedding (automatic!)
///          ▼
/// ┌─────────────────┐
/// │ Vector Store    │ ← Semantic search on chunks
/// └─────────────────┘
///
/// IMPORTANT PATTERN:
/// The EmbeddingText property is a string that is automatically
/// converted to an embedding by the vector store. You don't need to
/// manually handle the float[] vectors!
/// </summary>
public sealed class TextSearchDocument
{
    // ========================================================================
    // PRIMARY KEY
    // ========================================================================

    /// <summary>
    /// Unique chunk identifier.
    /// Format: "{DocumentId}_{ChunkIndex}"
    /// E.g.: "solid-principles_0", "solid-principles_1", etc.
    ///
    /// NOTE: VectorStoreKey indicates this is the primary key field.
    /// </summary>
    [VectorStoreKey]
    public required string ChunkId { get; init; }

    // ========================================================================
    // DOCUMENT METADATA
    // ========================================================================

    /// <summary>
    /// Original document identifier (file name without extension).
    /// E.g.: "solid-principles", "async-best-practices"
    ///
    /// Useful for:
    /// - Grouping chunks from the same document
    /// - Citing the source in responses
    /// </summary>
    [VectorStoreData]
    public required string DocumentId { get; init; }

    /// <summary>
    /// Document title (extracted from the first # heading of the markdown).
    /// E.g.: "SOLID Principles", "Async/Await Best Practices in C#"
    /// </summary>
    [VectorStoreData]
    public required string Title { get; init; }

    /// <summary>
    /// Original file path relative to the knowledge base.
    /// E.g.: "KnowledgeBase/solid-principles.md"
    ///
    /// Allows to:
    /// - Indicate the exact source to the Code Reviewer
    /// - Potentially reload the document
    /// </summary>
    [VectorStoreData]
    public required string FilePath { get; init; }

    /// <summary>
    /// Chunk index within the document (0-based).
    /// E.g.: 0, 1, 2, ...
    ///
    /// Useful for reconstructing the original order if needed.
    /// </summary>
    [VectorStoreData]
    public required int ChunkIndex { get; init; }

    // ========================================================================
    // CHUNK CONTENT
    // ========================================================================

    /// <summary>
    /// Chunk text.
    /// This is the actual content that is injected
    /// into the agent's context when relevant.
    ///
    /// TYPICAL SIZE: 500-1000 characters per chunk
    /// - Too small = loses context
    /// - Too large = less precise in search
    /// </summary>
    [VectorStoreData]
    public required string Text { get; init; }

    // ========================================================================
    // AUTOMATIC EMBEDDING
    // ========================================================================

    /// <summary>
    /// Text used to generate the embedding.
    ///
    /// IMPORTANT PATTERN:
    /// This property is marked with [VectorStoreVector] and returns string.
    /// The vector store (when it has an EmbeddingGenerator configured)
    /// automatically converts this string into a float[] vector.
    ///
    /// We include title + text to have richer embeddings
    /// that capture both the document context and the content.
    ///
    /// EMBEDDING DIMENSIONS:
    /// - 1536: text-embedding-3-small (OpenAI)
    /// - 3072: text-embedding-3-large (OpenAI)
    ///
    /// DistanceFunction.CosineSimilarity:
    /// - Measures the angle between vectors (independent of length)
    /// - Values: 1.0 = identical, 0.0 = orthogonal
    ///
    /// IndexKind.Hnsw:
    /// - Hierarchical Navigable Small World
    /// - Efficient algorithm for approximate search
    /// </summary>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public string? EmbeddingText => $"Title: {Title}\nContent: {Text}";

    // ========================================================================
    // UTILITY METHODS
    // ========================================================================

    /// <summary>
    /// Text representation for debugging and logging.
    /// </summary>
    public override string ToString()
    {
        // Show only the first 50 characters of text for brevity
        var preview = Text.Length > 50
            ? Text[..50] + "..."
            : Text;

        return $"[{ChunkId}] {Title}: {preview}";
    }
}
