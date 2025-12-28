// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// FILE: Data/DocumentChunkPostgres.cs
// ============================================================================
//
// MODELLO DATI OTTIMIZZATO PER POSTGRESQL + PGVECTOR
//
// PostgreSQL con pgvector è la soluzione open source più popolare per
// aggiungere capacità di ricerca vettoriale a un database relazionale.
//
// CARATTERISTICHE PGVECTOR:
// - Tipo di dato VECTOR nativo
// - Indici supportati: HNSW (consigliato), IVFFlat
// - Operatori di distanza: <-> (L2), <#> (inner product), <=> (cosine)
// - Supporto per milioni di vettori con buone performance
//
// VANTAGGI RISPETTO AD ALTRI DB RELAZIONALI:
// - Open source e gratuito
// - Estensione matura e ben documentata
// - Community attiva e in crescita
// - Supportato da tutti i major cloud provider
// - Può usare le stesse query SQL per dati strutturati e vettori
//
// ============================================================================

using Microsoft.Extensions.VectorData;

namespace _11.RAG.VectorStores.Data;

/// <summary>
/// Chunk di documento ottimizzato per PostgreSQL + pgvector.
/// </summary>
/// <remarks>
/// PostgreSQL supporta sia HNSW che IVFFlat come tipi di indice.
/// HNSW è generalmente preferito per il miglior compromesso velocità/accuratezza.
/// </remarks>
public sealed class DocumentChunkPostgres
{
    /// <summary>
    /// Chiave primaria univoca.
    /// </summary>
    /// <remarks>
    /// PostgreSQL supporta vari tipi di chiave. Usiamo Guid per
    /// coerenza con gli altri vector store nel progetto.
    /// </remarks>
    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// Titolo del documento sorgente.
    /// </summary>
    [VectorStoreData]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Categoria del documento.
    /// </summary>
    [VectorStoreData]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Contenuto testuale del chunk.
    /// </summary>
    [VectorStoreData]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Indice del chunk nel documento originale.
    /// </summary>
    [VectorStoreData]
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Vettore embedding con indice HNSW.
    /// </summary>
    /// <remarks>
    /// pgvector supporta HNSW (Hierarchical Navigable Small World):
    /// - Ricerca approssimata molto veloce
    /// - Buon compromesso tra velocità e accuratezza
    /// - Consigliato per la maggior parte dei casi d'uso
    ///
    /// Alternativa: IndexKind.IvfFlat per dataset molto grandi
    /// dove la memoria è un vincolo.
    /// </remarks>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Embedding { get; set; }

    /// <summary>
    /// Genera il testo da usare per creare l'embedding.
    /// </summary>
    public string GetTextForEmbedding() => $"Title: {Title}\nCategory: {Category}\nContent: {Content}";
}
