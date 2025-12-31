// ============================================================================
// 11. RAG WITH REAL VECTOR STORES
// FILE: VectorStores/SqlServerDemo.cs
// ============================================================================
//
// RAG DEMO WITH SQL SERVER
//
// SQL Server 2022 introduces native support for vectors:
// - VECTOR data type for storing embeddings
// - VECTOR_DISTANCE function for calculating similarity
// - Support for cosine, euclidean and dot product
//
// ADVANTAGES of using SQL Server as vector store:
// - No additional database to manage
// - ACID transactions on data + vectors
// - Join with existing relational tables
// - Familiarity for those already using SQL Server
//
// PREREQUISITES:
// 1. Docker Desktop running
// 2. SQL Server container started: docker compose up -d sqlserver
//
// CONNECTION:
// - Server: localhost,1434 (port 1434 to avoid conflicts)
// - User: sa
// - Password: VectorStore123!
//
// ============================================================================

using _11.RAG.VectorStores.Data;
using Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using OpenAI;

namespace _11.RAG.VectorStores.VectorStores;

/// <summary>
/// Demonstrates using SQL Server as a vector store for RAG.
/// </summary>
public static class SqlServerDemo
{
    // Configuration
    // NOTE: Port 1434 to avoid conflicts with local SQL Server (1433)
    private const string MasterConnectionString =
        "Server=localhost,1434;Database=master;User Id=sa;Password=VectorStore123!;TrustServerCertificate=True;";

    private const string ConnectionString =
        "Server=localhost,1434;Database=VectorStoreDemo;User Id=sa;Password=VectorStore123!;TrustServerCertificate=True;";

    private const string DatabaseName = "VectorStoreDemo";

    private const string CollectionName = "DocumentChunks";
    private const string EmbeddingModel = "text-embedding-3-small";
    private const string ChatModel = "gpt-4o-mini";

    public static async Task RunAsync()
    {
        ConsoleHelper.WriteTitle("RAG with SQL Server Vector Store");

        // ====================================================================
        // IMPORTANT NOTICE
        // ====================================================================
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  WARNING: SQL Server VECTOR requires SQL Server 2025!       ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  The native VECTOR type is available ONLY in SQL Server 2025 ║");
        Console.WriteLine("║  which is currently in private preview (not publicly         ║");
        Console.WriteLine("║  available on Docker Hub).                                   ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║  SQL Server 2022 does NOT support the VECTOR type.          ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║  To test vector store with relational databases,            ║");
        Console.WriteLine("║  consider PostgreSQL with pgvector as an alternative.       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.Write("Do you want to continue anyway? (y/n): ");
        var continueAnyway = Console.ReadLine();
        if (!continueAnyway?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }
        Console.WriteLine();

        // ====================================================================
        // STEP 1: VERIFY CONNECTION TO SQL SERVER
        // ====================================================================
        ConsoleHelper.WriteSeparator("1. Connection to SQL Server");

        Console.WriteLine("Connecting to SQL Server (Docker, port 1434)...");
        Console.WriteLine();

        try
        {
            // First connect to master to verify SQL Server and create the database
            await using var masterConn = new SqlConnection(MasterConnectionString);
            await masterConn.OpenAsync();

            // Verify version
            await using var versionCmd = masterConn.CreateCommand();
            versionCmd.CommandText = "SELECT @@VERSION";
            var version = await versionCmd.ExecuteScalarAsync();

            Console.WriteLine("SQL Server reachable!");
            Console.WriteLine($"Version: {TruncateVersion(version?.ToString() ?? "")}");
            Console.WriteLine();

            // Verify it's SQL Server 2022 or higher
            var versionStr = version?.ToString() ?? "";
            if (!versionStr.Contains("2022") && !versionStr.Contains("2025"))
            {
                Console.WriteLine("WARNING: Native vector support requires SQL Server 2022+");
                Console.WriteLine("The connector might use an alternative implementation.");
                Console.WriteLine();
            }

            // Create database if it doesn't exist
            Console.WriteLine($"Verifying database '{DatabaseName}'...");
            await using var createDbCmd = masterConn.CreateCommand();
            createDbCmd.CommandText = $"""
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{DatabaseName}')
                BEGIN
                    CREATE DATABASE [{DatabaseName}]
                    PRINT 'Database created'
                END
                ELSE
                BEGIN
                    PRINT 'Database exists'
                END
                """;
            await createDbCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Database '{DatabaseName}' ready!");
            Console.WriteLine();
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"ERROR: Unable to connect to SQL Server!");
            Console.WriteLine($"Detail: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure that:");
            Console.WriteLine("1. Docker Desktop is running");
            Console.WriteLine("2. The SQL Server container is started:");
            Console.WriteLine("   docker compose up -d sqlserver");
            Console.WriteLine("3. Wait 30-60 seconds after startup (SQL Server takes time)");
            Console.WriteLine();
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey();
            return;
        }

        // ====================================================================
        // STEP 2: SETUP OPENAI EMBEDDING GENERATOR
        // ====================================================================
        ConsoleHelper.WriteSeparator("2. Setup Embedding Generator");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);

        var embeddingGenerator = openAiClient
            .GetEmbeddingClient(EmbeddingModel)
            .AsIEmbeddingGenerator();

        Console.WriteLine($"Embedding model: {EmbeddingModel}");
        Console.WriteLine("Embedding generator ready!");
        Console.WriteLine();

        // ====================================================================
        // STEP 3: CREATE VECTOR STORE AND COLLECTION
        // ====================================================================
        ConsoleHelper.WriteSeparator("3. Creating Vector Store");

        // SqlServerVectorStore implements IVectorStore
        // Uses a SQL table to store records with vectors
        var vectorStore = new SqlServerVectorStore(ConnectionString);

        // Get the collection (will be a table in SQL Server)
        // We use DocumentChunkSqlServer which has Flat index (SQL Server doesn't support HNSW)
        var collection = vectorStore.GetCollection<Guid, DocumentChunkSqlServer>(CollectionName);

        // Create the table if it doesn't exist, with appropriate columns:
        // - Id (primary key)
        // - Title, Category, Content (data fields)
        // - ChunkIndex (integer)
        // - EmbeddingText_Embedding (VECTOR type or fallback)
        await collection.EnsureCollectionExistsAsync();

        Console.WriteLine($"Collection '{CollectionName}' ready!");
        Console.WriteLine("(Corresponds to a SQL Server table)");
        Console.WriteLine();

        // ====================================================================
        // STEP 4: INDEX DOCUMENTS
        // ====================================================================
        ConsoleHelper.WriteSeparator("4. Indexing Documents");

        var chunks = SampleDocuments.GetChunksForSqlServer().ToList();
        Console.WriteLine($"Documents to index: {SampleDocuments.Documents.Length}");
        Console.WriteLine($"Total chunks: {chunks.Count}");
        Console.WriteLine();

        // IMPORTANT: For SQL Server (and Qdrant) we must generate embeddings
        // manually BEFORE insertion.

        Console.WriteLine("Generating embeddings...");

        // Generate embeddings for all chunks in batch
        var textsForEmbedding = chunks.Select(c => c.GetTextForEmbedding()).ToList();
        var embeddings = await embeddingGenerator.GenerateAsync(textsForEmbedding);

        // Associate each embedding with its respective chunk
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Embedding = embeddings[i].Vector;
        }

        Console.WriteLine($"   Generated {embeddings.Count} embeddings!");
        Console.WriteLine();

        Console.WriteLine("Inserting into vector store...");

        var count = 0;
        foreach (var chunk in chunks)
        {
            await collection.UpsertAsync(chunk);
            count++;
            Console.Write($"\r   Chunks inserted: {count}/{chunks.Count}");
        }

        Console.WriteLine();
        Console.WriteLine("Indexing completed!");
        Console.WriteLine();

        // Show the structure of the created table
        await ShowTableStructureAsync();

        // ====================================================================
        // STEP 5: SEMANTIC SEARCH
        // ====================================================================
        ConsoleHelper.WriteSeparator("5. Semantic Search Test");

        var testQueries = new[]
        {
            "How does asynchronous programming work?",
            "What are the types of indexes in SQL Server?",
            "How do you implement a RAG system?"
        };

        foreach (var query in testQueries)
        {
            Console.WriteLine($"Query: \"{query}\"");
            Console.WriteLine();

            var queryEmbedding = await embeddingGenerator.GenerateAsync(query);

            var searchOptions = new VectorSearchOptions<DocumentChunkSqlServer>
            {
                IncludeVectors = false
            };

            // SearchAsync executes a query that calculates the distance
            // between the query vector and the vectors in the database
            // SearchAsync(vector, topK, options) - topK is a separate parameter
            var searchResults = collection.SearchAsync(queryEmbedding.Vector, 3, searchOptions);

            Console.WriteLine("Results:");
            await foreach (var result in searchResults)
            {
                Console.WriteLine($"   [{result.Score:F4}] {result.Record.Title} (chunk {result.Record.ChunkIndex})");
                Console.WriteLine($"            {Truncate(result.Record.Content, 80)}");
            }

            Console.WriteLine();
        }

        // ====================================================================
        // STEP 6: COMPLETE RAG WITH LLM
        // ====================================================================
        ConsoleHelper.WriteSeparator("6. Complete RAG with LLM");

        var chatClient = openAiClient.GetChatClient(ChatModel).AsIChatClient();

        Console.WriteLine("You can now ask questions about the indexed documents.");
        Console.WriteLine("Relevant chunks will be retrieved from SQL Server.");
        Console.WriteLine("Type 'exit' to return to menu.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Question: ");
            var question = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(question))
                continue;

            if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            Console.WriteLine();

            var questionEmbedding = await embeddingGenerator.GenerateAsync(question);

            var searchOptions = new VectorSearchOptions<DocumentChunkSqlServer>
            {
                IncludeVectors = false
            };

            var relevantChunks = collection.SearchAsync(questionEmbedding.Vector, 3, searchOptions);

            var context = new List<string>();
            Console.WriteLine("Chunks retrieved from SQL Server:");
            await foreach (var result in relevantChunks)
            {
                context.Add(result.Record.Content);
                Console.WriteLine($"   - {result.Record.Title} (score: {result.Score:F4})");
            }
            Console.WriteLine();

            var ragPrompt = $"""
                Use ONLY the following information to answer the question.
                If the information is not sufficient, say so clearly.

                CONTEXT:
                {string.Join("\n\n---\n\n", context)}

                QUESTION: {question}

                ANSWER:
                """;

            Console.Write("Answer: ");
            await foreach (var chunk in chatClient.GetStreamingResponseAsync(ragPrompt))
            {
                Console.Write(chunk);
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        // ====================================================================
        // STEP 7: CLEANUP (OPTIONAL)
        // ====================================================================
        ConsoleHelper.WriteSeparator("7. Cleanup");

        Console.Write("Do you want to delete the table? (y/n): ");
        var delete = Console.ReadLine();

        if (delete?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
        {
            await collection.EnsureCollectionDeletedAsync();
            Console.WriteLine($"Table '{CollectionName}' deleted!");
        }
        else
        {
            Console.WriteLine("Table kept for future use.");
            Console.WriteLine("You can examine it with SSMS or Azure Data Studio.");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey();
    }

    /// <summary>
    /// Shows the structure of the table created by the vector store.
    /// </summary>
    private static async Task ShowTableStructureAsync()
    {
        Console.WriteLine("SQL Server table structure:");
        Console.WriteLine();

        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = '{CollectionName}'
                ORDER BY ORDINAL_POSITION
                """;

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var colName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLen = reader.IsDBNull(2) ? "" : $"({reader.GetInt32(2)})";

                Console.WriteLine($"   - {colName}: {dataType}{maxLen}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Unable to read structure: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static string Truncate(string text, int maxLength)
    {
        var clean = text.Replace("\n", " ").Replace("\r", "").Trim();
        while (clean.Contains("  "))
            clean = clean.Replace("  ", " ");

        return clean.Length <= maxLength
            ? clean
            : clean[..(maxLength - 3)] + "...";
    }

    private static string TruncateVersion(string version)
    {
        // Take only the first line of the version
        var firstLine = version.Split('\n')[0].Trim();
        return firstLine.Length > 80 ? firstLine[..80] + "..." : firstLine;
    }
}
