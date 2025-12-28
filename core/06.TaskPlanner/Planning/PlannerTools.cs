// ============================================================================
// 06. TASK PLANNER
// FILE: PlannerTools.cs
// ============================================================================
// Questo file definisce i tools che l'agente usa per creare ed eseguire piani.
//
// PATTERN PLAN-EXECUTE:
//
// 1. FASE PLANNING:
//    L'agente analizza l'obiettivo e crea un piano usando create_plan
//    ┌────────────────┐
//    │ "Crea progetto │     ┌──────────────┐
//    │  con test"     │ ──► │ create_plan  │ ──► Piano con N step
//    └────────────────┘     └──────────────┘
//
// 2. FASE EXECUTE:
//    L'agente esegue ogni step usando execute_step
//    ┌────────────────┐     ┌──────────────┐
//    │ Step 1         │ ──► │ execute_step │ ──► Risultato
//    └────────────────┘     └──────────────┘
//          │
//          ▼
//    ┌────────────────┐     ┌──────────────┐
//    │ Step 2         │ ──► │ execute_step │ ──► Risultato
//    └────────────────┘     └──────────────┘
//          │
//          ▼
//         ...
//
// VANTAGGI:
// - L'LLM decide COSA fare (planning)
// - I tools eseguono le azioni (execution)
// - Tracciabilità completa degli step
// - Possibilità di retry su errori
// ============================================================================

using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace TaskPlanner.Planning;

/// <summary>
/// Tools per la pianificazione e l'esecuzione di task.
///
/// Questi tools vengono esposti all'agente tramite AIFunctionFactory.
/// L'agente li chiama autonomamente per:
/// 1. Creare un piano di esecuzione
/// 2. Eseguire singoli step del piano
/// 3. Verificare lo stato del piano
/// </summary>
public class PlannerTools
{
    // ========================================================================
    // STATO CONDIVISO
    // ========================================================================

    /// <summary>
    /// Piano corrente in esecuzione.
    /// Condiviso tra tutti i tools per mantenere lo stato.
    /// </summary>
    private TaskPlan? _currentPlan;

    /// <summary>
    /// Callback per notificare eventi durante l'esecuzione.
    /// </summary>
    public event Action<string>? OnLogMessage;

    /// <summary>
    /// Piano corrente (sola lettura).
    /// </summary>
    public TaskPlan? CurrentPlan => _currentPlan;

    // ========================================================================
    // TOOL: CREATE_PLAN
    // ========================================================================

    /// <summary>
    /// Crea un piano di esecuzione per raggiungere un obiettivo.
    ///
    /// L'agente deve chiamare questo tool per prima cosa, passando:
    /// - L'obiettivo da raggiungere
    /// - La descrizione del piano
    /// - La lista degli step da eseguire
    ///
    /// Ogni step deve essere atomico e verificabile.
    /// </summary>
    /// <param name="goal">Obiettivo da raggiungere</param>
    /// <param name="planDescription">Descrizione generale del piano</param>
    /// <param name="steps">Lista degli step (separati da |)</param>
    /// <returns>Conferma della creazione del piano</returns>
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

        // Crea il piano
        _currentPlan = new TaskPlan
        {
            Goal = goal,
            PlanDescription = planDescription
        };

        // Parsa e aggiungi gli step
        var stepDescriptions = steps
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var stepDescription in stepDescriptions)
        {
            _currentPlan.AddStep(stepDescription);
        }

        Log($"   → {_currentPlan.TotalSteps} step creati");

        // Mostra gli step
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
    /// Esegue il prossimo step in attesa del piano.
    ///
    /// L'agente deve chiamare questo tool ripetutamente per eseguire
    /// tutti gli step del piano, uno alla volta.
    ///
    /// Per ogni step, l'agente deve:
    /// 1. Chiamare execute_next_step con il risultato del lavoro svolto
    /// 2. Verificare se ci sono altri step (guardando hasMoreSteps)
    /// 3. Continuare fino al completamento
    /// </summary>
    /// <param name="stepResult">Descrizione del lavoro svolto per questo step</param>
    /// <returns>Stato dell'esecuzione e informazioni sul prossimo step</returns>
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

        // Se lo step è pending, avvialo
        if (currentStep.Status == TaskStepStatus.Pending)
        {
            if (_currentPlan.Status == TaskPlanStatus.Planned)
            {
                _currentPlan.StartExecution();
            }
            currentStep.Start();
            Log($"   ► Avviato Step {currentStep.Id}: {currentStep.Description}");
        }

        // Completa lo step corrente
        currentStep.Complete(stepResult);
        Log($"   ✓ Completato Step {currentStep.Id} ({currentStep.Duration?.TotalSeconds:F1}s)");

        // Verifica se ci sono altri step
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

        // Avvia il prossimo step
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
    /// Ottiene lo stato corrente del piano.
    ///
    /// Utile per verificare il progresso o per riprendere
    /// l'esecuzione dopo un'interruzione.
    /// </summary>
    /// <returns>Stato dettagliato del piano</returns>
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
    /// Segna lo step corrente come fallito.
    ///
    /// Usare quando lo step non può essere completato.
    /// L'agente deve decidere se continuare o abortire il piano.
    /// </summary>
    /// <param name="errorMessage">Messaggio di errore</param>
    /// <returns>Stato dopo il fallimento</returns>
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
    /// Annulla completamente il piano.
    ///
    /// Tutti gli step non completati vengono marcati come skipped.
    /// </summary>
    /// <param name="reason">Motivo dell'annullamento</param>
    /// <returns>Conferma dell'annullamento</returns>
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
    /// Log di un messaggio.
    /// </summary>
    private void Log(string message)
    {
        OnLogMessage?.Invoke(message);
    }

    /// <summary>
    /// Ottiene la lista dei tools come AIFunction.
    /// </summary>
    public IEnumerable<AIFunction> GetTools()
    {
        // Usa AIFunctionFactory per creare gli AIFunction dai metodi
        // Sintassi: AIFunctionFactory.Create(delegate, name, description)
        // Per metodi di istanza usiamo this.MethodName come delegate
        yield return AIFunctionFactory.Create(CreatePlan, "create_plan");
        yield return AIFunctionFactory.Create(ExecuteNextStep, "execute_next_step");
        yield return AIFunctionFactory.Create(GetPlanStatus, "get_plan_status");
        yield return AIFunctionFactory.Create(MarkStepFailed, "mark_step_failed");
        yield return AIFunctionFactory.Create(AbortPlan, "abort_plan");
    }
}
