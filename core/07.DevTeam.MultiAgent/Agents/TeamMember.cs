// ============================================================================
// 07. DEV TEAM - MULTI-AGENT
// FILE: TeamMember.cs
// ============================================================================
// This file defines the roles and configurations of team agents.
//
// MULTI-AGENT ARCHITECTURE:
//
//    ┌─────────────────────────────────────────────────────────────────────┐
//    │                         TEAM LEAD                                    │
//    │    Coordinates the team, assigns tasks, aggregates results          │
//    └──────────────────────────┬──────────────────────────────────────────┘
//                               │
//         ┌─────────────────────┼─────────────────────┐
//         │                     │                     │
//         ▼                     ▼                     ▼
//    ┌──────────┐         ┌──────────┐         ┌──────────┐
//    │ ARCHITECT│         │ DEVELOPER│         │ REVIEWER │
//    │          │         │          │         │          │
//    │ Designs  │         │ Writes   │         │ Reviews  │
//    │ solutions│         │ code     │         │ code     │
//    └──────────┘         └──────────┘         └──────────┘
//
// MULTI-AGENT ADVANTAGES:
// - Specialization: each agent is expert in its domain
// - Modularity: easy to add/remove competencies
// - Parallelism: multiple agents can work concurrently
// - Quality: cross review improves results
// ============================================================================

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace DevTeam.MultiAgent.Agents;

/// <summary>
/// Available roles in the development team.
/// </summary>
public enum TeamRole
{
    /// <summary>
    /// Team Lead: coordinates the team and manages the workflow.
    /// </summary>
    TeamLead,

    /// <summary>
    /// Architect: designs the architecture and technical solutions.
    /// </summary>
    Architect,

    /// <summary>
    /// Developer: implements the code following specifications.
    /// </summary>
    Developer,

    /// <summary>
    /// Reviewer: reviews the code and suggests improvements.
    /// </summary>
    Reviewer
}

/// <summary>
/// Represents a member of the AI development team.
///
/// Each member has:
/// - A specific role (Architect, Developer, Reviewer, TeamLead)
/// - A customized system prompt for the role
/// - A configured ChatClientAgent
/// - A dedicated thread for conversations
/// </summary>
public class TeamMember
{
    // ========================================================================
    // PROPERTIES
    // ========================================================================

    /// <summary>
    /// Role of the member in the team.
    /// </summary>
    public TeamRole Role { get; }

    /// <summary>
    /// Display name of the member.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Emoji to visually identify the member.
    /// </summary>
    public string Emoji { get; }

    /// <summary>
    /// Underlying AI agent.
    /// </summary>
    public ChatClientAgent Agent { get; }

    /// <summary>
    /// Dedicated conversation thread.
    /// </summary>
    public AgentThread Thread { get; private set; }

    /// <summary>
    /// System prompt that defines the agent's behavior.
    /// </summary>
    public string SystemPrompt { get; }

    /// <summary>
    /// Indicates if it's the first message (to inject the system prompt).
    /// </summary>
    private bool _isFirstMessage = true;

    // ========================================================================
    // CONSTRUCTOR
    // ========================================================================

    /// <summary>
    /// Creates a new team member.
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
    /// Creates a team member with the specified role.
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
    /// Gets the configuration for a specific role.
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
    /// Gets the appropriate temperature for the role.
    /// </summary>
    private static float GetTemperatureForRole(TeamRole role)
    {
        return role switch
        {
            TeamRole.TeamLead => 0.5f,   // Balanced
            TeamRole.Architect => 0.7f,  // More creative
            TeamRole.Developer => 0.3f,  // More deterministic
            TeamRole.Reviewer => 0.2f,   // Very precise
            _ => 0.5f
        };
    }

    // ========================================================================
    // INTERACTION METHODS
    // ========================================================================

    /// <summary>
    /// Sends a message to the agent and gets the response.
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <param name="includeSystemPrompt">Whether to include the system prompt</param>
    /// <returns>Agent's response</returns>
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
    /// Sends a message with streaming response.
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
    /// Resets the conversation thread.
    /// </summary>
    public void ResetThread()
    {
        Thread = Agent.GetNewThread();
        _isFirstMessage = true;
    }

    /// <summary>
    /// Textual representation of the member.
    /// </summary>
    public override string ToString() => $"{Emoji} {Name}";
}
