// ============================================================================
// 07. DEV TEAM - MULTI-AGENT
// FILE: TeamMember.cs
// ============================================================================
// Questo file definisce i ruoli e le configurazioni degli agenti del team.
//
// ARCHITETTURA MULTI-AGENTE:
//
//    ┌─────────────────────────────────────────────────────────────────────┐
//    │                         TEAM LEAD                                    │
//    │    Coordina il team, assegna task, aggrega risultati                │
//    └──────────────────────────┬──────────────────────────────────────────┘
//                               │
//         ┌─────────────────────┼─────────────────────┐
//         │                     │                     │
//         ▼                     ▼                     ▼
//    ┌──────────┐         ┌──────────┐         ┌──────────┐
//    │ ARCHITECT│         │ DEVELOPER│         │ REVIEWER │
//    │          │         │          │         │          │
//    │ Progetta │         │ Scrive   │         │ Revisiona│
//    │ soluzioni│         │ codice   │         │ codice   │
//    └──────────┘         └──────────┘         └──────────┘
//
// VANTAGGI MULTI-AGENTE:
// - Specializzazione: ogni agente è esperto nel suo dominio
// - Modularità: facile aggiungere/rimuovere competenze
// - Parallelismo: più agenti possono lavorare contemporaneamente
// - Qualità: revisione incrociata migliora i risultati
// ============================================================================

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace DevTeam.MultiAgent.Agents;

/// <summary>
/// Ruoli disponibili nel team di sviluppo.
/// </summary>
public enum TeamRole
{
    /// <summary>
    /// Team Lead: coordina il team e gestisce il workflow.
    /// </summary>
    TeamLead,

    /// <summary>
    /// Architect: progetta l'architettura e le soluzioni tecniche.
    /// </summary>
    Architect,

    /// <summary>
    /// Developer: implementa il codice seguendo le specifiche.
    /// </summary>
    Developer,

    /// <summary>
    /// Reviewer: revisiona il codice e suggerisce miglioramenti.
    /// </summary>
    Reviewer
}

/// <summary>
/// Rappresenta un membro del team di sviluppo AI.
///
/// Ogni membro ha:
/// - Un ruolo specifico (Architect, Developer, Reviewer, TeamLead)
/// - Un prompt di sistema personalizzato per il ruolo
/// - Un agente ChatClientAgent configurato
/// - Un thread dedicato per le conversazioni
/// </summary>
public class TeamMember
{
    // ========================================================================
    // PROPRIETÀ
    // ========================================================================

    /// <summary>
    /// Ruolo del membro nel team.
    /// </summary>
    public TeamRole Role { get; }

    /// <summary>
    /// Nome visualizzato del membro.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Emoji per identificare visivamente il membro.
    /// </summary>
    public string Emoji { get; }

    /// <summary>
    /// Agente AI sottostante.
    /// </summary>
    public ChatClientAgent Agent { get; }

    /// <summary>
    /// Thread di conversazione dedicato.
    /// </summary>
    public AgentThread Thread { get; private set; }

    /// <summary>
    /// System prompt che definisce il comportamento dell'agente.
    /// </summary>
    public string SystemPrompt { get; }

    /// <summary>
    /// Indica se è il primo messaggio (per iniettare il system prompt).
    /// </summary>
    private bool _isFirstMessage = true;

    // ========================================================================
    // COSTRUTTORE
    // ========================================================================

    /// <summary>
    /// Crea un nuovo membro del team.
    /// </summary>
    private TeamMember(
        TeamRole role,
        string name,
        string emoji,
        string systemPrompt,
        ChatClientAgent agent)
    {
        Role = role;
        Name = name;
        Emoji = emoji;
        SystemPrompt = systemPrompt;
        Agent = agent;
        Thread = agent.GetNewThread();
    }

    // ========================================================================
    // FACTORY METHODS
    // ========================================================================

    /// <summary>
    /// Crea un membro del team con il ruolo specificato.
    /// </summary>
    public static TeamMember Create(TeamRole role, ChatClient chatClient)
    {
        var (name, emoji, systemPrompt) = GetRoleConfiguration(role);

        var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions
            {
                Temperature = GetTemperatureForRole(role)
            }
        });

        return new TeamMember(role, name, emoji, systemPrompt, agent);
    }

    /// <summary>
    /// Ottiene la configurazione per un ruolo specifico.
    /// </summary>
    private static (string name, string emoji, string systemPrompt) GetRoleConfiguration(TeamRole role)
    {
        return role switch
        {
            TeamRole.TeamLead => (
                "TeamLead",
                "👔",
                """
                Sei il Team Lead di un team di sviluppo software.

                IL TUO RUOLO:
                - Coordini il lavoro del team
                - Analizzi le richieste e le dividi in task
                - Assegni i task ai membri appropriati
                - Aggreghi e presenti i risultati finali

                COME LAVORI:
                1. Ricevi una richiesta dall'utente
                2. La analizzi e identifichi cosa serve
                3. Decidi quali membri del team coinvolgere
                4. Presenti un piano d'azione chiaro

                Rispondi sempre in modo conciso e strutturato.
                Usa elenchi puntati quando appropriato.
                """
            ),

            TeamRole.Architect => (
                "Architect",
                "🏗️",
                """
                Sei un Software Architect esperto.

                IL TUO RUOLO:
                - Progetti architetture software scalabili
                - Definisci pattern e best practices
                - Scegli le tecnologie appropriate
                - Crei diagrammi e documentazione tecnica

                COME LAVORI:
                1. Analizzi i requisiti funzionali e non funzionali
                2. Proponi un'architettura con pro e contro
                3. Definisci i componenti principali
                4. Specifichi le interfacce tra componenti

                Rispondi con:
                - Descrizione dell'architettura
                - Componenti principali
                - Pattern utilizzati
                - Considerazioni su scalabilità/manutenibilità
                """
            ),

            TeamRole.Developer => (
                "Developer",
                "💻",
                """
                Sei uno sviluppatore senior esperto in C# e .NET.

                IL TUO RUOLO:
                - Scrivi codice pulito e manutenibile
                - Implementi le specifiche dell'architetto
                - Segui i principi SOLID e clean code
                - Aggiungi commenti dove necessario

                COME LAVORI:
                1. Leggi le specifiche ricevute
                2. Scrivi il codice richiesto
                3. Includi gestione errori appropriata
                4. Aggiungi commenti per la documentazione

                REGOLE:
                - Usa sempre C# moderno (.NET 8+)
                - Preferisci async/await per operazioni I/O
                - Usa dependency injection
                - Scrivi codice testabile
                """
            ),

            TeamRole.Reviewer => (
                "Reviewer",
                "🔍",
                """
                Sei un Code Reviewer esperto.

                IL TUO RUOLO:
                - Revisioni il codice per qualità e correttezza
                - Identifichi bug potenziali
                - Suggerisci miglioramenti
                - Verifichi l'aderenza alle best practices

                COME LAVORI:
                1. Analizzi il codice ricevuto
                2. Verifichi la logica e la correttezza
                3. Controlli naming, formattazione, pattern
                4. Fornisci feedback costruttivo

                FORMATO REVIEW:
                ✅ Punti positivi: cosa è fatto bene
                ⚠️ Suggerimenti: miglioramenti consigliati
                ❌ Problemi: bug o errori da correggere
                📊 Voto: da 1 a 10 con motivazione
                """
            ),

            _ => throw new ArgumentException($"Unknown role: {role}")
        };
    }

    /// <summary>
    /// Ottiene la temperatura appropriata per il ruolo.
    /// </summary>
    private static float GetTemperatureForRole(TeamRole role)
    {
        return role switch
        {
            TeamRole.TeamLead => 0.5f,   // Bilanciato
            TeamRole.Architect => 0.7f,  // Più creativo
            TeamRole.Developer => 0.3f,  // Più deterministico
            TeamRole.Reviewer => 0.2f,   // Molto preciso
            _ => 0.5f
        };
    }

    // ========================================================================
    // METODI DI INTERAZIONE
    // ========================================================================

    /// <summary>
    /// Invia un messaggio all'agente e ottiene la risposta.
    /// </summary>
    /// <param name="message">Messaggio da inviare</param>
    /// <param name="includeSystemPrompt">Se includere il system prompt</param>
    /// <returns>Risposta dell'agente</returns>
    public async Task<string> AskAsync(string message, bool includeSystemPrompt = true)
    {
        var prompt = includeSystemPrompt && _isFirstMessage
            ? $"[System: {SystemPrompt}]\n\n{message}"
            : message;

        _isFirstMessage = false;

        var response = await Agent.RunAsync(prompt, Thread);
        return response.Text ?? string.Empty;
    }

    /// <summary>
    /// Invia un messaggio con streaming della risposta.
    /// </summary>
    public async IAsyncEnumerable<string> AskStreamingAsync(string message, bool includeSystemPrompt = true)
    {
        var prompt = includeSystemPrompt && _isFirstMessage
            ? $"[System: {SystemPrompt}]\n\n{message}"
            : message;

        _isFirstMessage = false;

        await foreach (var update in Agent.RunStreamingAsync(prompt, Thread))
        {
            yield return update.ToString();
        }
    }

    /// <summary>
    /// Resetta il thread di conversazione.
    /// </summary>
    public void ResetThread()
    {
        Thread = Agent.GetNewThread();
        _isFirstMessage = true;
    }

    /// <summary>
    /// Rappresentazione testuale del membro.
    /// </summary>
    public override string ToString() => $"{Emoji} {Name}";
}
