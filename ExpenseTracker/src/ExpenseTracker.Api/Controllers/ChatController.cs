// ============================================================================
// ChatController
// ============================================================================
// AI-powered chat endpoint for natural language expense management.
// Maintains conversation context per session.
// ============================================================================

using System.Collections.Concurrent;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Core.Agents;
using ExpenseTracker.Core.Services;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using OpenAI;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly IBudgetService _budgetService;
    private readonly OpenAIClient _openAIClient;
    private readonly string _model;

    // In-memory conversation storage (use Redis/DB in production)
    private static readonly ConcurrentDictionary<string, (OrchestratorAgent Agent, AgentThread Thread)> _conversations = new();

    // Default user for API
    private const string DefaultUserId = "api-user";

    public ChatController(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        OpenAIClient openAIClient,
        string model)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _budgetService = budgetService;
        _openAIClient = openAIClient;
        _model = model;
    }

    /// <summary>
    /// Sends a message to the AI assistant and receives a response.
    /// </summary>
    /// <remarks>
    /// The AI can:
    /// - Parse and save expenses from natural language
    /// - Answer questions about your expenses
    /// - Generate reports and summaries
    ///
    /// Use conversationId to maintain context across multiple messages.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required");

        var effectiveUserId = request.UserId ?? DefaultUserId;
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        // Get or create conversation
        var (agent, thread) = _conversations.GetOrAdd(conversationId, _ =>
        {
            var newAgent = OrchestratorAgent.Create(
                _openAIClient,
                _expenseService,
                _categoryService,
                _budgetService,
                effectiveUserId,
                _model);
            return (newAgent, newAgent.GetNewThread());
        });

        // Process message
        var response = await agent.ProcessAsync(request.Message, thread, ct);

        return Ok(new ChatResponse(
            Message: response,
            ConversationId: conversationId
        ));
    }

    /// <summary>
    /// Clears a conversation's history.
    /// </summary>
    [HttpDelete("{conversationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ClearConversation(string conversationId)
    {
        _conversations.TryRemove(conversationId, out _);
        return NoContent();
    }
}
