// ============================================================================
// 06. TASK PLANNER
// FILE: PlannerTools.cs
// ============================================================================
// This file defines the tools the agent uses to create and execute plans.
//
// PLAN-EXECUTE PATTERN:
//
// 1. PLANNING PHASE:
//    The agent analyzes the objective and creates a plan using create_plan
//    ┌────────────────┐
//    │ "Create project│     ┌──────────────┐
//    │  with tests"   │ ──► │ create_plan  │ ──► Plan with N steps
//    └────────────────┘     └──────────────┘
//
// 2. EXECUTE PHASE:
//    The agent executes each step using execute_step
//    ┌────────────────┐     ┌──────────────┐
//    │ Step 1         │ ──► │ execute_step │ ──► Result
//    └────────────────┘     └──────────────┘
//          │
//          ▼
//    ┌────────────────┐     ┌──────────────┐
//    │ Step 2         │ ──► │ execute_step │ ──► Result
//    └────────────────┘     └──────────────┘
//          │
//          ▼
//         ...
//
// ADVANTAGES:
// - The LLM decides WHAT to do (planning)
// - The tools execute the actions (execution)
// - Complete traceability of steps
// - Ability to retry on errors
// ============================================================================

using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace TaskPlanner.Planning;

/// <summary>
/// Tools for task planning and execution.
///
/// These tools are exposed to the agent via AIFunctionFactory.
/// The agent calls them autonomously to:
/// 1. Create an execution plan
/// 2. Execute individual steps of the plan
/// 3. Verify the plan status
/// </summary>
public class PlannerTools
{
    // ========================================================================
    // SHARED STATE
    // ========================================================================

    /// <summary>
    /// Current plan being executed.
    /// Shared among all tools to maintain state.
    /// </summary>
    private TaskPlan? _currentPlan;

    /// <summary>
    /// Callback to notify events during execution.
    /// </summary>
    public event Action<string>? OnLogMessage;

    /// <summary>
    /// Current plan (read-only).
    /// </summary>
    public TaskPlan? CurrentPlan => _currentPlan;

    // ========================================================================
    // TOOL: CREATE_PLAN
    // ========================================================================

    /// <summary>
    /// Creates an execution plan to achieve an objective.
    ///
    /// The agent must call this tool first, passing:
    /// - The objective to achieve
    /// - The plan description
    /// - The list of steps to execute
    ///
    /// Each step must be atomic and verifiable.
    /// </summary>
    /// <param name="goal">Objective to achieve</param>
    /// <param name="planDescription">General description of the plan</param>
    /// <param name="steps">List of steps (separated by |)</param>
    /// <returns>Confirmation of plan creation</returns>
    [Description("""
        Creates an execution plan to achieve a goal.
        Call this tool FIRST before executing any steps.

        Parameters:
        - goal: The objective to achieve (what the user asked for)
        - planDescription: A brief description of the approach
        - steps: List of steps separated by | character. Each step should be:
          * Atomic (one clear action)
          * Verifiable (can check if done)
          * In imperative form ("Create folder", "Write file", etc.)

        Example steps: "Create project folder|Initialize .NET project|Add test project|Write first test|Run tests"
        """)]
    public string CreatePlan(string goal, string planDescription, string steps)
    {
        Log($"📋 Creazione piano per: {goal}");

        // Create the plan
        _currentPlan = new TaskPlan
        {
            Goal = goal,
            PlanDescription = planDescription
        };

        // Parse and add the steps
        var stepDescriptions = steps
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var stepDescription in stepDescriptions)
        {
            _currentPlan.AddStep(stepDescription);
        }

        Log($"   → {_currentPlan.TotalSteps} step creati");

        // Show the steps
        foreach (var step in _currentPlan.Steps)
        {
            Log($"   {step.StatusEmoji} Step {step.Id}: {step.Description}");
        }

        return $"Piano creato con {_currentPlan.TotalSteps} step. " +
               $"Usa 'execute_next_step' per eseguire gli step uno alla volta.";
    }

    // ========================================================================
    // TOOL: EXECUTE_NEXT_STEP
    // ========================================================================

    /// <summary>
    /// Executes the next pending step of the plan.
    ///
    /// The agent must call this tool repeatedly to execute
    /// all steps of the plan, one at a time.
    ///
    /// For each step, the agent must:
    /// 1. Call execute_next_step with the result of the work done
    /// 2. Check if there are more steps (looking at hasMoreSteps)
    /// 3. Continue until completion
    /// </summary>
    /// <param name="stepResult">Description of the work done for this step</param>
    /// <returns>Execution status and information about the next step</returns>
    [Description("""
        Executes the next pending step in the plan.

        Call this tool after completing the work for the current step.
        Pass a description of what was accomplished.

        The response will indicate:
        - Whether the step was completed successfully
        - If there are more steps to execute
        - What the next step is (if any)

        Keep calling this tool until hasMoreSteps is false.
        """)]
    public string ExecuteNextStep(string stepResult)
    {
        if (_currentPlan == null)
        {
            return "Error: No plan exists. Call 'create_plan' first.";
        }

        // Trova lo step corrente (in progress) o il prossimo (pending)
        var currentStep = _currentPlan.CurrentStep ?? _currentPlan.NextStep;

        if (currentStep == null)
        {
            return "All steps have been executed. Plan is complete.";
        }

        // If the step is pending, start it
        if (currentStep.Status == TaskStepStatus.Pending)
        {
            if (_currentPlan.Status == TaskPlanStatus.Planned)
            {
                _currentPlan.StartExecution();
            }
            currentStep.Start();
            Log($"   ► Avviato Step {currentStep.Id}: {currentStep.Description}");
        }

        // Complete the current step
        currentStep.Complete(stepResult);
        Log($"   ✓ Completato Step {currentStep.Id} ({currentStep.Duration?.TotalSeconds:F1}s)");

        // Check if there are more steps
        var nextStep = _currentPlan.NextStep;
        var hasMoreSteps = nextStep != null;

        if (!hasMoreSteps)
        {
            _currentPlan.CompleteExecution();
            Log($"✅ Piano completato! {_currentPlan.CompletedSteps}/{_currentPlan.TotalSteps} step eseguiti.");

            return $"Step {currentStep.Id} completed. " +
                   $"PLAN COMPLETED: All {_currentPlan.TotalSteps} steps executed successfully. " +
                   $"Total duration: {_currentPlan.Duration?.TotalSeconds:F1}s";
        }

        // Start the next step
        nextStep!.Start();
        Log($"   ► Prossimo Step {nextStep.Id}: {nextStep.Description}");

        return $"Step {currentStep.Id} completed. " +
               $"Progress: {_currentPlan.CompletedSteps}/{_currentPlan.TotalSteps}. " +
               $"NEXT STEP ({nextStep.Id}): {nextStep.Description}. " +
               $"Execute this step and call 'execute_next_step' with the result.";
    }

    // ========================================================================
    // TOOL: GET_PLAN_STATUS
    // ========================================================================

    /// <summary>
    /// Gets the current status of the plan.
    ///
    /// Useful for checking progress or resuming
    /// execution after an interruption.
    /// </summary>
    /// <returns>Detailed plan status</returns>
    [Description("""
        Gets the current status of the execution plan.

        Returns:
        - Overall plan status
        - Progress (completed/total steps)
        - Current step being executed
        - List of all steps with their status
        """)]
    public string GetPlanStatus()
    {
        if (_currentPlan == null)
        {
            return "No plan exists. Call 'create_plan' to create one.";
        }

        var lines = new List<string>
        {
            $"Plan: {_currentPlan.Goal}",
            $"Status: {_currentPlan.Status}",
            $"Progress: {_currentPlan.CompletedSteps}/{_currentPlan.TotalSteps} ({_currentPlan.ProgressPercentage}%)",
            "",
            "Steps:"
        };

        foreach (var step in _currentPlan.Steps)
        {
            lines.Add($"  {step}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    // ========================================================================
    // TOOL: MARK_STEP_FAILED
    // ========================================================================

    /// <summary>
    /// Marks the current step as failed.
    ///
    /// Use when the step cannot be completed.
    /// The agent must decide whether to continue or abort the plan.
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Status after failure</returns>
    [Description("""
        Marks the current step as failed.

        Use this when a step cannot be completed successfully.
        Provide an error message explaining what went wrong.

        After marking a step as failed, you can:
        - Continue with the next step (if possible)
        - Abort the entire plan
        """)]
    public string MarkStepFailed(string errorMessage)
    {
        if (_currentPlan == null)
        {
            return "Error: No plan exists.";
        }

        var currentStep = _currentPlan.CurrentStep;

        if (currentStep == null)
        {
            return "Error: No step is currently in progress.";
        }

        currentStep.Fail(errorMessage);
        Log($"   ✗ Step {currentStep.Id} fallito: {errorMessage}");

        var nextStep = _currentPlan.NextStep;

        if (nextStep != null)
        {
            return $"Step {currentStep.Id} marked as failed. " +
                   $"Next step available: {nextStep.Description}. " +
                   $"Call 'execute_next_step' to continue or 'abort_plan' to stop.";
        }

        _currentPlan.CompleteExecution();
        return $"Step {currentStep.Id} marked as failed. " +
               $"No more steps. Plan completed with failures.";
    }

    // ========================================================================
    // TOOL: ABORT_PLAN
    // ========================================================================

    /// <summary>
    /// Completely aborts the plan.
    ///
    /// All uncompleted steps are marked as skipped.
    /// </summary>
    /// <param name="reason">Reason for cancellation</param>
    /// <returns>Cancellation confirmation</returns>
    [Description("""
        Aborts the entire plan.

        Use this when continuing execution is not possible or not desired.
        All pending steps will be marked as skipped.
        """)]
    public string AbortPlan(string reason)
    {
        if (_currentPlan == null)
        {
            return "No plan to abort.";
        }

        _currentPlan.Cancel();
        Log($"⊘ Piano annullato: {reason}");

        return $"Plan aborted. Reason: {reason}. " +
               $"{_currentPlan.CompletedSteps} steps were completed before abort.";
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Logs a message.
    /// </summary>
    private void Log(string message)
    {
        OnLogMessage?.Invoke(message);
    }

    /// <summary>
    /// Gets the list of tools as AIFunction.
    /// </summary>
    public IEnumerable<AIFunction> GetTools()
    {
        // Use AIFunctionFactory to create AIFunctions from methods
        // Syntax: AIFunctionFactory.Create(delegate, name, description)
        // For instance methods we use this.MethodName as delegate
        yield return AIFunctionFactory.Create(CreatePlan, "create_plan");
        yield return AIFunctionFactory.Create(ExecuteNextStep, "execute_next_step");
        yield return AIFunctionFactory.Create(GetPlanStatus, "get_plan_status");
        yield return AIFunctionFactory.Create(MarkStepFailed, "mark_step_failed");
        yield return AIFunctionFactory.Create(AbortPlan, "abort_plan");
    }
}
