// ============================================================================
// 07. DEV TEAM - MULTI-AGENT
// FILE: TeamOrchestrator.cs
// ============================================================================
// This file implements the orchestrator that coordinates the team of agents.
//
// SUPPORTED ORCHESTRATION PATTERNS:
//
// 1. SEQUENTIAL (Pipeline):
//    ┌──────────┐    ┌──────────┐    ┌──────────┐
//    │ Architect│ ─► │ Developer│ ─► │ Reviewer │
//    └──────────┘    └──────────┘    └──────────┘
//    The output of one agent becomes input for the next.
//
// 2. PARALLEL (Fan-out / Fan-in):
//    ┌──────────┐
//    │ Architect│ ─┐
//    └──────────┘  │
//                  ├─► Result aggregation
//    ┌──────────┐  │
//    │ Developer│ ─┘
//    └──────────┘
//    Multiple agents work in parallel, then results are aggregated.
//
// 3. ROUTING (Conditional):
//    ┌──────────────┐
//    │   Analysis   │
//    └──────┬───────┘
//           │
//     ┌─────┴─────┐
//     ▼           ▼
//  [Design?]  [Code?]
//     │           │
//     ▼           ▼
//  Architect   Developer
//
//    Choose which agent to involve based on content.
// ============================================================================

using DevTeam.MultiAgent.Agents;
using OpenAI.Chat;

namespace DevTeam.MultiAgent.Orchestration;

/// <summary>
/// Workflow type for orchestration.
/// </summary>
public enum WorkflowType
{
    /// <summary>
    /// Agents work in sequence (pipeline).
    /// </summary>
    Sequential,

    /// <summary>
    /// Agents work in parallel.
    /// </summary>
    Parallel,

    /// <summary>
    /// An agent is chosen based on content.
    /// </summary>
    Routed
}

/// <summary>
/// Result of a team operation.
/// </summary>
public class TeamResult
{
    /// <summary>
    /// Member that produced the result.
    /// </summary>
    public required TeamMember Member { get; init; }

    /// <summary>
    /// Agent's response.
    /// </summary>
    public required string Response { get; init; }

    /// <summary>
    /// Execution time.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Orchestrator for the team of agents.
///
/// Manages:
/// - Team creation and configuration
/// - Multi-agent workflow execution
/// - Result aggregation
/// - Operation logging
/// </summary>
public class TeamOrchestrator
{
    // ========================================================================
    // TEAM MEMBERS
    // ========================================================================

    private readonly Dictionary<TeamRole, TeamMember> _team = new();
    private readonly ChatClient _chatClient;

    /// <summary>
    /// Event for operation logging.
    /// </summary>
    public event Action<string>? OnLog;

    // ========================================================================
    // CONSTRUCTOR
    // ========================================================================

    /// <summary>
    /// Creates a new orchestrator.
    /// </summary>
    /// <param name="chatClient">OpenAI client to use for agents</param>
    public TeamOrchestrator(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    // ========================================================================
    // TEAM MANAGEMENT
    // ========================================================================

    /// <summary>
    /// Adds a member to the team.
    /// </summary>
    public TeamMember AddMember(TeamRole role)
    {
        if (_team.ContainsKey(role))
        {
            Log($"⚠️ Member {role} already present, being replaced");
        }

        var member = TeamMember.Create(role, _chatClient);
        _team[role] = member;

        Log($"✅ {member} added to team");
        return member;
    }

    /// <summary>
    /// Gets a team member.
    /// </summary>
    public TeamMember? GetMember(TeamRole role)
    {
        return _team.GetValueOrDefault(role);
    }

    /// <summary>
    /// Gets all team members.
    /// </summary>
    public IEnumerable<TeamMember> GetAllMembers() => _team.Values;

    /// <summary>
    /// Initializes the full team.
    /// </summary>
    public void InitializeFullTeam()
    {
        AddMember(TeamRole.TeamLead);
        AddMember(TeamRole.Architect);
        AddMember(TeamRole.Developer);
        AddMember(TeamRole.Reviewer);
    }

    // ========================================================================
    // SEQUENTIAL WORKFLOW
    // ========================================================================

    /// <summary>
    /// Executes a sequential workflow (pipeline).
    ///
    /// Each agent receives the output of the previous one as input.
    /// Useful for: Design → Implement → Review
    /// </summary>
    /// <param name="initialPrompt">Initial prompt</param>
    /// <param name="roles">Sequence of roles to involve</param>
    /// <returns>List of results in order</returns>
    public async Task<List<TeamResult>> RunSequentialAsync(
        string initialPrompt,
        params TeamRole[] roles)
    {
        Log($"🔄 Starting SEQUENTIAL workflow with {roles.Length} steps");

        var results = new List<TeamResult>();
        var currentInput = initialPrompt;

        foreach (var role in roles)
        {
            var member = GetMember(role);
            if (member == null)
            {
                Log($"❌ Member {role} not found, skip");
                continue;
            }

            Log($"   ► {member} is processing...");

            var startTime = DateTime.UtcNow;

            // Build the prompt including previous context
            var prompt = results.Count > 0
                ? $"Context from previous step:\n{currentInput}\n\nNow it's your turn. Proceed with your task."
                : currentInput;

            var response = await member.AskAsync(prompt);

            var duration = DateTime.UtcNow - startTime;

            var result = new TeamResult
            {
                Member = member,
                Response = response,
                Duration = duration
            };

            results.Add(result);
            currentInput = response; // Output becomes input for the next

            Log($"   ✓ {member} completed in {duration.TotalSeconds:F1}s");
        }

        Log($"✅ Sequential workflow completed: {results.Count} steps");
        return results;
    }

    // ========================================================================
    // PARALLEL WORKFLOW
    // ========================================================================

    /// <summary>
    /// Executes a parallel workflow (fan-out).
    ///
    /// All agents receive the same input and work in parallel.
    /// Useful for: getting different perspectives on the same problem.
    /// </summary>
    /// <param name="prompt">Prompt to send to all</param>
    /// <param name="roles">Roles to involve in parallel</param>
    /// <returns>List of results (order not guaranteed)</returns>
    public async Task<List<TeamResult>> RunParallelAsync(
        string prompt,
        params TeamRole[] roles)
    {
        Log($"⚡ Starting PARALLEL workflow with {roles.Length} agents");

        var tasks = new List<Task<TeamResult>>();

        foreach (var role in roles)
        {
            var member = GetMember(role);
            if (member == null)
            {
                Log($"❌ Member {role} not found, skip");
                continue;
            }

            Log($"   ► {member} started in parallel");

            // Create a task for each agent
            var task = Task.Run(async () =>
            {
                var startTime = DateTime.UtcNow;
                var response = await member.AskAsync(prompt);
                var duration = DateTime.UtcNow - startTime;

                return new TeamResult
                {
                    Member = member,
                    Response = response,
                    Duration = duration
                };
            });

            tasks.Add(task);
        }

        // Wait for all tasks
        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            Log($"   ✓ {result.Member} completed in {result.Duration.TotalSeconds:F1}s");
        }

        Log($"✅ Parallel workflow completed: {results.Length} results");
        return results.ToList();
    }

    // ========================================================================
    // ROUTED WORKFLOW
    // ========================================================================

    /// <summary>
    /// Executes a workflow with content-based routing.
    ///
    /// The TeamLead analyzes the request and decides which agent to involve.
    /// </summary>
    /// <param name="request">User's request</param>
    /// <returns>Result with the selected agent</returns>
    public async Task<TeamResult> RunRoutedAsync(string request)
    {
        Log($"🔀 Starting ROUTED workflow");

        var teamLead = GetMember(TeamRole.TeamLead);
        if (teamLead == null)
        {
            throw new InvalidOperationException("TeamLead not found in team");
        }

        // Step 1: TeamLead analyzes and decides
        Log($"   ► {teamLead} is analyzing the request...");

        var analysisPrompt = $"""
            Analyze this request and decide which team member should handle it.

            REQUEST: {request}

            AVAILABLE MEMBERS:
            - ARCHITECT: for design issues, architecture, patterns, technical decisions
            - DEVELOPER: to write code, implement features, fix bugs
            - REVIEWER: to review existing code, find problems, suggest improvements

            Respond ONLY with the name of the most appropriate member (ARCHITECT, DEVELOPER, or REVIEWER)
            followed by a brief explanation of why.

            Format: MEMBER_NAME | Reason
            """;

        var analysisResponse = await teamLead.AskAsync(analysisPrompt);
        Log($"   ✓ Analysis: {analysisResponse}");

        // Step 2: Determine the selected role
        var selectedRole = DetermineRoleFromResponse(analysisResponse);
        Log($"   → Routing to: {selectedRole}");

        // Step 3: Send the request to the selected agent
        var selectedMember = GetMember(selectedRole);
        if (selectedMember == null)
        {
            // Fallback to developer
            selectedMember = GetMember(TeamRole.Developer) ?? teamLead;
        }

        Log($"   ► {selectedMember} is processing...");

        var startTime = DateTime.UtcNow;
        var response = await selectedMember.AskAsync(request);
        var duration = DateTime.UtcNow - startTime;

        Log($"   ✓ {selectedMember} completed in {duration.TotalSeconds:F1}s");

        return new TeamResult
        {
            Member = selectedMember,
            Response = response,
            Duration = duration
        };
    }

    /// <summary>
    /// Determines the role from the TeamLead's response.
    /// </summary>
    private TeamRole DetermineRoleFromResponse(string response)
    {
        var upper = response.ToUpperInvariant();

        if (upper.Contains("ARCHITECT"))
            return TeamRole.Architect;

        if (upper.Contains("REVIEWER") || upper.Contains("REVIEW"))
            return TeamRole.Reviewer;

        // Default to Developer
        return TeamRole.Developer;
    }

    // ========================================================================
    // FULL WORKFLOW: DESIGN-IMPLEMENT-REVIEW
    // ========================================================================

    /// <summary>
    /// Executes the full development workflow.
    ///
    /// 1. Architect designs the solution
    /// 2. Developer implements the code
    /// 3. Reviewer reviews the result
    /// </summary>
    /// <param name="requirement">Requirement to implement</param>
    /// <returns>Results from all steps</returns>
    public async Task<List<TeamResult>> RunFullDevelopmentCycleAsync(string requirement)
    {
        Log($"🚀 Starting FULL DEVELOPMENT CYCLE");
        Log($"   Requirement: {requirement}");

        // Step 1: Architect
        var architect = GetMember(TeamRole.Architect);
        if (architect == null) throw new InvalidOperationException("Architect missing");

        Log($"\n📐 PHASE 1: DESIGN");
        Log($"   ► {architect} is designing...");

        var designPrompt = $"""
            Design a solution for this requirement:

            {requirement}

            Provide:
            1. Proposed architecture
            2. Main components
            3. Interfaces between components
            4. Technical considerations
            """;

        var designStart = DateTime.UtcNow;
        var design = await architect.AskAsync(designPrompt);
        var designDuration = DateTime.UtcNow - designStart;

        Log($"   ✓ Design completed in {designDuration.TotalSeconds:F1}s");

        // Step 2: Developer
        var developer = GetMember(TeamRole.Developer);
        if (developer == null) throw new InvalidOperationException("Developer missing");

        Log($"\n💻 PHASE 2: IMPLEMENTATION");
        Log($"   ► {developer} is implementing...");

        var implementPrompt = $"""
            Implement the code following this design:

            === DESIGN ===
            {design}
            === END DESIGN ===

            Write complete C# code with:
            - Necessary classes and interfaces
            - Logic implementation
            - Appropriate error handling
            - Comments where useful
            """;

        var implStart = DateTime.UtcNow;
        var implementation = await developer.AskAsync(implementPrompt);
        var implDuration = DateTime.UtcNow - implStart;

        Log($"   ✓ Implementation completed in {implDuration.TotalSeconds:F1}s");

        // Step 3: Reviewer
        var reviewer = GetMember(TeamRole.Reviewer);
        if (reviewer == null) throw new InvalidOperationException("Reviewer missing");

        Log($"\n🔍 PHASE 3: CODE REVIEW");
        Log($"   ► {reviewer} is reviewing...");

        var reviewPrompt = $"""
            Review this code:

            === CODE ===
            {implementation}
            === END CODE ===

            Provide a complete review with:
            ✅ Positive points
            ⚠️ Improvement suggestions
            ❌ Problems to fix
            📊 Overall score (1-10)
            """;

        var reviewStart = DateTime.UtcNow;
        var review = await reviewer.AskAsync(reviewPrompt);
        var reviewDuration = DateTime.UtcNow - reviewStart;

        Log($"   ✓ Review completed in {reviewDuration.TotalSeconds:F1}s");

        Log($"\n✅ FULL CYCLE COMPLETED");

        return new List<TeamResult>
        {
            new() { Member = architect, Response = design, Duration = designDuration },
            new() { Member = developer, Response = implementation, Duration = implDuration },
            new() { Member = reviewer, Response = review, Duration = reviewDuration }
        };
    }

    // ========================================================================
    // HELPER
    // ========================================================================

    private void Log(string message)
    {
        OnLog?.Invoke(message);
    }
}
