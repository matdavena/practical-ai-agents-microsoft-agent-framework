// ============================================================================
// CHAT CONTROLLER
// ============================================================================
// REST API endpoints for chat functionality.
// Demonstrates:
// - Using Keyed Services to resolve different agents
// - Per-user conversation management
// - Thread state persistence for resumable conversations
// - ChatGPT-style API patterns
// ============================================================================

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using WebApi.ChatAgents.Models;
using WebApi.ChatAgents.Services;

namespace WebApi.ChatAgents.Controllers;

/// <summary>
/// Chat API Controller.
/// Provides REST endpoints for interacting with AI agents.
/// </summary>
[ApiController]
[Route("api")]
public class ChatController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConversationStore _conversationStore;
    private readonly ILogger<ChatController> _logger;

    // Agent metadata (in production, this could come from configuration)
    private static readonly Dictionary<string, AgentInfo> _availableAgents = new()
    {
        ["assistant"] = new AgentInfo
        {
            Key = "assistant",
            Name = "General Assistant",
            Description = "A helpful general-purpose AI assistant for everyday tasks."
        },
        ["coder"] = new AgentInfo
        {
            Key = "coder",
            Name = "Code Assistant",
            Description = "A specialized assistant for programming and code-related tasks."
        },
        ["translator"] = new AgentInfo
        {
            Key = "translator",
            Name = "Translator",
            Description = "A multilingual translator for translating text between languages."
        }
    };

    public ChatController(
        IServiceProvider serviceProvider,
        IConversationStore conversationStore,
        ILogger<ChatController> logger)
    {
        _serviceProvider = serviceProvider;
        _conversationStore = conversationStore;
        _logger = logger;
    }

    // ========================================================================
    // AGENT ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get list of available agents.
    /// </summary>
    /// <returns>List of available agents with their metadata.</returns>
    [HttpGet("agents")]
    [ProducesResponseType(typeof(IEnumerable<AgentInfo>), StatusCodes.Status200OK)]
    public IActionResult GetAgents()
    {
        return Ok(_availableAgents.Values);
    }

    /// <summary>
    /// Get information about a specific agent.
    /// </summary>
    /// <param name="agentKey">The agent key.</param>
    /// <returns>Agent information.</returns>
    [HttpGet("agents/{agentKey}")]
    [ProducesResponseType(typeof(AgentInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAgent(string agentKey)
    {
        if (!_availableAgents.TryGetValue(agentKey, out var agent))
        {
            return NotFound(new { error = $"Agent '{agentKey}' not found." });
        }
        return Ok(agent);
    }

    // ========================================================================
    // CHAT ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Send a message to an agent.
    /// Creates a new conversation if conversationId is not provided.
    /// </summary>
    /// <param name="agentKey">The agent to chat with.</param>
    /// <param name="request">The chat request.</param>
    /// <returns>The agent's response.</returns>
    [HttpPost("chat/{agentKey}")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Chat(
        string agentKey,
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        // Validate agent exists
        if (!_availableAgents.ContainsKey(agentKey))
        {
            return NotFound(new { error = $"Agent '{agentKey}' not found." });
        }

        // Get user ID (in production, this comes from authentication)
        var userId = GetUserId();

        try
        {
            // Get the agent using Keyed Services
            var agent = _serviceProvider.GetRequiredKeyedService<ChatClientAgent>(agentKey);

            // Get or create conversation
            ConversationInfo conversation;
            AgentThread thread;

            if (string.IsNullOrEmpty(request.ConversationId))
            {
                // Create new conversation
                conversation = await _conversationStore.CreateConversationAsync(
                    userId, agentKey, null, cancellationToken);
                thread = agent.GetNewThread();

                _logger.LogInformation(
                    "Created new conversation {ConversationId} for user {UserId} with agent {AgentKey}",
                    conversation.Id, userId, agentKey);
            }
            else
            {
                // Get existing conversation
                conversation = await _conversationStore.GetConversationAsync(
                    request.ConversationId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Conversation '{request.ConversationId}' not found.");

                // Verify user owns this conversation
                if (conversation.UserId != userId)
                {
                    return Forbid();
                }

                // Restore thread state if available
                var savedState = await _conversationStore.GetThreadStateAsync(
                    conversation.Id, cancellationToken);

                if (savedState.HasValue)
                {
                    thread = agent.DeserializeThread(savedState.Value);
                    _logger.LogInformation(
                        "Restored thread state for conversation {ConversationId}",
                        conversation.Id);
                }
                else
                {
                    thread = agent.GetNewThread();
                }
            }

            // Save user message
            await _conversationStore.AddMessageAsync(
                conversation.Id, "user", request.Message, cancellationToken);

            // Invoke the agent
            var agentResponse = await agent.RunAsync(request.Message, thread);
            var responseText = agentResponse.ToString();

            // Save assistant response
            await _conversationStore.AddMessageAsync(
                conversation.Id, "assistant", responseText, cancellationToken);

            // Save thread state for later resumption
            var threadState = thread.Serialize();
            await _conversationStore.SaveThreadStateAsync(
                conversation.Id, threadState, cancellationToken);

            _logger.LogInformation(
                "Completed chat in conversation {ConversationId}",
                conversation.Id);

            return Ok(new ChatResponse
            {
                ConversationId = conversation.Id,
                Message = responseText
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new { error = "An error occurred processing your request." });
        }
    }

    // ========================================================================
    // CONVERSATION ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get all conversations for the current user.
    /// </summary>
    /// <param name="agentKey">Optional filter by agent.</param>
    /// <returns>List of conversations.</returns>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IEnumerable<ConversationInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] string? agentKey = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var conversations = await _conversationStore.GetUserConversationsAsync(
            userId, agentKey, cancellationToken);
        return Ok(conversations);
    }

    /// <summary>
    /// Get a specific conversation with its message history.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <returns>Conversation with messages.</returns>
    [HttpGet("conversations/{conversationId}")]
    [ProducesResponseType(typeof(ConversationWithHistory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var conversation = await _conversationStore.GetConversationAsync(
            conversationId, cancellationToken);

        if (conversation == null)
        {
            return NotFound(new { error = "Conversation not found." });
        }

        // Verify user owns this conversation
        if (conversation.UserId != userId)
        {
            return Forbid();
        }

        var messages = await _conversationStore.GetMessagesAsync(
            conversationId, cancellationToken: cancellationToken);

        return Ok(new ConversationWithHistory
        {
            Conversation = conversation,
            Messages = messages.ToList()
        });
    }

    /// <summary>
    /// Update a conversation (e.g., rename it).
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="request">Update request.</param>
    /// <returns>Updated conversation info.</returns>
    [HttpPatch("conversations/{conversationId}")]
    [ProducesResponseType(typeof(ConversationInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConversation(
        string conversationId,
        [FromBody] UpdateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var conversation = await _conversationStore.GetConversationAsync(
            conversationId, cancellationToken);

        if (conversation == null)
        {
            return NotFound(new { error = "Conversation not found." });
        }

        if (conversation.UserId != userId)
        {
            return Forbid();
        }

        await _conversationStore.UpdateConversationAsync(
            conversationId, request.Title, cancellationToken);

        // Return updated conversation
        conversation = await _conversationStore.GetConversationAsync(
            conversationId, cancellationToken);

        return Ok(conversation);
    }

    /// <summary>
    /// Delete a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("conversations/{conversationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var conversation = await _conversationStore.GetConversationAsync(
            conversationId, cancellationToken);

        if (conversation == null)
        {
            return NotFound(new { error = "Conversation not found." });
        }

        if (conversation.UserId != userId)
        {
            return Forbid();
        }

        await _conversationStore.DeleteConversationAsync(conversationId, cancellationToken);

        _logger.LogInformation(
            "Deleted conversation {ConversationId} for user {UserId}",
            conversationId, userId);

        return NoContent();
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Gets the current user ID.
    /// In production, this would come from authentication/JWT claims.
    /// For demo purposes, we use a header or default to "demo-user".
    /// </summary>
    private string GetUserId()
    {
        // Check for X-User-Id header (for demo/testing)
        if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
        {
            return userIdHeader.ToString();
        }

        // In production, use: User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        return "demo-user";
    }
}

/// <summary>
/// Request to update a conversation.
/// </summary>
public class UpdateConversationRequest
{
    /// <summary>
    /// New title for the conversation.
    /// </summary>
    public string? Title { get; set; }
}
