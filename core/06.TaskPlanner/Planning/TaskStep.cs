// ============================================================================
// 06. TASK PLANNER
// FILE: TaskStep.cs
// ============================================================================
// Questo file definisce il modello per uno step di un piano di task.
//
// CONCETTI CHIAVE:
//
// 1. DECOMPOSIZIONE DEL TASK:
//    ┌────────────────────────────────────────────────────────────────────┐
//    │ Obiettivo: "Crea un progetto .NET con unit test"                   │
//    └────────────────────────────────────────────────────────────────────┘
//                              │
//                              ▼
//    ┌─────────────────────────────────────────────────────────────────────┐
//    │ Step 1: Creare cartella progetto                    [✓ Completed]  │
//    │ Step 2: Inizializzare progetto con dotnet new       [► In Progress] │
//    │ Step 3: Aggiungere progetto test                    [○ Pending]     │
//    │ Step 4: Scrivere primo test                         [○ Pending]     │
//    │ Step 5: Eseguire test                               [○ Pending]     │
//    └─────────────────────────────────────────────────────────────────────┘
//
// 2. STATO DELLO STEP:
//    - Pending: non ancora iniziato
//    - InProgress: in esecuzione
//    - Completed: completato con successo
//    - Failed: fallito (con messaggio di errore)
//    - Skipped: saltato (dipendenza fallita)
// ============================================================================

namespace TaskPlanner.Planning;

/// <summary>
/// Stati possibili di uno step del task.
/// </summary>
public enum TaskStepStatus
{
    /// <summary>
    /// Step non ancora iniziato.
    /// </summary>
    Pending,

    /// <summary>
    /// Step attualmente in esecuzione.
    /// </summary>
    InProgress,

    /// <summary>
    /// Step completato con successo.
    /// </summary>
    Completed,

    /// <summary>
    /// Step fallito durante l'esecuzione.
    /// </summary>
    Failed,

    /// <summary>
    /// Step saltato (es: dipendenza fallita).
    /// </summary>
    Skipped
}

/// <summary>
/// Rappresenta un singolo step atomico di un piano di esecuzione.
///
/// CARATTERISTICHE DI UN BUON STEP:
/// 1. Atomico: una singola azione ben definita
/// 2. Verificabile: si può determinare se è completato
/// 3. Indipendente: minime dipendenze dagli altri step
/// 4. Descrivibile: chiaro cosa fa e perché
/// </summary>
public class TaskStep
{
    /// <summary>
    /// Identificatore univoco dello step (1-based).
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Descrizione breve dello step (azione in forma imperativa).
    /// Es: "Creare cartella progetto", "Eseguire unit test"
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Descrizione estesa con dettagli sull'implementazione.
    /// Contiene informazioni specifiche per l'esecuzione.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Nome del tool da usare per eseguire questo step.
    /// Se null, lo step viene eseguito tramite conversazione.
    /// Es: "CreateDirectory", "RunCommand", "WriteFile"
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Parametri per il tool (se applicabile).
    /// Dizionario chiave-valore con i parametri necessari.
    /// </summary>
    public Dictionary<string, string>? ToolParameters { get; init; }

    /// <summary>
    /// Stato corrente dello step.
    /// </summary>
    public TaskStepStatus Status { get; set; } = TaskStepStatus.Pending;

    /// <summary>
    /// Risultato dell'esecuzione (se completato).
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Messaggio di errore (se fallito).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp di inizio esecuzione.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp di completamento.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Durata dell'esecuzione.
    /// </summary>
    public TimeSpan? Duration =>
        StartedAt.HasValue && CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt.Value
            : null;

    /// <summary>
    /// Indica se lo step è in uno stato terminale.
    /// </summary>
    public bool IsTerminal =>
        Status is TaskStepStatus.Completed or TaskStepStatus.Failed or TaskStepStatus.Skipped;

    /// <summary>
    /// Indica se lo step è stato completato con successo.
    /// </summary>
    public bool IsSuccess => Status == TaskStepStatus.Completed;

    /// <summary>
    /// Emoji per lo stato corrente.
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
    /// Rappresentazione testuale dello step per visualizzazione.
    /// </summary>
    public override string ToString()
    {
        var status = StatusEmoji;
        var duration = Duration.HasValue ? $" ({Duration.Value.TotalSeconds:F1}s)" : "";
        return $"[{status}] Step {Id}: {Description}{duration}";
    }

    /// <summary>
    /// Avvia l'esecuzione dello step.
    /// </summary>
    public void Start()
    {
        Status = TaskStepStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completa lo step con successo.
    /// </summary>
    /// <param name="result">Risultato dell'esecuzione</param>
    public void Complete(string? result = null)
    {
        Status = TaskStepStatus.Completed;
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Segna lo step come fallito.
    /// </summary>
    /// <param name="errorMessage">Messaggio di errore</param>
    public void Fail(string errorMessage)
    {
        Status = TaskStepStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Segna lo step come saltato.
    /// </summary>
    /// <param name="reason">Motivo per cui è stato saltato</param>
    public void Skip(string reason)
    {
        Status = TaskStepStatus.Skipped;
        Result = reason;
        CompletedAt = DateTime.UtcNow;
    }
}
