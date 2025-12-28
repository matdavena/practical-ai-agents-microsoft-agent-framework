// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// FILE: Data/DocumentChunk.cs
// ============================================================================
//
// MODELLO DATI PER I CHUNK DI DOCUMENTO
//
// Questa classe rappresenta un frammento di documento da indicizzare nel
// vector store. Ogni chunk contiene:
// - Un ID univoco (Guid per compatibilità con Qdrant)
// - Il testo del contenuto
// - Metadati (titolo, categoria, ecc.)
// - Il vettore embedding (generato manualmente)
//
// IMPORTANTE: A differenza di InMemoryVectorStore, i connector Qdrant e
// SQL Server richiedono che gli embedding siano generati manualmente
// PRIMA dell'inserimento nel vector store.
//
// ============================================================================

using Microsoft.Extensions.VectorData;

namespace _11.RAG.VectorStores.Data;

/// <summary>
/// Rappresenta un chunk di documento indicizzato nel vector store.
/// </summary>
/// <remarks>
/// La struttura è ottimizzata per scenari RAG:
/// - Title e Category sono metadati per filtering
/// - Content contiene il testo da passare all'LLM come contesto
/// - Embedding contiene il vettore per la ricerca semantica
/// </remarks>
public sealed class DocumentChunk
{
    // ========================================================================
    // CHIAVE PRIMARIA
    // ========================================================================
    // [VectorStoreKey] indica il campo usato come identificatore univoco.
    //
    // NOTA: Qdrant supporta solo chiavi Guid o ulong (non string).
    // Usiamo Guid per compatibilità con entrambi i vector store.

    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.Empty;

    // ========================================================================
    // CAMPI DATI (METADATA)
    // ========================================================================
    // [VectorStoreData] indica campi che contengono dati testuali.
    // Questi campi vengono memorizzati e restituiti nei risultati.

    /// <summary>
    /// Titolo del documento sorgente.
    /// </summary>
    [VectorStoreData]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Categoria del documento (es. "programming", "database", "ai").
    /// </summary>
    [VectorStoreData]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Contenuto testuale del chunk.
    /// </summary>
    /// <remarks>
    /// Questo è il testo che verrà passato all'LLM come contesto.
    /// </remarks>
    [VectorStoreData]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Indice del chunk all'interno del documento originale.
    /// </summary>
    [VectorStoreData]
    public int ChunkIndex { get; set; }

    // ========================================================================
    // VETTORE EMBEDDING
    // ========================================================================
    // [VectorStoreVector] indica il campo che contiene l'embedding.
    //
    // IMPORTANTE: Per Qdrant e SQL Server, l'embedding deve essere un
    // ReadOnlyMemory<float>, NON una stringa. Gli embedding devono essere
    // generati manualmente PRIMA dell'inserimento.
    //
    // DIMENSIONI: 1536 per text-embedding-3-small (OpenAI)
    // DISTANZA: CosineSimilarity per testi
    // INDICE: Hnsw per ricerca veloce

    /// <summary>
    /// Vettore embedding del contenuto.
    /// </summary>
    /// <remarks>
    /// Deve essere popolato manualmente usando un embedding generator
    /// prima di inserire il documento nel vector store.
    /// </remarks>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Embedding { get; set; }

    // ========================================================================
    // HELPER PER EMBEDDING
    // ========================================================================

    /// <summary>
    /// Genera il testo da usare per creare l'embedding.
    /// </summary>
    /// <remarks>
    /// Combina titolo, categoria e contenuto per un embedding più ricco.
    /// Usa questo metodo per generare l'embedding prima dell'inserimento.
    /// </remarks>
    public string GetTextForEmbedding() => $"Title: {Title}\nCategory: {Category}\nContent: {Content}";
}
