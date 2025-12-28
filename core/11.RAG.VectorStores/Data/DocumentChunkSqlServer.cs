// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// FILE: Data/DocumentChunkSqlServer.cs
// ============================================================================
//
// MODELLO DATI OTTIMIZZATO PER SQL SERVER
//
// SQL Server 2022 supporta:
// - Chiavi di vari tipi (string, Guid, int, ecc.)
// - Indici Flat (ricerca esatta, no HNSW)
// - CosineSimilarity, DotProduct, Euclidean
//
// NOTA: SQL Server non supporta HNSW, quindi usiamo Flat.
//
// ============================================================================

using Microsoft.Extensions.VectorData;

namespace _11.RAG.VectorStores.Data;

/// <summary>
/// Chunk di documento ottimizzato per SQL Server.
/// </summary>
public sealed class DocumentChunkSqlServer
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
    /// Embedding con indice Flat (richiesto da SQL Server).
    /// </summary>
    /// <remarks>
    /// SQL Server non supporta HNSW, quindi usa ricerca esatta (Flat).
    /// Per dataset piccoli/medi la differenza di performance è trascurabile.
    /// </remarks>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Flat)]
    public ReadOnlyMemory<float> Embedding { get; set; }

    public string GetTextForEmbedding() => $"Title: {Title}\nCategory: {Category}\nContent: {Content}";
}
