// ============================================================================
// 05. CODE REVIEWER - RAG
// FILE: TextSearchDocument.cs
// ============================================================================
// Questo file definisce il modello per i documenti della knowledge base.
//
// CONCETTI CHIAVE:
// - VectorStoreKey: indica il campo chiave unico
// - VectorStoreData: indica campi aggiuntivi da memorizzare
// - VectorStoreVector: indica il campo che genera l'embedding
//
// IMPORTANTE: Il VectorStoreVector è una proprietà string!
// L'embedding viene generato automaticamente dal vector store
// quando ha un EmbeddingGenerator configurato.
//
// Il documento viene "chunkato" (diviso in pezzi) per:
// 1. Rispettare i limiti del modello di embedding
// 2. Avere ricerche più precise (chunk più piccoli = più rilevanti)
// 3. Permettere citazioni accurate delle fonti
// ============================================================================

using Microsoft.Extensions.VectorData;

namespace CodeReviewer.RAG.Rag;

/// <summary>
/// Rappresenta un chunk (pezzo) di documento nella knowledge base.
///
/// ARCHITETTURA RAG:
/// ┌─────────────────┐
/// │ Documento.md    │ ← File originale (es: solid-principles.md)
/// └────────┬────────┘
///          │ Chunking
///          ▼
/// ┌─────────────────┐
/// │ Chunk 1         │ ← Primo pezzo del documento
/// │ Chunk 2         │ ← Secondo pezzo
/// │ ...             │
/// └────────┬────────┘
///          │ Embedding (automatico!)
///          ▼
/// ┌─────────────────┐
/// │ Vector Store    │ ← Ricerca semantica sui chunk
/// └─────────────────┘
///
/// PATTERN IMPORTANTE:
/// La proprietà EmbeddingText è una stringa che viene automaticamente
/// convertita in embedding dal vector store. Non devi gestire
/// manualmente i vettori float[]!
/// </summary>
public sealed class TextSearchDocument
{
    // ========================================================================
    // CHIAVE PRIMARIA
    // ========================================================================

    /// <summary>
    /// Identificatore unico del chunk.
    /// Formato: "{DocumentId}_{ChunkIndex}"
    /// Es: "solid-principles_0", "solid-principles_1", etc.
    ///
    /// NOTA: VectorStoreKey indica che questo è il campo chiave primaria.
    /// </summary>
    [VectorStoreKey]
    public required string ChunkId { get; init; }

    // ========================================================================
    // METADATI DEL DOCUMENTO
    // ========================================================================

    /// <summary>
    /// Identificatore del documento originale (nome del file senza estensione).
    /// Es: "solid-principles", "async-best-practices"
    ///
    /// Utile per:
    /// - Raggruppare chunk dello stesso documento
    /// - Citare la fonte nelle risposte
    /// </summary>
    [VectorStoreData]
    public required string DocumentId { get; init; }

    /// <summary>
    /// Titolo del documento (estratto dal primo heading # del markdown).
    /// Es: "SOLID Principles", "Async/Await Best Practices in C#"
    /// </summary>
    [VectorStoreData]
    public required string Title { get; init; }

    /// <summary>
    /// Percorso del file originale relativo alla knowledge base.
    /// Es: "KnowledgeBase/solid-principles.md"
    ///
    /// Permette di:
    /// - Indicare la fonte esatta al Code Reviewer
    /// - Eventualmente ricaricare il documento
    /// </summary>
    [VectorStoreData]
    public required string FilePath { get; init; }

    /// <summary>
    /// Indice del chunk all'interno del documento (0-based).
    /// Es: 0, 1, 2, ...
    ///
    /// Utile per ricostruire l'ordine originale se necessario.
    /// </summary>
    [VectorStoreData]
    public required int ChunkIndex { get; init; }

    // ========================================================================
    // CONTENUTO DEL CHUNK
    // ========================================================================

    /// <summary>
    /// Testo del chunk.
    /// Questo è il contenuto effettivo che viene iniettato
    /// nel contesto dell'agente quando rilevante.
    ///
    /// DIMENSIONE TIPICA: 500-1000 caratteri per chunk
    /// - Troppo piccolo = perde contesto
    /// - Troppo grande = meno preciso nella ricerca
    /// </summary>
    [VectorStoreData]
    public required string Text { get; init; }

    // ========================================================================
    // EMBEDDING AUTOMATICO
    // ========================================================================

    /// <summary>
    /// Testo usato per generare l'embedding.
    ///
    /// PATTERN IMPORTANTE:
    /// Questa proprietà è marcata con [VectorStoreVector] e restituisce string.
    /// Il vector store (quando ha un EmbeddingGenerator configurato)
    /// converte automaticamente questa stringa in un vettore float[].
    ///
    /// Includiamo titolo + testo per avere embeddings più ricchi
    /// che catturano sia il contesto del documento che il contenuto.
    ///
    /// DIMENSIONI EMBEDDING:
    /// - 1536: text-embedding-3-small (OpenAI)
    /// - 3072: text-embedding-3-large (OpenAI)
    ///
    /// DistanceFunction.CosineSimilarity:
    /// - Misura l'angolo tra vettori (indipendente dalla lunghezza)
    /// - Valori: 1.0 = identici, 0.0 = ortogonali
    ///
    /// IndexKind.Hnsw:
    /// - Hierarchical Navigable Small World
    /// - Algoritmo efficiente per ricerca approssimata
    /// </summary>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public string? EmbeddingText => $"Title: {Title}\nContent: {Text}";

    // ========================================================================
    // UTILITY METHODS
    // ========================================================================

    /// <summary>
    /// Rappresentazione testuale per debug e logging.
    /// </summary>
    public override string ToString()
    {
        // Mostra solo i primi 50 caratteri del testo per brevità
        var preview = Text.Length > 50
            ? Text[..50] + "..."
            : Text;

        return $"[{ChunkId}] {Title}: {preview}";
    }
}
