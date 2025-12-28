// ============================================================================
// IVectorStore Interface
// ============================================================================
// Abstraction for vector database operations.
// Used for semantic search over expenses.
//
// BOOK CHAPTER NOTE:
// This demonstrates:
// 1. Abstraction over vector databases
// 2. Semantic search interface design
// 3. Embedding storage and retrieval
// ============================================================================

namespace ExpenseTracker.Core.Abstractions;

/// <summary>
/// Interface for vector store operations.
/// Supports storing and searching embeddings for semantic search.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Initializes the vector store (creates collections if needed).
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an expense embedding in the vector store.
    /// </summary>
    /// <param name="expenseId">The expense ID.</param>
    /// <param name="userId">The user ID who owns the expense.</param>
    /// <param name="text">The text to embed (description + category + location).</param>
    /// <param name="metadata">Additional metadata to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertExpenseAsync(
        string expenseId,
        string userId,
        string text,
        ExpenseVectorMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for similar expenses using semantic search.
    /// </summary>
    /// <param name="userId">The user ID to search within.</param>
    /// <param name="query">The search query text.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="minScore">Minimum similarity score (0-1).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching expense IDs with scores.</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string userId,
        string query,
        int limit = 10,
        float minScore = 0.7f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an expense from the vector store.
    /// </summary>
    Task DeleteExpenseAsync(string expenseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the vector store is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadata stored with expense vectors.
/// </summary>
public record ExpenseVectorMetadata(
    string ExpenseId,
    string UserId,
    string Description,
    string CategoryId,
    string CategoryName,
    decimal Amount,
    DateTime Date,
    string? Location
);

/// <summary>
/// Result from vector search.
/// </summary>
public record VectorSearchResult(
    string ExpenseId,
    float Score,
    ExpenseVectorMetadata Metadata
);
