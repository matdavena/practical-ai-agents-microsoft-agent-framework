// ============================================================================
// NullVectorStore
// ============================================================================
// No-op implementation of IVectorStore for when vector search is disabled
// or Qdrant is not available. Provides graceful degradation.
// ============================================================================

using ExpenseTracker.Core.Abstractions;

namespace ExpenseTracker.Infrastructure.VectorStore;

/// <summary>
/// Null implementation that does nothing.
/// Used when vector search is disabled or unavailable.
/// </summary>
public class NullVectorStore : IVectorStore
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task UpsertExpenseAsync(
        string expenseId,
        string userId,
        string text,
        ExpenseVectorMetadata metadata,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string userId,
        string query,
        int limit = 10,
        float minScore = 0.7f,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);

    public Task DeleteExpenseAsync(string expenseId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
