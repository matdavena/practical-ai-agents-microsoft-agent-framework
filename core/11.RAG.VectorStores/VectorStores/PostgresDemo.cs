// ============================================================================
// 11. RAG WITH REAL VECTOR STORES
// FILE: VectorStores/PostgresDemo.cs
// ============================================================================
//
// RAG DEMO WITH POSTGRESQL + PGVECTOR
//
// PostgreSQL with the pgvector extension is the most popular open source
// solution for adding vector search capabilities to a relational
// database.
//
// WHY PGVECTOR IS SO POPULAR:
// - Open source and free (no license)
// - PostgreSQL is already widely used and known
// - Mature extension with active community
// - Supported by AWS RDS, Azure, Google Cloud, Supabase, Neon, etc.
// - Allows combining traditional SQL queries with vector search
//
// SUPPORTED INDEX TYPES:
// - HNSW: Hierarchical Navigable Small World (fast, approximate)
// - IVFFlat: Inverted File with Flat (good for very large datasets)
//
// NUGET PACKAGE:
// - Microsoft.SemanticKernel.Connectors.PgVector
// - NOTE: Renamed from "Connectors.Postgres" in May 2025
//
// PREREQUISITES:
// 1. Docker Desktop running
// 2. PostgreSQL container started: docker compose up -d postgres
//
// CONNECTION:
// - Host: localhost
// - Port: 5433 (to avoid conflicts with local PostgreSQL)
// - Database: vectorstore
// - User: postgres
// - Password: VectorStore123!
//
// ============================================================================

using _11.RAG.VectorStores.Data;
using Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.PgVector;
using Npgsql;
using OpenAI;

namespace _11.RAG.VectorStores.VectorStores;

/// <summary>
/// Demonstrates using PostgreSQL + pgvector as a vector store for RAG.
/// </summary>
public static class PostgresDemo
{
    // Configuration
    // NOTE: Port 5433 to avoid conflicts with local PostgreSQL (5432)
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=vectorstore;Username=postgres;Password=VectorStore123!";

    private const string CollectionName = "document_chunks";
    private const string EmbeddingModel = "text-embedding-3-small";
    private const string ChatModel = "gpt-4o-mini";

    public static async Task RunAsync()
    {
        ConsoleHelper.WriteTitle("RAG with PostgreSQL + pgvector");

        // ====================================================================
        // STEP 1: VERIFY CONNECTION TO POSTGRESQL
        // ====================================================================
        ConsoleHelper.WriteSeparator("1. Connection to PostgreSQL");

        Console.WriteLine("Connecting to PostgreSQL (Docker, port 5433)...");
        Console.WriteLine();

        NpgsqlDataSource dataSource;

        try
        {
            // Create an NpgsqlDataSource to manage connections
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);

            // IMPORTANT: Register the vector type for pgvector
            // This allows Npgsql to properly handle vectors
            dataSourceBuilder.UseVector();

            dataSource = dataSourceBuilder.Build();

            // Test connection
            await using var testConn = await dataSource.OpenConnectionAsync();

            // Verify PostgreSQL version
            await using var versionCmd = testConn.CreateCommand();
            versionCmd.CommandText = "SELECT version()";
            var version = await versionCmd.ExecuteScalarAsync();

            Console.WriteLine("PostgreSQL reachable!");
            Console.WriteLine($"Version: {TruncateVersion(version?.ToString() ?? "")}");
            Console.WriteLine();

            // Verify that pgvector is installed
            await using var extensionCmd = testConn.CreateCommand();
            extensionCmd.CommandText = "SELECT extversion FROM pg_extension WHERE extname = 'vector'";
            var pgvectorVersion = await extensionCmd.ExecuteScalarAsync();

            if (pgvectorVersion != null)
            {
                Console.WriteLine($"pgvector installed! Version: {pgvectorVersion}");
            }
            else
            {
                // Try to create the extension
                Console.WriteLine("pgvector not found, attempting to install...");
                await using var createExtCmd = testConn.CreateCommand();
                createExtCmd.CommandText = "CREATE EXTENSION IF NOT EXISTS vector";
                await createExtCmd.ExecuteNonQueryAsync();
                Console.WriteLine("pgvector extension created successfully!");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Unable to connect to PostgreSQL!");
            Console.WriteLine($"Detail: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure that:");
            Console.WriteLine("1. Docker Desktop is running");
            Console.WriteLine("2. The PostgreSQL container is started:");
            Console.WriteLine("   docker compose up -d postgres");
            Console.WriteLine("3. Wait a few seconds for PostgreSQL to be ready");
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

        // PostgresVectorStore implements IVectorStore
        // Uses PostgreSQL tables with VECTOR columns to store embeddings
        // Note: the new API (May 2025) accepts the connection string directly
        var vectorStore = new PostgresVectorStore(ConnectionString);

        // Get the collection (will be a table in PostgreSQL)
        var collection = vectorStore.GetCollection<Guid, DocumentChunkPostgres>(CollectionName);

        // Create the table if it doesn't exist
        // The structure is inferred from the [VectorStore*] attributes of the class
        await collection.EnsureCollectionExistsAsync();

        Console.WriteLine($"Collection '{CollectionName}' ready!");
        Console.WriteLine("(Corresponds to a PostgreSQL table with VECTOR column)");
        Console.WriteLine();

        // Show the table structure
        await ShowTableStructureAsync(dataSource);

        // ====================================================================
        // STEP 4: INDEX DOCUMENTS
        // ====================================================================
        ConsoleHelper.WriteSeparator("4. Indexing Documents");

        var chunks = SampleDocuments.GetChunksForPostgres().ToList();
        Console.WriteLine($"Documents to index: {SampleDocuments.Documents.Length}");
        Console.WriteLine($"Total chunks: {chunks.Count}");
        Console.WriteLine();

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

        // ====================================================================
        // STEP 5: SEMANTIC SEARCH
        // ====================================================================
        ConsoleHelper.WriteSeparator("5. Semantic Search Test");

        var testQueries = new[]
        {
            "How does LINQ work?",
            "What are indexes in a database?",
            "How do you implement RAG?"
        };

        foreach (var query in testQueries)
        {
            Console.WriteLine($"Query: \"{query}\"");
            Console.WriteLine();

            var queryEmbedding = await embeddingGenerator.GenerateAsync(query);

            var searchOptions = new VectorSearchOptions<DocumentChunkPostgres>
            {
                IncludeVectors = false
            };

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
        Console.WriteLine("Relevant chunks will be retrieved from PostgreSQL.");
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

            var searchOptions = new VectorSearchOptions<DocumentChunkPostgres>
            {
                IncludeVectors = false
            };

            var relevantChunks = collection.SearchAsync(questionEmbedding.Vector, 3, searchOptions);

            var context = new List<string>();
            Console.WriteLine("Chunks retrieved from PostgreSQL:");
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
            Console.WriteLine("You can examine it with pgAdmin or psql.");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey();
    }

    /// <summary>
    /// Shows the structure of the created table.
    /// </summary>
    private static async Task ShowTableStructureAsync(NpgsqlDataSource dataSource)
    {
        Console.WriteLine("PostgreSQL table structure:");
        Console.WriteLine();

        try
        {
            await using var conn = await dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT column_name, data_type, character_maximum_length
                FROM information_schema.columns
                WHERE table_name = '{CollectionName}'
                ORDER BY ordinal_position
                """;

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var colName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLen = reader.IsDBNull(2) ? "" : $"({reader.GetInt32(2)})";

                // Highlight the VECTOR column
                if (dataType == "USER-DEFINED")
                {
                    Console.WriteLine($"   - {colName}: vector(1536)  <-- embedding!");
                }
                else
                {
                    Console.WriteLine($"   - {colName}: {dataType}{maxLen}");
                }
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
        var firstLine = version.Split('\n')[0].Trim();
        return firstLine.Length > 70 ? firstLine[..70] + "..." : firstLine;
    }
}
