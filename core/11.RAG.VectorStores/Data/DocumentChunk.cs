// ============================================================================
// 11. RAG WITH REAL VECTOR STORES
// FILE: Data/DocumentChunk.cs
// ============================================================================
//
// DATA MODEL FOR DOCUMENT CHUNKS
//
// This class represents a document fragment to be indexed in the
// vector store. Each chunk contains:
// - A unique ID (Guid for compatibility with Qdrant)
// - The content text
// - Metadata (title, category, etc.)
// - The embedding vector (generated manually)
//
// IMPORTANT: Unlike InMemoryVectorStore, the Qdrant and
// SQL Server connectors require embeddings to be generated manually
// BEFORE insertion into the vector store.
//
// ============================================================================

using Microsoft.Extensions.VectorData;

namespace _11.RAG.VectorStores.Data;

/// <summary>
/// Represents a document chunk indexed in the vector store.
/// </summary>
/// <remarks>
/// The structure is optimized for RAG scenarios:
/// - Title and Category are metadata for filtering
/// - Content contains the text to pass to the LLM as context
/// - Embedding contains the vector for semantic search
/// </remarks>
public sealed class DocumentChunk
{
    // ========================================================================
    // PRIMARY KEY
    // ========================================================================
    // [VectorStoreKey] indicates the field used as unique identifier.
    //
    // NOTE: Qdrant only supports Guid or ulong keys (not string).
    // We use Guid for compatibility with both vector stores.

    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.Empty;

    // ========================================================================
    // DATA FIELDS (METADATA)
    // ========================================================================
    // [VectorStoreData] indicates fields that contain textual data.
    // These fields are stored and returned in results.

    /// <summary>
    /// Title of the source document.
    /// </summary>
    [VectorStoreData]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Category of the document (e.g. "programming", "database", "ai").
    /// </summary>
    [VectorStoreData]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Text content of the chunk.
    /// </summary>
    /// <remarks>
    /// This is the text that will be passed to the LLM as context.
    /// </remarks>
    [VectorStoreData]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Index of the chunk within the original document.
    /// </summary>
    [VectorStoreData]
    public int ChunkIndex { get; set; }

    // ========================================================================
    // EMBEDDING VECTOR
    // ========================================================================
    // [VectorStoreVector] indicates the field that contains the embedding.
    //
    // IMPORTANT: For Qdrant and SQL Server, the embedding must be a
    // ReadOnlyMemory<float>, NOT a string. Embeddings must be
    // generated manually BEFORE insertion.
    //
    // DIMENSIONS: 1536 for text-embedding-3-small (OpenAI)
    // DISTANCE: CosineSimilarity for texts
    // INDEX: Hnsw for fast search

    /// <summary>
    /// Embedding vector of the content.
    /// </summary>
    /// <remarks>
    /// Must be populated manually using an embedding generator
    /// before inserting the document into the vector store.
    /// </remarks>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Embedding { get; set; }

    // ========================================================================
    // EMBEDDING HELPER
    // ========================================================================

    /// <summary>
    /// Generates the text to use for creating the embedding.
    /// </summary>
    /// <remarks>
    /// Combines title, category and content for a richer embedding.
    /// Use this method to generate the embedding before insertion.
    /// </remarks>
    public string GetTextForEmbedding() => $"Title: {Title}\nCategory: {Category}\nContent: {Content}";
}
