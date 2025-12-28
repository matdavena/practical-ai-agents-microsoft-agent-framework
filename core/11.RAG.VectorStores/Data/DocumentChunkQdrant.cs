// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// FILE: Data/DocumentChunkQdrant.cs
// ============================================================================
//
// MODELLO DATI OTTIMIZZATO PER QDRANT
//
// Qdrant supporta:
// - Chiavi Guid o ulong (non string)
// - Indici HNSW per ricerca veloce
// - CosineSimilarity, DotProduct, Euclidean
//
// ============================================================================

using Microsoft.Extensions.VectorData;

namespace _11.RAG.VectorStores.Data;

/// <summary>
/// Chunk di documento ottimizzato per Qdrant.
/// </summary>
public sealed class DocumentChunkQdrant
{
    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.Empty;

    [VectorStoreData]
    public string Title { get; set; } = string.Empty;

    [VectorStoreData]
    public string Category { get; set; } = string.Empty;

    [VectorStoreData]
    public string Content { get; set; } = string.Empty;

    [VectorStoreData]
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Embedding con indice HNSW (ottimale per Qdrant).
    /// </summary>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Embedding { get; set; }

    public string GetTextForEmbedding() => $"Title: {Title}\nCategory: {Category}\nContent: {Content}";
}
