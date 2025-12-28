// ============================================================================
// CHAT API CLIENT
// ============================================================================
// HTTP client wrapper for the Chat Agents API.
// Supports multiple users via X-User-Id header.
// ============================================================================

using System.Net.Http.Json;
using System.Text.Json;

namespace WebApi.ChatAgents.Client;

/// <summary>
/// HTTP client for the Chat Agents API.
/// Each instance represents a specific user.
/// </summary>
public class ChatApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _userId;
    private readonly JsonSerializerOptions _jsonOptions;

    public string UserId => _userId;
    public string BaseUrl { get; }

    public ChatApiClient(string baseUrl, string userId)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _userId = userId;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("X-User-Id", userId);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // ========================================================================
    // AGENT ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get list of available agents.
    /// </summary>
    public async Task<List<AgentInfo>> GetAgentsAsync()
    {
        var response = await _httpClient.GetAsync("/api/agents");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AgentInfo>>(_jsonOptions)
            ?? new List<AgentInfo>();
    }

    /// <summary>
    /// Get information about a specific agent.
    /// </summary>
    public async Task<AgentInfo?> GetAgentAsync(string agentKey)
    {
        var response = await _httpClient.GetAsync($"/api/agents/{agentKey}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AgentInfo>(_jsonOptions);
    }

    // ========================================================================
    // CHAT ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Send a message to an agent.
    /// </summary>
    public async Task<ChatResponse?> ChatAsync(string agentKey, string message, string? conversationId = null)
    {
        var request = new ChatRequest
        {
            Message = message,
            ConversationId = conversationId
        };

        var response = await _httpClient.PostAsJsonAsync($"/api/chat/{agentKey}", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Chat failed: {response.StatusCode} - {error}");
        }

        return await response.Content.ReadFromJsonAsync<ChatResponse>(_jsonOptions);
    }

    // ========================================================================
    // CONVERSATION ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get all conversations for the current user.
    /// </summary>
    public async Task<List<ConversationInfo>> GetConversationsAsync(string? agentKey = null)
    {
        var url = "/api/conversations";
        if (!string.IsNullOrEmpty(agentKey))
        {
            url += $"?agentKey={agentKey}";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ConversationInfo>>(_jsonOptions)
            ?? new List<ConversationInfo>();
    }

    /// <summary>
    /// Get a specific conversation with its message history.
    /// </summary>
    public async Task<ConversationWithHistory?> GetConversationAsync(string conversationId)
    {
        var response = await _httpClient.GetAsync($"/api/conversations/{conversationId}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ConversationWithHistory>(_jsonOptions);
    }

    /// <summary>
    /// Update a conversation (e.g., rename).
    /// </summary>
    public async Task<ConversationInfo?> UpdateConversationAsync(string conversationId, string title)
    {
        var request = new { title };
        var response = await _httpClient.PatchAsJsonAsync($"/api/conversations/{conversationId}", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ConversationInfo>(_jsonOptions);
    }

    /// <summary>
    /// Delete a conversation.
    /// </summary>
    public async Task<bool> DeleteConversationAsync(string conversationId)
    {
        var response = await _httpClient.DeleteAsync($"/api/conversations/{conversationId}");
        return response.IsSuccessStatusCode;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

// ============================================================================
// API MODELS
// ============================================================================

public class ChatRequest
{
    public required string Message { get; set; }
    public string? ConversationId { get; set; }
    public bool Stream { get; set; } = false;
}

public class ChatResponse
{
    public required string ConversationId { get; set; }
    public required string Message { get; set; }
    public string Role { get; set; } = "assistant";
    public DateTimeOffset Timestamp { get; set; }
    public string? MessageId { get; set; }
}

public class AgentInfo
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}

public class ConversationInfo
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string AgentKey { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

public class ConversationMessage
{
    public required string Id { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class ConversationWithHistory
{
    public required ConversationInfo Conversation { get; set; }
    public required List<ConversationMessage> Messages { get; set; }
}
