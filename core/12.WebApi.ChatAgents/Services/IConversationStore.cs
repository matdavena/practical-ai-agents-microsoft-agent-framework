// ============================================================================
// CONVERSATION STORE INTERFACE
// ============================================================================
// Abstraction for storing and retrieving conversations.
// This enables swapping implementations (in-memory, database, Redis, etc.).
// ============================================================================

using System.Text.Json;
using WebApi.ChatAgents.Models;

namespace WebApi.ChatAgents.Services;

/// <summary>
/// Interface for conversation storage.
/// Implementations can use in-memory storage, databases, or distributed caches.
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    Task<ConversationInfo> CreateConversationAsync(
        string userId,
        string agentKey,
        string? title = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conversation by ID.
    /// </summary>
    Task<ConversationInfo?> GetConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all conversations for a user.
    /// </summary>
    Task<IEnumerable<ConversationInfo>> GetUserConversationsAsync(
        string userId,
        string? agentKey = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation.
    /// </summary>
    Task<bool> DeleteConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the agent thread state for a conversation.
    /// This enables resuming conversations later.
    /// </summary>
    Task SaveThreadStateAsync(
        string conversationId,
        JsonElement threadState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the saved agent thread state for a conversation.
    /// </summary>
    Task<JsonElement?> GetThreadStateAsync(
        string conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message to the conversation history.
    /// </summary>
    Task AddMessageAsync(
        string conversationId,
        string role,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the message history for a conversation.
    /// </summary>
    Task<IEnumerable<ConversationMessage>> GetMessagesAsync(
        string conversationId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates conversation metadata (title, updatedAt).
    /// </summary>
    Task UpdateConversationAsync(
        string conversationId,
        string? title = null,
        CancellationToken cancellationToken = default);
}
