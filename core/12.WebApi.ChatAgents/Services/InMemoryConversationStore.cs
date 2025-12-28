// ============================================================================
// IN-MEMORY CONVERSATION STORE
// ============================================================================
// Simple in-memory implementation for development and demos.
// In production, replace with database or Redis implementation.
// ============================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using WebApi.ChatAgents.Models;

namespace WebApi.ChatAgents.Services;

/// <summary>
/// In-memory implementation of IConversationStore.
/// Thread-safe using ConcurrentDictionary.
/// Data is lost when the application restarts.
/// </summary>
public class InMemoryConversationStore : IConversationStore
{
    // Storage dictionaries
    private readonly ConcurrentDictionary<string, ConversationInfo> _conversations = new();
    private readonly ConcurrentDictionary<string, List<ConversationMessage>> _messages = new();
    private readonly ConcurrentDictionary<string, JsonElement> _threadStates = new();

    public Task<ConversationInfo> CreateConversationAsync(
        string userId,
        string agentKey,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = new ConversationInfo
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            AgentKey = agentKey,
            Title = title ?? "New Conversation",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            MessageCount = 0
        };

        _conversations[conversation.Id] = conversation;
        _messages[conversation.Id] = new List<ConversationMessage>();

        return Task.FromResult(conversation);
    }

    public Task<ConversationInfo?> GetConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        _conversations.TryGetValue(conversationId, out var conversation);
        return Task.FromResult(conversation);
    }

    public Task<IEnumerable<ConversationInfo>> GetUserConversationsAsync(
        string userId,
        string? agentKey = null,
        CancellationToken cancellationToken = default)
    {
        var conversations = _conversations.Values
            .Where(c => c.UserId == userId)
            .Where(c => agentKey == null || c.AgentKey == agentKey)
            .OrderByDescending(c => c.UpdatedAt)
            .ToList();

        return Task.FromResult<IEnumerable<ConversationInfo>>(conversations);
    }

    public Task<bool> DeleteConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        var removed = _conversations.TryRemove(conversationId, out _);
        _messages.TryRemove(conversationId, out _);
        _threadStates.TryRemove(conversationId, out _);

        return Task.FromResult(removed);
    }

    public Task SaveThreadStateAsync(
        string conversationId,
        JsonElement threadState,
        CancellationToken cancellationToken = default)
    {
        _threadStates[conversationId] = threadState;
        return Task.CompletedTask;
    }

    public Task<JsonElement?> GetThreadStateAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        if (_threadStates.TryGetValue(conversationId, out var state))
        {
            return Task.FromResult<JsonElement?>(state);
        }
        return Task.FromResult<JsonElement?>(null);
    }

    public Task AddMessageAsync(
        string conversationId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (!_messages.ContainsKey(conversationId))
        {
            _messages[conversationId] = new List<ConversationMessage>();
        }

        var message = new ConversationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = role,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow
        };

        _messages[conversationId].Add(message);

        // Update conversation metadata
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            conversation.UpdatedAt = DateTimeOffset.UtcNow;
            conversation.MessageCount = _messages[conversationId].Count;

            // Auto-generate title from first user message if not set
            if (conversation.Title == "New Conversation" && role == "user")
            {
                conversation.Title = content.Length > 50
                    ? content[..47] + "..."
                    : content;
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<ConversationMessage>> GetMessagesAsync(
        string conversationId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (!_messages.TryGetValue(conversationId, out var messages))
        {
            return Task.FromResult<IEnumerable<ConversationMessage>>([]);
        }

        IEnumerable<ConversationMessage> result = messages;
        if (limit.HasValue)
        {
            result = messages.TakeLast(limit.Value);
        }

        return Task.FromResult(result);
    }

    public Task UpdateConversationAsync(
        string conversationId,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            if (title != null)
            {
                conversation.Title = title;
            }
            conversation.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }
}
