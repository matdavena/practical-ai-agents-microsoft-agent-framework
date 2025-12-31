// ============================================================================
// 11. RAG WITH REAL VECTOR STORES
// FILE: Data/SampleDocuments.cs
// ============================================================================
//
// SAMPLE DOCUMENTS FOR TESTING
//
// This class provides sample documents on various tech topics
// to test RAG functionality with different vector stores.
//
// Documents are organized by category:
// - programming: C# programming concepts
// - database: database and SQL concepts
// - ai: AI and machine learning concepts
//
// ============================================================================

namespace _11.RAG.VectorStores.Data;

/// <summary>
/// Provides sample documents to populate vector stores.
/// </summary>
public static class SampleDocuments
{
    // ========================================================================
    // C# PROGRAMMING DOCUMENTS
    // ========================================================================

    public static readonly (string Title, string Category, string[] Chunks)[] Documents =
    [
        // --------------------------------------------------------------------
        // Document 1: LINQ in C#
        // --------------------------------------------------------------------
        (
            Title: "LINQ in C#",
            Category: "programming",
            Chunks: [
                """
                LINQ (Language Integrated Query) is a powerful C# feature that
                allows you to write queries directly in code. LINQ provides a
                uniform syntax for querying different data sources such as in-memory
                collections, SQL databases, XML documents and more.
                """,

                """
                LINQ syntax can be expressed in two ways: query syntax and method
                syntax. Query syntax resembles SQL with from, where, select clauses.
                Method syntax uses extension methods like Where(), Select(),
                OrderBy() chained with lambda expressions.
                """,

                """
                LINQ operations are lazy: the query is not executed until
                you iterate over the results or call a terminal method like ToList(),
                Count(), First(). This allows you to build complex queries efficiently,
                doing the work only when necessary.
                """,

                """
                LINQ to Objects operates on in-memory IEnumerable<T> collections.
                LINQ to Entities translates queries to SQL for Entity Framework.
                LINQ to XML allows you to navigate and transform XML documents.
                Each LINQ provider implements the IQueryable<T> interface.
                """
            ]
        ),

        // --------------------------------------------------------------------
        // Document 2: Async/Await in C#
        // --------------------------------------------------------------------
        (
            Title: "Asynchronous Programming in C#",
            Category: "programming",
            Chunks: [
                """
                Asynchronous programming in C# is based on the async and await keywords.
                An async method returns Task or Task<T> and can contain await
                expressions. When await is encountered, control returns to the caller
                until the asynchronous operation completes.
                """,

                """
                Task represents an asynchronous operation that can be awaited.
                Task<T> represents an operation that will return a value of type T.
                ValueTask<T> is a more efficient alternative when the operation often
                completes synchronously, avoiding heap allocations.
                """,

                """
                ConfigureAwait(false) indicates that the continuation doesn't require the
                original synchronization context. It's recommended in libraries to avoid
                deadlocks and improve performance. In UI applications instead you
                want to return to the UI thread, so you don't use ConfigureAwait(false).
                """,

                """
                CancellationToken allows you to cancel asynchronous operations in progress.
                You pass the token to async methods that support it and can check
                IsCancellationRequested or call ThrowIfCancellationRequested().
                It's good practice to always support cancellation in async APIs.
                """
            ]
        ),

        // --------------------------------------------------------------------
        // Document 3: SQL Server Fundamentals
        // --------------------------------------------------------------------
        (
            Title: "SQL Server Fundamentals",
            Category: "database",
            Chunks: [
                """
                SQL Server is an RDBMS (Relational Database Management System) developed
                by Microsoft. It supports the T-SQL (Transact-SQL) language, an extension
                of standard SQL with procedural features like variables, conditions,
                loops and error handling with TRY-CATCH.
                """,

                """
                Indexes in SQL Server improve query performance. A clustered
                index physically orders the table data (only one per table).
                Non-clustered indexes are separate structures that point to the data.
                A covering index includes all columns needed by the query.
                """,

                """
                Transactions in SQL Server guarantee ACID properties: Atomicity
                (all or nothing), Consistency (from valid state to valid state),
                Isolation (concurrent transactions don't interfere), Durability
                (changes persist even after system crash).
                """,

                """
                SQL Server 2022 introduces native support for vectors (vector data type)
                and distance functions like VECTOR_DISTANCE to calculate cosine,
                euclidean or dot product similarity. This enables semantic search and RAG
                scenarios directly in the relational database.
                """
            ]
        ),

        // --------------------------------------------------------------------
        // Document 4: Machine Learning Basics
        // --------------------------------------------------------------------
        (
            Title: "Introduction to Machine Learning",
            Category: "ai",
            Chunks: [
                """
                Machine Learning (ML) is a subset of Artificial Intelligence
                that allows systems to learn from data without being explicitly
                programmed. There are three main paradigms: supervised learning,
                unsupervised learning and reinforcement learning.
                """,

                """
                In supervised learning, the model learns from labeled examples. For
                classification problems (predicting categories) algorithms like
                logistic regression, decision trees, random forest, SVM are used. For regression
                (predicting continuous values) linear regression, gradient boosting are used.
                """,

                """
                In unsupervised learning, the model finds patterns in unlabeled data.
                Clustering groups similar data (K-means, DBSCAN, hierarchical).
                Dimensionality reduction compresses data while preserving information
                (PCA, t-SNE, UMAP). Useful for visualization and data exploration.
                """,

                """
                Embeddings are dense vector representations of data (text, images).
                Word2Vec and GloVe create embeddings for words. Transformer models like
                BERT and GPT generate contextual embeddings for entire sentences. Embeddings
                are fundamental for semantic search and RAG systems.
                """
            ]
        ),

        // --------------------------------------------------------------------
        // Document 5: RAG Architecture
        // --------------------------------------------------------------------
        (
            Title: "RAG Architecture",
            Category: "ai",
            Chunks: [
                """
                RAG (Retrieval-Augmented Generation) is an architectural pattern that
                combines the power of Large Language Models with an external
                knowledge base. Instead of relying only on the model's knowledge,
                RAG retrieves relevant information and passes it as context.
                """,

                """
                The typical RAG flow is: 1) User asks a question. 2) The question is
                converted to an embedding vector. 3) Search the vector store for most
                similar documents. 4) Retrieved documents are passed to the LLM along with
                the question. 5) LLM generates an answer based on the provided context.
                """,

                """
                Chunking is crucial for RAG: documents must be divided into fragments of
                appropriate size (typically 200-500 tokens). Chunks too small
                lose context, too large dilute relevance. You can use
                overlap between chunks to preserve semantic continuity.
                """,

                """
                Vector stores (Qdrant, Pinecone, Weaviate, Chroma, SQL Server) store
                embeddings and support similarity search. The choice depends on:
                scale (millions/billions of vectors), performance, costs, filtering
                features, integration with existing ecosystem.
                """
            ]
        )
    ];

    // ========================================================================
    // HELPER TO GENERATE DOCUMENTCHUNK
    // ========================================================================

    /// <summary>
    /// Generates chunks for Qdrant (with HNSW index).
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
    /// Generates chunks for SQL Server (with Flat index).
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
    /// Generates chunks for PostgreSQL + pgvector (with HNSW index).
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
    /// Generates a deterministic Guid based on a string and an index.
    /// </summary>
    /// <remarks>
    /// This allows having the same ID for the same chunk,
    /// useful for upsert operations (insert or update).
    /// </remarks>
    private static Guid GenerateDeterministicGuid(string text, int index)
    {
        // Use an MD5 hash to generate a deterministic Guid
        // MD5 produces 16 bytes, exactly the size of a Guid
        var input = $"{text}-chunk-{index}";
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);
        return new Guid(hashBytes);
    }
}
