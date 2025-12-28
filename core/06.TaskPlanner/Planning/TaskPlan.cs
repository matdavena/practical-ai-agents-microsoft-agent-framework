// ============================================================================
// 06. TASK PLANNER
// FILE: TaskPlan.cs
// ============================================================================
// Questo file definisce il piano completo con tutti gli step.
//
// CICLO DI VITA DEL PIANO:
//
//    ┌─────────────┐
//    │   Created   │ ← Piano creato, nessuno step ancora
//    └──────┬──────┘
//           │ AddSteps()
//           ▼
//    ┌─────────────┐
//    │   Planned   │ ← Step definiti, pronto per esecuzione
//    └──────┬──────┘
//           │ Execute()
//           ▼
//    ┌─────────────┐
//    │  Executing  │ ← Esecuzione in corso
//    └──────┬──────┘
//           │
//     ┌─────┴─────┐
//     ▼           ▼
// ┌─────────┐ ┌─────────┐
// │Completed│ │ Failed  │
// └─────────┘ └─────────┘
// ============================================================================

namespace TaskPlanner.Planning;

/// <summary>
/// Stati del piano di esecuzione.
/// </summary>
public enum TaskPlanStatus
{
    /// <summary>
    /// Piano creato ma senza step.
    /// </summary>
    Created,

    /// <summary>
    /// Step definiti, pronto per esecuzione.
    /// </summary>
    Planned,

    /// <summary>
    /// Esecuzione in corso.
    /// </summary>
    Executing,

    /// <summary>
    /// Tutti gli step completati con successo.
    /// </summary>
    Completed,

    /// <summary>
    /// Almeno uno step è fallito.
    /// </summary>
    Failed,

    /// <summary>
    /// Esecuzione annullata dall'utente.
    /// </summary>
    Cancelled
}

/// <summary>
/// Rappresenta un piano completo di esecuzione.
///
/// Il piano contiene:
/// - L'obiettivo originale dell'utente
/// - La lista degli step da eseguire
/// - Lo stato complessivo del piano
/// - Statistiche di esecuzione
/// </summary>
public class TaskPlan
{
    // ========================================================================
    // PROPRIETÀ DEL PIANO
    // ========================================================================

    /// <summary>
    /// Obiettivo originale richiesto dall'utente.
    /// Es: "Crea un progetto .NET con unit test"
    /// </summary>
    public required string Goal { get; init; }

    /// <summary>
    /// Descrizione del piano generata dall'agente.
    /// Spiega l'approccio generale che verrà seguito.
    /// </summary>
    public string? PlanDescription { get; set; }

    /// <summary>
    /// Lista degli step da eseguire.
    /// </summary>
    public List<TaskStep> Steps { get; init; } = new();

    /// <summary>
    /// Stato corrente del piano.
    /// </summary>
    public TaskPlanStatus Status { get; set; } = TaskPlanStatus.Created;

    /// <summary>
    /// Timestamp di creazione del piano.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp di inizio esecuzione.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp di completamento.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // ========================================================================
    // PROPRIETÀ CALCOLATE
    // ========================================================================

    /// <summary>
    /// Numero totale di step.
    /// </summary>
    public int TotalSteps => Steps.Count;

    /// <summary>
    /// Numero di step completati con successo.
    /// </summary>
    public int CompletedSteps => Steps.Count(s => s.Status == TaskStepStatus.Completed);

    /// <summary>
    /// Numero di step falliti.
    /// </summary>
    public int FailedSteps => Steps.Count(s => s.Status == TaskStepStatus.Failed);

    /// <summary>
    /// Numero di step in attesa.
    /// </summary>
    public int PendingSteps => Steps.Count(s => s.Status == TaskStepStatus.Pending);

    /// <summary>
    /// Percentuale di completamento (0-100).
    /// </summary>
    public int ProgressPercentage =>
        TotalSteps > 0
            ? (int)((CompletedSteps + FailedSteps) * 100.0 / TotalSteps)
            : 0;

    /// <summary>
    /// Step attualmente in esecuzione (se presente).
    /// </summary>
    public TaskStep? CurrentStep =>
        Steps.FirstOrDefault(s => s.Status == TaskStepStatus.InProgress);

    /// <summary>
    /// Prossimo step da eseguire (se presente).
    /// </summary>
    public TaskStep? NextStep =>
        Steps.FirstOrDefault(s => s.Status == TaskStepStatus.Pending);

    /// <summary>
    /// Indica se tutti gli step sono stati completati con successo.
    /// </summary>
    public bool AllStepsCompleted =>
        Steps.Count > 0 && Steps.All(s => s.Status == TaskStepStatus.Completed);

    /// <summary>
    /// Indica se almeno uno step è fallito.
    /// </summary>
    public bool HasFailures =>
        Steps.Any(s => s.Status == TaskStepStatus.Failed);

    /// <summary>
    /// Durata totale dell'esecuzione.
    /// </summary>
    public TimeSpan? Duration =>
        StartedAt.HasValue && CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt.Value
            : StartedAt.HasValue
                ? DateTime.UtcNow - StartedAt.Value
                : null;

    // ========================================================================
    // METODI DI GESTIONE STEP
    // ========================================================================

    /// <summary>
    /// Aggiunge uno step al piano.
    /// </summary>
    public TaskStep AddStep(string description, string? details = null, string? toolName = null)
    {
        var step = new TaskStep
        {
            Id = Steps.Count + 1,
            Description = description,
            Details = details,
            ToolName = toolName
        };

        Steps.Add(step);

        if (Status == TaskPlanStatus.Created)
        {
            Status = TaskPlanStatus.Planned;
        }

        return step;
    }

    /// <summary>
    /// Aggiunge più step al piano.
    /// </summary>
    public void AddSteps(IEnumerable<(string description, string? details)> steps)
    {
        foreach (var (description, details) in steps)
        {
            AddStep(description, details);
        }
    }

    // ========================================================================
    // METODI DI ESECUZIONE
    // ========================================================================

    /// <summary>
    /// Avvia l'esecuzione del piano.
    /// </summary>
    public void StartExecution()
    {
        if (Status != TaskPlanStatus.Planned)
        {
            throw new InvalidOperationException(
                $"Cannot start execution: plan is in state {Status}");
        }

        Status = TaskPlanStatus.Executing;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completa l'esecuzione del piano.
    /// </summary>
    public void CompleteExecution()
    {
        CompletedAt = DateTime.UtcNow;
        Status = HasFailures ? TaskPlanStatus.Failed : TaskPlanStatus.Completed;
    }

    /// <summary>
    /// Annulla l'esecuzione del piano.
    /// </summary>
    public void Cancel()
    {
        Status = TaskPlanStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;

        // Marca tutti gli step pending come skipped
        foreach (var step in Steps.Where(s => s.Status == TaskStepStatus.Pending))
        {
            step.Skip("Execution cancelled");
        }
    }

    // ========================================================================
    // VISUALIZZAZIONE
    // ========================================================================

    /// <summary>
    /// Genera un riepilogo testuale del piano.
    /// </summary>
    public string GetSummary()
    {
        var lines = new List<string>
        {
            $"📋 Piano: {Goal}",
            $"   Stato: {Status}",
            $"   Progresso: {CompletedSteps}/{TotalSteps} step ({ProgressPercentage}%)"
        };

        if (Duration.HasValue)
        {
            lines.Add($"   Durata: {Duration.Value.TotalSeconds:F1}s");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Genera la lista degli step con stato.
    /// </summary>
    public string GetStepList()
    {
        if (Steps.Count == 0)
        {
            return "   (nessuno step definito)";
        }

        return string.Join(Environment.NewLine, Steps.Select(s => $"   {s}"));
    }

    /// <summary>
    /// Genera il report completo del piano.
    /// </summary>
    public string GetFullReport()
    {
        return $"{GetSummary()}{Environment.NewLine}{Environment.NewLine}Step:{Environment.NewLine}{GetStepList()}";
    }
}
