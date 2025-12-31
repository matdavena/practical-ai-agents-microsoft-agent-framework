// ============================================================================
// 11. RAG WITH REAL VECTOR STORES
// FILE: Data/DocumentChunkPostgres.cs
// ============================================================================
//
// DATA MODEL OPTIMIZED FOR POSTGRESQL + PGVECTOR
//
// PostgreSQL with pgvector is the most popular open source solution for
// adding vector search capabilities to a relational database.
//
// PGVECTOR FEATURES:
// - Native VECTOR data type
// - Supported indexes: HNSW (recommended), IVFFlat
// - Distance operators: <-> (L2), <#> (inner product), <=> (cosine)
// - Support for millions of vectors with good performance
//
// ADVANTAGES OVER OTHER RELATIONAL DBS:
// - Open source and free
// - Mature and well-documented extension
// - Active and growing community
// - Supported by all major cloud providers
// - Can use the same SQL queries for structured data and vectors
//
// ============================================================================

using Microsoft.Extensions.VectorData;

namespace _11.RAG.VectorStores.Data;

/// <summary>
/// Document chunk optimized for PostgreSQL + pgvector.
/// </summary>
/// <remarks>
/// PostgreSQL supports both HNSW and IVFFlat as index types.
/// HNSW is generally preferred for the best speed/accuracy tradeoff.
/// </remarks>
public sealed class DocumentChunkPostgres
{
    /// <summary>
    /// Unique primary key.
    /// </summary>
    /// <remarks>
    /// PostgreSQL supports various key types. We use Guid for
    /// consistency with other vector stores in the project.
    /// </remarks>
    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// Title of the source document.
    /// </summary>
    [VectorStoreData]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Category of the document.
    /// </summary>
    [VectorStoreData]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Text content of the chunk.
    /// </summary>
    [VectorStoreData]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Index of the chunk in the original document.
    /// </summary>
    [VectorStoreData]
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Embedding vector with HNSW index.
    /// </summary>
    /// <remarks>
    /// pgvector supports HNSW (Hierarchical Navigable Small World):
    /// - Very fast approximate search
    /// - Good tradeoff between speed and accuracy
    /// - Recommended for most use cases
    ///
    /// Alternative: IndexKind.IvfFlat for very large datasets
    /// where memory is a constraint.
    /// </remarks>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Embedding { get; set; }

    /// <summary>
    /// Generates the text to use for creating the embedding.
    /// </summary>
    public string GetTextForEmbedding() => $"Title: {Title}\nCategory: {Category}\nContent: {Content}";
}
