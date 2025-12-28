// ============================================================================
// 07. DEV TEAM - MULTI-AGENT
// FILE: TeamOrchestrator.cs
// ============================================================================
// Questo file implementa l'orchestratore che coordina il team di agenti.
//
// PATTERN DI ORCHESTRAZIONE SUPPORTATI:
//
// 1. SEQUENZIALE (Pipeline):
//    ┌──────────┐    ┌──────────┐    ┌──────────┐
//    │ Architect│ ─► │ Developer│ ─► │ Reviewer │
//    └──────────┘    └──────────┘    └──────────┘
//    L'output di un agente diventa input del successivo.
//
// 2. PARALLELO (Fan-out / Fan-in):
//    ┌──────────┐
//    │ Architect│ ─┐
//    └──────────┘  │
//                  ├─► Aggregazione risultati
//    ┌──────────┐  │
//    │ Developer│ ─┘
//    └──────────┘
//    Più agenti lavorano in parallelo, poi si aggregano i risultati.
//
// 3. ROUTING (Condizionale):
//    ┌──────────────┐
//    │   Analisi    │
//    └──────┬───────┘
//           │
//     ┌─────┴─────┐
//     ▼           ▼
//  [Design?]  [Code?]
//     │           │
//     ▼           ▼
//  Architect   Developer
//
//    Si sceglie quale agente coinvolgere in base al contenuto.
// ============================================================================

using DevTeam.MultiAgent.Agents;
using OpenAI.Chat;

namespace DevTeam.MultiAgent.Orchestration;

/// <summary>
/// Tipo di workflow per l'orchestrazione.
/// </summary>
public enum WorkflowType
{
    /// <summary>
    /// Gli agenti lavorano in sequenza (pipeline).
    /// </summary>
    Sequential,

    /// <summary>
    /// Gli agenti lavorano in parallelo.
    /// </summary>
    Parallel,

    /// <summary>
    /// Un agente viene scelto in base al contenuto.
    /// </summary>
    Routed
}

/// <summary>
/// Risultato di un'operazione del team.
/// </summary>
public class TeamResult
{
    /// <summary>
    /// Membro che ha prodotto il risultato.
    /// </summary>
    public required TeamMember Member { get; init; }

    /// <summary>
    /// Risposta dell'agente.
    /// </summary>
    public required string Response { get; init; }

    /// <summary>
    /// Tempo di esecuzione.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Orchestratore per il team di agenti.
///
/// Gestisce:
/// - Creazione e configurazione del team
/// - Esecuzione di workflow multi-agente
/// - Aggregazione dei risultati
/// - Logging delle operazioni
/// </summary>
public class TeamOrchestrator
{
    // ========================================================================
    // MEMBRI DEL TEAM
    // ========================================================================

    private readonly Dictionary<TeamRole, TeamMember> _team = new();
    private readonly ChatClient _chatClient;

    /// <summary>
    /// Evento per logging delle operazioni.
    /// </summary>
    public event Action<string>? OnLog;

    // ========================================================================
    // COSTRUTTORE
    // ========================================================================

    /// <summary>
    /// Crea un nuovo orchestratore.
    /// </summary>
    /// <param name="chatClient">Client OpenAI da usare per gli agenti</param>
    public TeamOrchestrator(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    // ========================================================================
    // GESTIONE TEAM
    // ========================================================================

    /// <summary>
    /// Aggiunge un membro al team.
    /// </summary>
    public TeamMember AddMember(TeamRole role)
    {
        if (_team.ContainsKey(role))
        {
            Log($"⚠️ Membro {role} già presente, viene sostituito");
        }

        var member = TeamMember.Create(role, _chatClient);
        _team[role] = member;

        Log($"✅ {member} aggiunto al team");
        return member;
    }

    /// <summary>
    /// Ottiene un membro del team.
    /// </summary>
    public TeamMember? GetMember(TeamRole role)
    {
        return _team.GetValueOrDefault(role);
    }

    /// <summary>
    /// Ottiene tutti i membri del team.
    /// </summary>
    public IEnumerable<TeamMember> GetAllMembers() => _team.Values;

    /// <summary>
    /// Inizializza il team completo.
    /// </summary>
    public void InitializeFullTeam()
    {
        AddMember(TeamRole.TeamLead);
        AddMember(TeamRole.Architect);
        AddMember(TeamRole.Developer);
        AddMember(TeamRole.Reviewer);
    }

    // ========================================================================
    // WORKFLOW SEQUENZIALE
    // ========================================================================

    /// <summary>
    /// Esegue un workflow sequenziale (pipeline).
    ///
    /// Ogni agente riceve l'output del precedente come input.
    /// Utile per: Design → Implement → Review
    /// </summary>
    /// <param name="initialPrompt">Prompt iniziale</param>
    /// <param name="roles">Sequenza di ruoli da coinvolgere</param>
    /// <returns>Lista dei risultati in ordine</returns>
    public async Task<List<TeamResult>> RunSequentialAsync(
        string initialPrompt,
        params TeamRole[] roles)
    {
        Log($"🔄 Avvio workflow SEQUENZIALE con {roles.Length} step");

        var results = new List<TeamResult>();
        var currentInput = initialPrompt;

        foreach (var role in roles)
        {
            var member = GetMember(role);
            if (member == null)
            {
                Log($"❌ Membro {role} non trovato, skip");
                continue;
            }

            Log($"   ► {member} sta elaborando...");

            var startTime = DateTime.UtcNow;

            // Costruisci il prompt includendo il contesto precedente
            var prompt = results.Count > 0
                ? $"Contesto dal passaggio precedente:\n{currentInput}\n\nOra è il tuo turno. Procedi con il tuo compito."
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
            currentInput = response; // L'output diventa input per il prossimo

            Log($"   ✓ {member} completato in {duration.TotalSeconds:F1}s");
        }

        Log($"✅ Workflow sequenziale completato: {results.Count} step");
        return results;
    }

    // ========================================================================
    // WORKFLOW PARALLELO
    // ========================================================================

    /// <summary>
    /// Esegue un workflow parallelo (fan-out).
    ///
    /// Tutti gli agenti ricevono lo stesso input e lavorano in parallelo.
    /// Utile per: ottenere prospettive diverse sullo stesso problema.
    /// </summary>
    /// <param name="prompt">Prompt da inviare a tutti</param>
    /// <param name="roles">Ruoli da coinvolgere in parallelo</param>
    /// <returns>Lista dei risultati (ordine non garantito)</returns>
    public async Task<List<TeamResult>> RunParallelAsync(
        string prompt,
        params TeamRole[] roles)
    {
        Log($"⚡ Avvio workflow PARALLELO con {roles.Length} agenti");

        var tasks = new List<Task<TeamResult>>();

        foreach (var role in roles)
        {
            var member = GetMember(role);
            if (member == null)
            {
                Log($"❌ Membro {role} non trovato, skip");
                continue;
            }

            Log($"   ► {member} avviato in parallelo");

            // Crea un task per ogni agente
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

        // Attendi tutti i task
        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            Log($"   ✓ {result.Member} completato in {result.Duration.TotalSeconds:F1}s");
        }

        Log($"✅ Workflow parallelo completato: {results.Length} risultati");
        return results.ToList();
    }

    // ========================================================================
    // WORKFLOW CON ROUTING
    // ========================================================================

    /// <summary>
    /// Esegue un workflow con routing basato sul contenuto.
    ///
    /// Il TeamLead analizza la richiesta e decide quale agente coinvolgere.
    /// </summary>
    /// <param name="request">Richiesta dell'utente</param>
    /// <returns>Risultato con l'agente selezionato</returns>
    public async Task<TeamResult> RunRoutedAsync(string request)
    {
        Log($"🔀 Avvio workflow ROUTED");

        var teamLead = GetMember(TeamRole.TeamLead);
        if (teamLead == null)
        {
            throw new InvalidOperationException("TeamLead non trovato nel team");
        }

        // Step 1: TeamLead analizza e decide
        Log($"   ► {teamLead} sta analizzando la richiesta...");

        var analysisPrompt = $"""
            Analizza questa richiesta e decidi quale membro del team dovrebbe gestirla.

            RICHIESTA: {request}

            MEMBRI DISPONIBILI:
            - ARCHITECT: per questioni di design, architettura, pattern, decisioni tecniche
            - DEVELOPER: per scrivere codice, implementare funzionalità, correggere bug
            - REVIEWER: per revisionare codice esistente, trovare problemi, suggerire miglioramenti

            Rispondi SOLO con il nome del membro più appropriato (ARCHITECT, DEVELOPER, o REVIEWER)
            seguito da una breve spiegazione del perché.

            Formato: NOME_MEMBRO | Motivazione
            """;

        var analysisResponse = await teamLead.AskAsync(analysisPrompt);
        Log($"   ✓ Analisi: {analysisResponse}");

        // Step 2: Determina il ruolo selezionato
        var selectedRole = DetermineRoleFromResponse(analysisResponse);
        Log($"   → Routing verso: {selectedRole}");

        // Step 3: Invia la richiesta all'agente selezionato
        var selectedMember = GetMember(selectedRole);
        if (selectedMember == null)
        {
            // Fallback al developer
            selectedMember = GetMember(TeamRole.Developer) ?? teamLead;
        }

        Log($"   ► {selectedMember} sta elaborando...");

        var startTime = DateTime.UtcNow;
        var response = await selectedMember.AskAsync(request);
        var duration = DateTime.UtcNow - startTime;

        Log($"   ✓ {selectedMember} completato in {duration.TotalSeconds:F1}s");

        return new TeamResult
        {
            Member = selectedMember,
            Response = response,
            Duration = duration
        };
    }

    /// <summary>
    /// Determina il ruolo dalla risposta del TeamLead.
    /// </summary>
    private TeamRole DetermineRoleFromResponse(string response)
    {
        var upper = response.ToUpperInvariant();

        if (upper.Contains("ARCHITECT"))
            return TeamRole.Architect;

        if (upper.Contains("REVIEWER") || upper.Contains("REVIEW"))
            return TeamRole.Reviewer;

        // Default a Developer
        return TeamRole.Developer;
    }

    // ========================================================================
    // WORKFLOW COMPLETO: DESIGN-IMPLEMENT-REVIEW
    // ========================================================================

    /// <summary>
    /// Esegue il workflow completo di sviluppo.
    ///
    /// 1. Architect progetta la soluzione
    /// 2. Developer implementa il codice
    /// 3. Reviewer revisiona il risultato
    /// </summary>
    /// <param name="requirement">Requisito da implementare</param>
    /// <returns>Risultati di tutti i passaggi</returns>
    public async Task<List<TeamResult>> RunFullDevelopmentCycleAsync(string requirement)
    {
        Log($"🚀 Avvio CICLO DI SVILUPPO COMPLETO");
        Log($"   Requisito: {requirement}");

        // Step 1: Architect
        var architect = GetMember(TeamRole.Architect);
        if (architect == null) throw new InvalidOperationException("Architect mancante");

        Log($"\n📐 FASE 1: DESIGN");
        Log($"   ► {architect} sta progettando...");

        var designPrompt = $"""
            Progetta una soluzione per questo requisito:

            {requirement}

            Fornisci:
            1. Architettura proposta
            2. Componenti principali
            3. Interfacce tra componenti
            4. Considerazioni tecniche
            """;

        var designStart = DateTime.UtcNow;
        var design = await architect.AskAsync(designPrompt);
        var designDuration = DateTime.UtcNow - designStart;

        Log($"   ✓ Design completato in {designDuration.TotalSeconds:F1}s");

        // Step 2: Developer
        var developer = GetMember(TeamRole.Developer);
        if (developer == null) throw new InvalidOperationException("Developer mancante");

        Log($"\n💻 FASE 2: IMPLEMENTAZIONE");
        Log($"   ► {developer} sta implementando...");

        var implementPrompt = $"""
            Implementa il codice seguendo questo design:

            === DESIGN ===
            {design}
            === FINE DESIGN ===

            Scrivi il codice C# completo con:
            - Classi e interfacce necessarie
            - Implementazione della logica
            - Gestione errori appropriata
            - Commenti dove utile
            """;

        var implStart = DateTime.UtcNow;
        var implementation = await developer.AskAsync(implementPrompt);
        var implDuration = DateTime.UtcNow - implStart;

        Log($"   ✓ Implementazione completata in {implDuration.TotalSeconds:F1}s");

        // Step 3: Reviewer
        var reviewer = GetMember(TeamRole.Reviewer);
        if (reviewer == null) throw new InvalidOperationException("Reviewer mancante");

        Log($"\n🔍 FASE 3: CODE REVIEW");
        Log($"   ► {reviewer} sta revisionando...");

        var reviewPrompt = $"""
            Revisiona questo codice:

            === CODICE ===
            {implementation}
            === FINE CODICE ===

            Fornisci una review completa con:
            ✅ Punti positivi
            ⚠️ Suggerimenti di miglioramento
            ❌ Problemi da correggere
            📊 Voto complessivo (1-10)
            """;

        var reviewStart = DateTime.UtcNow;
        var review = await reviewer.AskAsync(reviewPrompt);
        var reviewDuration = DateTime.UtcNow - reviewStart;

        Log($"   ✓ Review completata in {reviewDuration.TotalSeconds:F1}s");

        Log($"\n✅ CICLO COMPLETO TERMINATO");

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
