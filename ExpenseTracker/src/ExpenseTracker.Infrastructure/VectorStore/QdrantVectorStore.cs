// ============================================================================
// QdrantVectorStore
// ============================================================================
// Implementation of IVectorStore using Qdrant vector database.
// Uses OpenAI embeddings for vector generation.
//
// BOOK CHAPTER NOTE:
// This demonstrates:
// 1. Vector database integration with Qdrant
// 2. OpenAI embeddings generation
// 3. Semantic search implementation
// 4. Graceful degradation when vector store is unavailable
// ============================================================================

using ExpenseTracker.Core.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace ExpenseTracker.Infrastructure.VectorStore;

/// <summary>
/// Qdrant-based vector store implementation for semantic expense search.
/// </summary>
public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _qdrantClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly string _collectionName;
    private readonly int _vectorSize;
    private bool _isInitialized;

    /// <summary>
    /// Creates a new QdrantVectorStore instance.
    /// </summary>
    /// <param name="qdrantHost">Qdrant server host (e.g., "localhost").</param>
    /// <param name="qdrantPort">Qdrant server port (default: 6334 for gRPC).</param>
    /// <param name="openAIClient">OpenAI client for embeddings.</param>
    /// <param name="embeddingModel">Embedding model name (default: text-embedding-3-small).</param>
    /// <param name="collectionName">Qdrant collection name.</param>
    public QdrantVectorStore(
        string qdrantHost,
        int qdrantPort,
        OpenAIClient openAIClient,
        string embeddingModel = "text-embedding-3-small",
        string collectionName = "expenses")
    {
        _qdrantClient = new QdrantClient(qdrantHost, qdrantPort);
        _embeddingGenerator = openAIClient
            .GetEmbeddingClient(embeddingModel)
            .AsIEmbeddingGenerator();
        _collectionName = collectionName;
        _vectorSize = 1536; // text-embedding-3-small dimension
    }

    /// <summary>
    /// Creates a QdrantVectorStore with default settings for local development.
    /// </summary>
    public static QdrantVectorStore CreateLocal(OpenAIClient openAIClient)
    {
        return new QdrantVectorStore("localhost", 6334, openAIClient);
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        try
        {
            // Check if collection exists
            var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);

            if (!collections.Contains(_collectionName))
            {
                // Create collection with vector configuration
                await _qdrantClient.CreateCollectionAsync(
                    _collectionName,
                    new VectorParams
                    {
                        Size = (ulong)_vectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);

                Console.WriteLine($"[Qdrant] Created collection: {_collectionName}");
            }

            _isInitialized = true;
            Console.WriteLine("[Qdrant] Vector store initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Qdrant] Failed to initialize: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _qdrantClient.ListCollectionsAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task UpsertExpenseAsync(
        string expenseId,
        string userId,
        string text,
        ExpenseVectorMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        // Generate embedding
        var embedding = await _embeddingGenerator.GenerateAsync(
            text,
            cancellationToken: cancellationToken);

        var vector = embedding.Vector.ToArray();

        // Create point with metadata
        var point = new PointStruct
        {
            Id = new PointId { Uuid = expenseId },
            Vectors = vector,
            Payload =
            {
                ["expense_id"] = expenseId,
                ["user_id"] = userId,
                ["description"] = metadata.Description,
                ["category_id"] = metadata.CategoryId,
                ["category_name"] = metadata.CategoryName,
                ["amount"] = (double)metadata.Amount,
                ["date"] = metadata.Date.ToString("o"),
                ["location"] = metadata.Location ?? ""
            }
        };

        // Upsert to Qdrant
        await _qdrantClient.UpsertAsync(
            _collectionName,
            [point],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string userId,
        string query,
        int limit = 10,
        float minScore = 0.7f,
        CancellationToken cancellationToken = default)
    {
        if (!await IsAvailableAsync(cancellationToken))
        {
            return [];
        }

        await InitializeAsync(cancellationToken);

        // Generate query embedding
        var embedding = await _embeddingGenerator.GenerateAsync(
            query,
            cancellationToken: cancellationToken);

        var queryVector = embedding.Vector.ToArray();

        // Search with user filter
        var results = await _qdrantClient.SearchAsync(
            _collectionName,
            queryVector,
            filter: new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "user_id",
                            Match = new Match { Keyword = userId }
                        }
                    }
                }
            },
            limit: (ulong)limit,
            scoreThreshold: minScore,
            cancellationToken: cancellationToken);

        return results.Select(r => new VectorSearchResult(
            ExpenseId: r.Payload["expense_id"].StringValue,
            Score: r.Score,
            Metadata: new ExpenseVectorMetadata(
                ExpenseId: r.Payload["expense_id"].StringValue,
                UserId: r.Payload["user_id"].StringValue,
                Description: r.Payload["description"].StringValue,
                CategoryId: r.Payload["category_id"].StringValue,
                CategoryName: r.Payload["category_name"].StringValue,
                Amount: (decimal)r.Payload["amount"].DoubleValue,
                Date: DateTime.Parse(r.Payload["date"].StringValue),
                Location: string.IsNullOrEmpty(r.Payload["location"].StringValue)
                    ? null
                    : r.Payload["location"].StringValue
            )
        )).ToList();
    }

    /// <inheritdoc/>
    public async Task DeleteExpenseAsync(string expenseId, CancellationToken cancellationToken = default)
    {
        if (!await IsAvailableAsync(cancellationToken))
        {
            return;
        }

        // Delete by filter on expense_id field
        await _qdrantClient.DeleteAsync(
            _collectionName,
            filter: new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "expense_id",
                            Match = new Match { Keyword = expenseId }
                        }
                    }
                }
            },
            cancellationToken: cancellationToken);
    }
}
