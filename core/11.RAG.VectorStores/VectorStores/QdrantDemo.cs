// ============================================================================
// 11. RAG WITH REAL VECTOR STORES
// FILE: VectorStores/QdrantDemo.cs
// ============================================================================
//
// RAG DEMO WITH QDRANT
//
// Qdrant is an open-source vector database optimized for:
// - High-performance semantic search
// - Support for billions of vectors
// - Advanced filtering during search
// - Native REST and gRPC APIs
//
// PREREQUISITES:
// 1. Docker Desktop running
// 2. Qdrant container started: docker compose up -d qdrant
//
// ENDPOINTS:
// - REST API: http://localhost:6333
// - Dashboard: http://localhost:6333/dashboard
// - gRPC: localhost:6334
//
// ============================================================================

using _11.RAG.VectorStores.Data;
using Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using Qdrant.Client;

namespace _11.RAG.VectorStores.VectorStores;

/// <summary>
/// Demonstrates using Qdrant as a vector store for RAG.
/// </summary>
public static class QdrantDemo
{
    // Configuration
    // NOTE: Port 6334 for gRPC (the C# client uses gRPC)
    // Port 6333 is for REST API and web Dashboard
    private const string QdrantHost = "localhost";
    private const int QdrantGrpcPort = 6334;
    private const string CollectionName = "learning-documents";
    private const string EmbeddingModel = "text-embedding-3-small";
    private const string ChatModel = "gpt-4o-mini";

    public static async Task RunAsync()
    {
        ConsoleHelper.WriteTitle("RAG with Qdrant Vector Store");

        // ====================================================================
        // STEP 1: VERIFY CONNECTION TO QDRANT
        // ====================================================================
        ConsoleHelper.WriteSeparator("1. Connection to Qdrant");

        Console.WriteLine($"Connecting to Qdrant via gRPC: {QdrantHost}:{QdrantGrpcPort}");
        Console.WriteLine();

        // QdrantClient is the official client for communicating with Qdrant
        // Uses gRPC on port 6334 for better performance
        var qdrantClient = new QdrantClient(QdrantHost, QdrantGrpcPort);

        try
        {
            // Verify that Qdrant is reachable
            var collections = await qdrantClient.ListCollectionsAsync();
            Console.WriteLine($"Qdrant reachable! Existing collections: {collections.Count}");

            foreach (var col in collections)
            {
                Console.WriteLine($"   - {col}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Unable to connect to Qdrant!");
            Console.WriteLine($"Detail: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure that:");
            Console.WriteLine("1. Docker Desktop is running");
            Console.WriteLine("2. The Qdrant container is started:");
            Console.WriteLine("   docker compose up -d qdrant");
            Console.WriteLine();
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine();

        // ====================================================================
        // STEP 2: SETUP OPENAI EMBEDDING GENERATOR
        // ====================================================================
        ConsoleHelper.WriteSeparator("2. Setup Embedding Generator");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);

        // IEmbeddingGenerator<string, Embedding<float>> is the standard interface
        // from Microsoft.Extensions.AI for generating embeddings
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

        // QdrantVectorStore implements IVectorStore from Microsoft.Extensions.VectorData
        // The second parameter (ownsClient) indicates whether the vector store should
        // manage the Qdrant client lifecycle
        var vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: false);

        // GetCollection<TKey, TRecord> gets a reference to the collection
        // NOTE: Qdrant requires Guid or ulong keys, not string
        // We use DocumentChunkQdrant which has HNSW index
        var collection = vectorStore.GetCollection<Guid, DocumentChunkQdrant>(CollectionName);

        // EnsureCollectionExistsAsync creates the collection if it doesn't exist
        // The structure is inferred from the [VectorStore*] attributes of the class
        await collection.EnsureCollectionExistsAsync();

        Console.WriteLine($"Collection '{CollectionName}' ready!");
        Console.WriteLine();

        // ====================================================================
        // STEP 4: INDEX DOCUMENTS
        // ====================================================================
        ConsoleHelper.WriteSeparator("4. Indexing Documents");

        // Get all chunks from sample documents (Qdrant version)
        var chunks = SampleDocuments.GetChunksForQdrant().ToList();
        Console.WriteLine($"Documents to index: {SampleDocuments.Documents.Length}");
        Console.WriteLine($"Total chunks: {chunks.Count}");
        Console.WriteLine();

        // IMPORTANT: For Qdrant (and SQL Server) we must generate embeddings
        // manually BEFORE insertion. InMemoryVectorStore does this automatically,
        // but connectors for real databases require pre-calculated embeddings.

        Console.WriteLine("Generating embeddings...");

        // Generate embeddings for all chunks in batch (more efficient)
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
            // UpsertAsync inserts or updates if the ID already exists
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

        // Test queries
        var testQueries = new[]
        {
            "How does LINQ work in C#?",
            "What are ACID transactions?",
            "What is RAG and how does it work?"
        };

        foreach (var query in testQueries)
        {
            Console.WriteLine($"Query: \"{query}\"");
            Console.WriteLine();

            // Generate the query embedding manually
            var queryEmbedding = await embeddingGenerator.GenerateAsync(query);

            // Create search options
            var searchOptions = new VectorSearchOptions<DocumentChunkQdrant>
            {
                IncludeVectors = false
            };

            // Search for most similar chunks using the vector
            // SearchAsync(vector, topK, options) - topK is a separate parameter
            var searchResults = collection.SearchAsync(queryEmbedding.Vector, 3, searchOptions);

            Console.WriteLine("Results:");
            await foreach (var result in searchResults)
            {
                // Score indicates similarity (higher = more similar for cosine)
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
        Console.WriteLine("Relevant chunks will be retrieved and passed to the LLM.");
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

            // 1. Generate question embedding
            var questionEmbedding = await embeddingGenerator.GenerateAsync(question);

            // 2. Retrieve relevant chunks
            var searchOptions = new VectorSearchOptions<DocumentChunkQdrant>
            {
                IncludeVectors = false
            };

            var relevantChunks = collection.SearchAsync(questionEmbedding.Vector, 3, searchOptions);

            var context = new List<string>();
            Console.WriteLine("Retrieved chunks:");
            await foreach (var result in relevantChunks)
            {
                context.Add(result.Record.Content);
                Console.WriteLine($"   - {result.Record.Title} (score: {result.Score:F4})");
            }
            Console.WriteLine();

            // 3. Build prompt with RAG context
            var ragPrompt = $"""
                Use ONLY the following information to answer the question.
                If the information is not sufficient, say so clearly.

                CONTEXT:
                {string.Join("\n\n---\n\n", context)}

                QUESTION: {question}

                ANSWER:
                """;

            // 4. Generate answer with LLM
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

        Console.Write("Do you want to delete the collection? (y/n): ");
        var delete = Console.ReadLine();

        if (delete?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
        {
            await collection.EnsureCollectionDeletedAsync();
            Console.WriteLine($"Collection '{CollectionName}' deleted!");
        }
        else
        {
            Console.WriteLine("Collection kept for future use.");
            Console.WriteLine($"Dashboard: http://localhost:6333/dashboard");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey();
    }

    private static string Truncate(string text, int maxLength)
    {
        // Remove newlines and extra spaces
        var clean = text.Replace("\n", " ").Replace("\r", "").Trim();
        while (clean.Contains("  "))
            clean = clean.Replace("  ", " ");

        return clean.Length <= maxLength
            ? clean
            : clean[..(maxLength - 3)] + "...";
    }
}
