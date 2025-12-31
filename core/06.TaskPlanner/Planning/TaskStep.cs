// ============================================================================
// 06. TASK PLANNER
// FILE: TaskStep.cs
// ============================================================================
// This file defines the model for a step in a task plan.
//
// KEY CONCEPTS:
//
// 1. TASK DECOMPOSITION:
//    ┌────────────────────────────────────────────────────────────────────┐
//    │ Objective: "Create a .NET project with unit tests"                 │
//    └────────────────────────────────────────────────────────────────────┘
//                              │
//                              ▼
//    ┌─────────────────────────────────────────────────────────────────────┐
//    │ Step 1: Create project folder                       [✓ Completed]  │
//    │ Step 2: Initialize project with dotnet new          [► In Progress] │
//    │ Step 3: Add test project                            [○ Pending]     │
//    │ Step 4: Write first test                            [○ Pending]     │
//    │ Step 5: Run tests                                   [○ Pending]     │
//    └─────────────────────────────────────────────────────────────────────┘
//
// 2. STEP STATUS:
//    - Pending: not yet started
//    - InProgress: executing
//    - Completed: completed successfully
//    - Failed: failed (with error message)
//    - Skipped: skipped (failed dependency)
// ============================================================================

namespace TaskPlanner.Planning;

/// <summary>
/// Possible states of a task step.
/// </summary>
public enum TaskStepStatus
{
    /// <summary>
    /// Step not yet started.
    /// </summary>
    Pending,

    /// <summary>
    /// Step currently executing.
    /// </summary>
    InProgress,

    /// <summary>
    /// Step completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Step failed during execution.
    /// </summary>
    Failed,

    /// <summary>
    /// Step skipped (e.g.: failed dependency).
    /// </summary>
    Skipped
}

/// <summary>
/// Represents a single atomic step of an execution plan.
///
/// CHARACTERISTICS OF A GOOD STEP:
/// 1. Atomic: a single well-defined action
/// 2. Verifiable: can determine if it is completed
/// 3. Independent: minimal dependencies on other steps
/// 4. Describable: clear what it does and why
/// </summary>
public class TaskStep
{
    /// <summary>
    /// Unique identifier of the step (1-based).
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Short description of the step (action in imperative form).
    /// E.g.: "Create project folder", "Run unit tests"
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Extended description with implementation details.
    /// Contains specific information for execution.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Name of the tool to use to execute this step.
    /// If null, the step is executed via conversation.
    /// E.g.: "CreateDirectory", "RunCommand", "WriteFile"
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Parameters for the tool (if applicable).
    /// Key-value dictionary with necessary parameters.
    /// </summary>
    public Dictionary<string, string>? ToolParameters { get; init; }

    /// <summary>
    /// Current state of the step.
    /// </summary>
    public TaskStepStatus Status { get; set; } = TaskStepStatus.Pending;

    /// <summary>
    /// Result of execution (if completed).
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution start timestamp.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Execution duration.
    /// </summary>
    public TimeSpan? Duration =>
        StartedAt.HasValue && CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt.Value
            : null;

    /// <summary>
    /// Indicates whether the step is in a terminal state.
    /// </summary>
    public bool IsTerminal =>
        Status is TaskStepStatus.Completed or TaskStepStatus.Failed or TaskStepStatus.Skipped;

    /// <summary>
    /// Indicates whether the step was completed successfully.
    /// </summary>
    public bool IsSuccess => Status == TaskStepStatus.Completed;

    /// <summary>
    /// Emoji for the current state.
    /// </summary>
    public string StatusEmoji => Status switch
    {
        TaskStepStatus.Pending => "○",
        TaskStepStatus.InProgress => "►",
        TaskStepStatus.Completed => "✓",
        TaskStepStatus.Failed => "✗",
        TaskStepStatus.Skipped => "⊘",
        _ => "?"
    };

    /// <summary>
    /// Textual representation of the step for display.
    /// </summary>
    public override string ToString()
    {
        var status = StatusEmoji;
        var duration = Duration.HasValue ? $" ({Duration.Value.TotalSeconds:F1}s)" : "";
        return $"[{status}] Step {Id}: {Description}{duration}";
    }

    /// <summary>
    /// Starts the execution of the step.
    /// </summary>
    public void Start()
    {
        Status = TaskStepStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the step successfully.
    /// </summary>
    /// <param name="result">Execution result</param>
    public void Complete(string? result = null)
    {
        Status = TaskStepStatus.Completed;
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the step as failed.
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    public void Fail(string errorMessage)
    {
        Status = TaskStepStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the step as skipped.
    /// </summary>
    /// <param name="reason">Reason why it was skipped</param>
    public void Skip(string reason)
    {
        Status = TaskStepStatus.Skipped;
        Result = reason;
        CompletedAt = DateTime.UtcNow;
    }
}
