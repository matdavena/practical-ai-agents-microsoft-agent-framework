// ============================================================================
// CHAT API MODELS
// ============================================================================
// Models for the REST API following OpenAI/ChatGPT-style conventions.
// These models enable React or any frontend to communicate with agents.
// ============================================================================

using System.Text.Json.Serialization;

namespace WebApi.ChatAgents.Models;

// ============================================================================
// REQUEST MODELS
// ============================================================================

/// <summary>
/// Request to send a message in a conversation.
/// Similar to OpenAI Chat Completions API.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// The user's message content.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// Optional conversation ID to continue an existing conversation.
    /// If null, a new conversation is created.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    /// <summary>
    /// Whether to stream the response (Server-Sent Events).
    /// Default is false.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

/// <summary>
/// Request to create a new conversation.
/// </summary>
public class CreateConversationRequest
{
    /// <summary>
    /// Optional title for the conversation.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Optional system prompt override for this conversation.
    /// </summary>
    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; set; }
}

// ============================================================================
// RESPONSE MODELS
// ============================================================================

/// <summary>
/// Response from a chat message.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// The conversation ID (for continuing the conversation).
    /// </summary>
    [JsonPropertyName("conversationId")]
    public required string ConversationId { get; set; }

    /// <summary>
    /// The assistant's response message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// The role of the responder (always "assistant" for now).
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    /// <summary>
    /// Timestamp of the response.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Message ID for this specific message.
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Streaming response chunk (for SSE).
/// </summary>
public class ChatStreamChunk
{
    /// <summary>
    /// The conversation ID.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public required string ConversationId { get; set; }

    /// <summary>
    /// The text content of this chunk.
    /// </summary>
    [JsonPropertyName("delta")]
    public required string Delta { get; set; }

    /// <summary>
    /// Whether this is the final chunk.
    /// </summary>
    [JsonPropertyName("done")]
    public bool Done { get; set; } = false;
}

/// <summary>
/// Information about a conversation.
/// </summary>
public class ConversationInfo
{
    /// <summary>
    /// Unique conversation ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// User ID who owns this conversation.
    /// </summary>
    [JsonPropertyName("userId")]
    public required string UserId { get; set; }

    /// <summary>
    /// The agent used for this conversation.
    /// </summary>
    [JsonPropertyName("agentKey")]
    public required string AgentKey { get; set; }

    /// <summary>
    /// Title of the conversation (auto-generated or user-defined).
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// When the conversation was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the conversation was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Number of messages in the conversation.
    /// </summary>
    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; } = 0;
}

/// <summary>
/// A message in a conversation history.
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Unique message ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Role: "user" or "assistant".
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// Message content.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    /// <summary>
    /// When the message was sent.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Full conversation with message history.
/// </summary>
public class ConversationWithHistory
{
    /// <summary>
    /// Conversation metadata.
    /// </summary>
    [JsonPropertyName("conversation")]
    public required ConversationInfo Conversation { get; set; }

    /// <summary>
    /// Message history.
    /// </summary>
    [JsonPropertyName("messages")]
    public required List<ConversationMessage> Messages { get; set; }
}

/// <summary>
/// Information about an available agent.
/// </summary>
public class AgentInfo
{
    /// <summary>
    /// Unique key to identify the agent.
    /// </summary>
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    /// <summary>
    /// Display name of the agent.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Description of what the agent does.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }
}
