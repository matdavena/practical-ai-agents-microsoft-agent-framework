// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// FILE: Data/SampleDocuments.cs
// ============================================================================
//
// DOCUMENTI DI ESEMPIO PER TESTING
//
// Questa classe fornisce documenti di esempio su vari argomenti tech
// per testare le funzionalità RAG con i diversi vector store.
//
// I documenti sono organizzati per categoria:
// - programming: concetti di programmazione C#
// - database: concetti su database e SQL
// - ai: concetti su AI e machine learning
//
// ============================================================================

namespace _11.RAG.VectorStores.Data;

/// <summary>
/// Fornisce documenti di esempio per popolare i vector store.
/// </summary>
public static class SampleDocuments
{
    // ========================================================================
    // DOCUMENTI DI PROGRAMMAZIONE C#
    // ========================================================================

    public static readonly (string Title, string Category, string[] Chunks)[] Documents =
    [
        // --------------------------------------------------------------------
        // Documento 1: LINQ in C#
        // --------------------------------------------------------------------
        (
            Title: "LINQ in C#",
            Category: "programming",
            Chunks: [
                """
                LINQ (Language Integrated Query) è una potente funzionalità di C# che
                permette di scrivere query direttamente nel codice. LINQ fornisce una
                sintassi uniforme per interrogare diverse fonti di dati come collezioni
                in memoria, database SQL, documenti XML e altro ancora.
                """,

                """
                La sintassi LINQ può essere espressa in due modi: query syntax e method
                syntax. La query syntax assomiglia a SQL con le clausole from, where,
                select. La method syntax usa metodi di estensione come Where(), Select(),
                OrderBy() concatenati con lambda expressions.
                """,

                """
                Le operazioni LINQ sono lazy (pigre): la query non viene eseguita finché
                non si itera sui risultati o si chiama un metodo terminale come ToList(),
                Count(), First(). Questo permette di costruire query complesse in modo
                efficiente, eseguendo il lavoro solo quando necessario.
                """,

                """
                LINQ to Objects opera su collezioni IEnumerable<T> in memoria.
                LINQ to Entities traduce le query in SQL per Entity Framework.
                LINQ to XML permette di navigare e trasformare documenti XML.
                Ogni provider LINQ implementa l'interfaccia IQueryable<T>.
                """
            ]
        ),

        // --------------------------------------------------------------------
        // Documento 2: Async/Await in C#
        // --------------------------------------------------------------------
        (
            Title: "Programmazione Asincrona in C#",
            Category: "programming",
            Chunks: [
                """
                La programmazione asincrona in C# si basa sulle keyword async e await.
                Un metodo async restituisce Task o Task<T> e può contenere espressioni
                await. Quando si incontra un await, il controllo ritorna al chiamante
                finché l'operazione asincrona non completa.
                """,

                """
                Task rappresenta un'operazione asincrona che può essere attesa con await.
                Task<T> rappresenta un'operazione che restituirà un valore di tipo T.
                ValueTask<T> è un'alternativa più efficiente quando l'operazione spesso
                completa in modo sincrono, evitando allocazioni heap.
                """,

                """
                ConfigureAwait(false) indica che la continuazione non richiede il contesto
                di sincronizzazione originale. È consigliato nelle librerie per evitare
                deadlock e migliorare le performance. Nelle applicazioni UI invece si
                vuole tornare sul thread UI, quindi non si usa ConfigureAwait(false).
                """,

                """
                CancellationToken permette di cancellare operazioni asincrone in corso.
                Si passa il token ai metodi async che lo supportano e si può controllare
                IsCancellationRequested o chiamare ThrowIfCancellationRequested().
                È buona pratica supportare sempre la cancellazione nelle API async.
                """
            ]
        ),

        // --------------------------------------------------------------------
        // Documento 3: SQL Server Fundamentals
        // --------------------------------------------------------------------
        (
            Title: "Fondamenti di SQL Server",
            Category: "database",
            Chunks: [
                """
                SQL Server è un RDBMS (Relational Database Management System) sviluppato
                da Microsoft. Supporta il linguaggio T-SQL (Transact-SQL), un'estensione
                di SQL standard con funzionalità procedurali come variabili, condizioni,
                cicli e gestione errori con TRY-CATCH.
                """,

                """
                Gli indici in SQL Server migliorano le performance delle query. Un indice
                clustered ordina fisicamente i dati della tabella (uno solo per tabella).
                Gli indici non-clustered sono strutture separate che puntano ai dati.
                L'indice covering include tutte le colonne necessarie alla query.
                """,

                """
                Le transazioni in SQL Server garantiscono le proprietà ACID: Atomicità
                (tutto o niente), Consistenza (da stato valido a stato valido),
                Isolamento (transazioni concorrenti non interferiscono), Durabilità
                (i cambiamenti persistono anche dopo crash del sistema).
                """,

                """
                SQL Server 2022 introduce il supporto nativo per i vettori (vector data type)
                e funzioni di distanza come VECTOR_DISTANCE per calcolare similarità coseno,
                euclidea o dot product. Questo abilita scenari di ricerca semantica e RAG
                direttamente nel database relazionale.
                """
            ]
        ),

        // --------------------------------------------------------------------
        // Documento 4: Machine Learning Basics
        // --------------------------------------------------------------------
        (
            Title: "Introduzione al Machine Learning",
            Category: "ai",
            Chunks: [
                """
                Il Machine Learning (ML) è un sottoinsieme dell'Intelligenza Artificiale
                che permette ai sistemi di apprendere dai dati senza essere esplicitamente
                programmati. Esistono tre paradigmi principali: supervised learning,
                unsupervised learning e reinforcement learning.
                """,

                """
                Nel supervised learning, il modello apprende da esempi etichettati. Per
                problemi di classificazione (predire categorie) si usano algoritmi come
                logistic regression, decision trees, random forest, SVM. Per regressione
                (predire valori continui) si usa linear regression, gradient boosting.
                """,

                """
                Nel unsupervised learning, il modello trova pattern in dati non etichettati.
                Il clustering raggruppa dati simili (K-means, DBSCAN, hierarchical).
                La riduzione dimensionale comprime i dati preservando l'informazione
                (PCA, t-SNE, UMAP). Utile per visualizzazione ed esplorazione dati.
                """,

                """
                Gli embedding sono rappresentazioni vettoriali dense di dati (testi, immagini).
                Word2Vec e GloVe creano embedding per parole. I modelli transformer come
                BERT e GPT generano embedding contestuali per frasi intere. Gli embedding
                sono fondamentali per la ricerca semantica e i sistemi RAG.
                """
            ]
        ),

        // --------------------------------------------------------------------
        // Documento 5: RAG Architecture
        // --------------------------------------------------------------------
        (
            Title: "Architettura RAG",
            Category: "ai",
            Chunks: [
                """
                RAG (Retrieval-Augmented Generation) è un pattern architetturale che
                combina la potenza dei Large Language Models con una base di conoscenza
                esterna. Invece di fare affidamento solo sulla conoscenza del modello,
                RAG recupera informazioni rilevanti e le passa come contesto.
                """,

                """
                Il flusso RAG tipico è: 1) L'utente pone una domanda. 2) La domanda viene
                convertita in un vettore embedding. 3) Si cerca nel vector store i documenti
                più simili. 4) I documenti recuperati vengono passati all'LLM insieme alla
                domanda. 5) L'LLM genera una risposta basata sul contesto fornito.
                """,

                """
                Il chunking è cruciale per RAG: i documenti vanno divisi in frammenti di
                dimensione appropriata (tipicamente 200-500 token). Chunk troppo piccoli
                perdono contesto, troppo grandi diluiscono la rilevanza. Si può usare
                overlap tra chunk per preservare continuità semantica.
                """,

                """
                I vector store (Qdrant, Pinecone, Weaviate, Chroma, SQL Server) memorizzano
                gli embedding e supportano ricerca per similarità. La scelta dipende da:
                scala (milioni/miliardi di vettori), performance, costi, funzionalità di
                filtering, integrazione con l'ecosistema esistente.
                """
            ]
        )
    ];

    // ========================================================================
    // HELPER PER GENERARE DOCUMENTCHUNK
    // ========================================================================

    /// <summary>
    /// Genera chunk per Qdrant (con indice HNSW).
    /// </summary>
    public static IEnumerable<DocumentChunkQdrant> GetChunksForQdrant()
    {
        foreach (var doc in Documents)
        {
            for (int i = 0; i < doc.Chunks.Length; i++)
            {
                yield return new DocumentChunkQdrant
                {
                    Id = GenerateDeterministicGuid(doc.Title, i),
                    Title = doc.Title,
                    Category = doc.Category,
                    Content = doc.Chunks[i].Trim(),
                    ChunkIndex = i
                };
            }
        }
    }

    /// <summary>
    /// Genera chunk per SQL Server (con indice Flat).
    /// </summary>
    public static IEnumerable<DocumentChunkSqlServer> GetChunksForSqlServer()
    {
        foreach (var doc in Documents)
        {
            for (int i = 0; i < doc.Chunks.Length; i++)
            {
                yield return new DocumentChunkSqlServer
                {
                    Id = GenerateDeterministicGuid(doc.Title, i),
                    Title = doc.Title,
                    Category = doc.Category,
                    Content = doc.Chunks[i].Trim(),
                    ChunkIndex = i
                };
            }
        }
    }

    /// <summary>
    /// Genera chunk per PostgreSQL + pgvector (con indice HNSW).
    /// </summary>
    public static IEnumerable<DocumentChunkPostgres> GetChunksForPostgres()
    {
        foreach (var doc in Documents)
        {
            for (int i = 0; i < doc.Chunks.Length; i++)
            {
                yield return new DocumentChunkPostgres
                {
                    Id = GenerateDeterministicGuid(doc.Title, i),
                    Title = doc.Title,
                    Category = doc.Category,
                    Content = doc.Chunks[i].Trim(),
                    ChunkIndex = i
                };
            }
        }
    }

    /// <summary>
    /// Genera un Guid deterministico basato su una stringa e un indice.
    /// </summary>
    /// <remarks>
    /// Questo permette di avere sempre lo stesso ID per lo stesso chunk,
    /// utile per operazioni di upsert (insert or update).
    /// </remarks>
    private static Guid GenerateDeterministicGuid(string text, int index)
    {
        // Usa un hash MD5 per generare un Guid deterministico
        // MD5 produce 16 byte, esattamente la dimensione di un Guid
        var input = $"{text}-chunk-{index}";
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);
        return new Guid(hashBytes);
    }
}
