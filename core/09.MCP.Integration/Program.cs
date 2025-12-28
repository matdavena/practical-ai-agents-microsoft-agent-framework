// ============================================================================
// 09. MCP INTEGRATION
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBIETTIVO DI QUESTO PROGETTO:
// Imparare come gli agenti AI possono utilizzare MCP (Model Context Protocol)
// per accedere a tool e risorse esterne in modo standardizzato.
//
// ============================================================================
// COS'È MCP (MODEL CONTEXT PROTOCOL)?
// ============================================================================
//
// MCP è un protocollo STANDARD e APERTO che permette agli LLM di:
//
//    ┌─────────────────────────────────────────────────────────────────────┐
//    │                         AGENTE AI                                   │
//    │                    (usa tool via MCP)                               │
//    └──────────────────────────┬──────────────────────────────────────────┘
//                               │
//                    ┌──────────┴──────────┐
//                    │   MCP PROTOCOL      │
//                    │   (standard)        │
//                    └──────────┬──────────┘
//                               │
//         ┌─────────────────────┼─────────────────────┐
//         │                     │                     │
//         ▼                     ▼                     ▼
//    ┌──────────┐         ┌──────────┐         ┌──────────┐
//    │Filesystem│         │  GitHub  │         │ Database │
//    │  Server  │         │  Server  │         │  Server  │
//    └──────────┘         └──────────┘         └──────────┘
//
// COSA ESPONE UN MCP SERVER:
// - Tools: funzioni che l'LLM può chiamare (es: read_file, search_repos)
// - Resources: dati e contesto (es: file content, repo info)
// - Prompts: template predefiniti per task comuni
//
// TRASPORTI SUPPORTATI:
// - Stdio: comunicazione via stdin/stdout (processi locali)
// - HTTP/SSE: server remoti con Server-Sent Events
// - WebSocket: comunicazione bidirezionale real-time
//
// VANTAGGI:
// - Standard aperto: qualsiasi LLM può usare qualsiasi MCP server
// - Sicurezza: controllo granulare su quali tool sono disponibili
// - Estensibilità: facile aggiungere nuove capability
// - Ecosistema: migliaia di MCP server già disponibili
//
// ESEGUI CON: dotnet run --project core/09.MCP.Integration
//
// PREREQUISITI:
// - Node.js installato (per npx)
// - GITHUB_TOKEN env var (opzionale, per rate limit più alti)
// ============================================================================

using System.Text;
using Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

namespace MCP.Integration;

public static class Program
{
    // ========================================================================
    // CONFIGURAZIONE
    // ========================================================================

    private const string ChatModel = "gpt-4o-mini";

    // ========================================================================
    // ENTRY POINT
    // ========================================================================

    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleHelper.WriteTitle("09. MCP Integration");
        ConsoleHelper.WriteSubtitle("Model Context Protocol");

        // ====================================================================
        // VERIFICA PREREQUISITI
        // ====================================================================
        ConsoleHelper.WriteSeparator("Verifica Prerequisiti");

        if (!await CheckNodeInstalled())
        {
            Console.WriteLine("Node.js non trovato. Installa Node.js per usare MCP server npm.");
            Console.WriteLine("https://nodejs.org/");
            return;
        }

        Console.WriteLine("Node.js: OK");

        var hasGitHubToken = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_TOKEN"));
        Console.WriteLine($"GITHUB_TOKEN: {(hasGitHubToken ? "Configurato" : "Non configurato (rate limit ridotti)")}");

        // ====================================================================
        // SETUP
        // ====================================================================
        ConsoleHelper.WriteSeparator("Setup");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(ChatModel).AsIChatClient();

        Console.WriteLine("OpenAI Client inizializzato.");
        Console.WriteLine();

        // ====================================================================
        // MENU DEMO MCP
        // ====================================================================
        ConsoleHelper.WriteSeparator("Scegli Demo MCP");

        Console.WriteLine();
        Console.WriteLine("Demo disponibili:");
        Console.WriteLine("   [1] Filesystem Server - Leggi/scrivi file locali");
        Console.WriteLine("   [2] GitHub Server - Interroga repository GitHub");
        Console.WriteLine("   [3] Everything Server - Server di test con vari tool");
        Console.WriteLine("   [4] Esplora Tool - Mostra tool disponibili senza eseguire");
        Console.WriteLine();
        Console.Write("Scegli (1-4): ");

        var choice = Console.ReadLine()?.Trim();

        switch (choice)
        {
            case "1":
                await RunFilesystemDemo(chatClient);
                break;
            case "2":
                await RunGitHubDemo(chatClient);
                break;
            case "3":
                await RunEverythingDemo(chatClient);
                break;
            case "4":
                await RunExploreToolsDemo();
                break;
            default:
                Console.WriteLine("Scelta non valida, eseguo Explore Tools...");
                await RunExploreToolsDemo();
                break;
        }

        // ====================================================================
        // RIEPILOGO
        // ====================================================================
        ConsoleHelper.WriteSeparator("Riepilogo");

        Console.WriteLine("In questo progetto hai imparato:");
        Console.WriteLine("   1. MCP (Model Context Protocol) - standard per tool esterni");
        Console.WriteLine("   2. McpClient.CreateAsync() - connettersi a MCP server");
        Console.WriteLine("   3. StdioClientTransport - trasporto via stdin/stdout");
        Console.WriteLine("   4. ListToolsAsync() - scoperta dinamica dei tool");
        Console.WriteLine("   5. Integrazione tool MCP con agenti AI");
        Console.WriteLine();
        Console.WriteLine("Prossimi passi:");
        Console.WriteLine("   - Esplora altri MCP server: https://github.com/modelcontextprotocol/servers");
        Console.WriteLine("   - Crea il tuo MCP server custom");
        Console.WriteLine("   - Usa HTTP/SSE per server remoti");
    }

    // ========================================================================
    // DEMO 1: FILESYSTEM SERVER
    // ========================================================================
    //
    // Il Filesystem MCP Server permette all'agente di:
    // - Leggere file
    // - Scrivere file
    // - Listare directory
    // - Cercare file
    //
    // SICUREZZA: Il server limita l'accesso a directory specifiche!
    // ========================================================================

    private static async Task RunFilesystemDemo(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Demo: Filesystem MCP Server");

        Console.WriteLine();
        Console.WriteLine("Questo server permette all'agente di operare sul filesystem.");
        Console.WriteLine("Per sicurezza, limitiamo l'accesso alla directory temp.");
        Console.WriteLine();

        // Directory a cui il server avrà accesso
        var allowedDir = Path.GetTempPath();
        Console.WriteLine($"Directory consentita: {allowedDir}");
        Console.WriteLine();

        // Creiamo un file di test
        var testFile = Path.Combine(allowedDir, "mcp_test.txt");
        await File.WriteAllTextAsync(testFile, "Questo è un file di test creato per la demo MCP.\nContiene informazioni importanti.");
        Console.WriteLine($"File di test creato: {testFile}");
        Console.WriteLine();

        try
        {
            // ================================================================
            // CONNESSIONE AL MCP SERVER
            // ================================================================
            // StdioClientTransport avvia il server come processo figlio
            // e comunica via stdin/stdout (standard MCP transport)

            Console.WriteLine("Connessione al Filesystem MCP Server...");

            await using var mcpClient = await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "filesystem",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-filesystem", allowedDir]
                }));

            Console.WriteLine("Connesso!");
            Console.WriteLine();

            // ================================================================
            // SCOPERTA DEI TOOL
            // ================================================================
            // Il client interroga il server per ottenere la lista dei tool

            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine($"Tool disponibili ({mcpTools.Count}):");
            foreach (var tool in mcpTools)
            {
                Console.WriteLine($"   - {tool.Name}: {tool.Description}");
            }
            Console.WriteLine();

            // ================================================================
            // CREAZIONE AGENTE CON TOOL MCP
            // ================================================================
            // I tool MCP vengono convertiti in AITool e passati all'agente
            // NOTA: Filtriamo solo i tool essenziali per evitare di superare
            // il context limit del modello (le descrizioni MCP sono verbose!)

            var essentialTools = new[] {
                "read_file", "write_file", "list_directory",
                "create_directory", "search_files"
            };

            var filteredTools = mcpTools
                .Where(t => essentialTools.Contains(t.Name))
                .Cast<AITool>()
                .ToList();

            Console.WriteLine($"Tool filtrati per il modello: {filteredTools.Count}");
            Console.WriteLine();

            var agent = chatClient.CreateAIAgent(
                instructions: $"""
                    Sei un assistente che può operare sui file nella directory: {allowedDir}
                    Puoi leggere, scrivere, listare e cercare file.
                    Rispondi sempre in italiano.
                    """,
                tools: filteredTools);

            Console.WriteLine("Agente creato con tool MCP!");
            Console.WriteLine();

            // Loop di chat
            var thread = agent.GetNewThread();

            Console.WriteLine("Esempi di comandi:");
            Console.WriteLine("   - Leggi il file mcp_test.txt");
            Console.WriteLine("   - Quali file ci sono nella directory?");
            Console.WriteLine("   - Crea un file chiamato note.txt con dentro 'Ciao mondo'");
            Console.WriteLine();
            Console.WriteLine("Scrivi 'exit' per uscire.");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Tu: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                Console.WriteLine();
                Console.Write("Agente: ");

                await foreach (var chunk in agent.RunStreamingAsync(input, thread))
                {
                    Console.Write(chunk);
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Assicurati che Node.js e npx siano installati.");
        }
    }

    // ========================================================================
    // DEMO 2: GITHUB SERVER
    // ========================================================================
    //
    // Il GitHub MCP Server permette all'agente di:
    // - Cercare repository
    // - Leggere file da repo
    // - Vedere commit e pull request
    // - Analizzare codice
    //
    // NOTA: Richiede GITHUB_TOKEN per rate limit più alti
    // ========================================================================

    private static async Task RunGitHubDemo(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Demo: GitHub MCP Server");

        Console.WriteLine();
        Console.WriteLine("Questo server permette all'agente di interrogare GitHub.");
        Console.WriteLine();

        try
        {
            // ================================================================
            // CONNESSIONE AL MCP SERVER GITHUB
            // ================================================================

            Console.WriteLine("Connessione al GitHub MCP Server...");

            await using var mcpClient = await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "github",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-github"]
                }));

            Console.WriteLine("Connesso!");
            Console.WriteLine();

            // Scoperta tool
            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine($"Tool disponibili ({mcpTools.Count}):");
            foreach (var tool in mcpTools.Take(10)) // Mostra solo i primi 10
            {
                Console.WriteLine($"   - {tool.Name}");
            }
            if (mcpTools.Count > 10)
            {
                Console.WriteLine($"   ... e altri {mcpTools.Count - 10} tool");
            }
            Console.WriteLine();

            // Filtriamo i tool essenziali per evitare context overflow
            var essentialTools = new[] {
                "search_repositories", "get_file_contents", "list_commits",
                "get_issue", "search_code"
            };

            var filteredTools = mcpTools
                .Where(t => essentialTools.Contains(t.Name))
                .Cast<AITool>()
                .ToList();

            // Se non troviamo i tool filtrati, usiamo i primi 5
            if (filteredTools.Count == 0)
            {
                filteredTools = mcpTools.Take(5).Cast<AITool>().ToList();
            }

            Console.WriteLine($"Tool filtrati per il modello: {filteredTools.Count}");
            Console.WriteLine();

            // Creazione agente
            var agent = chatClient.CreateAIAgent(
                instructions: """
                    Sei un assistente esperto di GitHub.
                    Puoi cercare repository, leggere codice, vedere commit e PR.
                    Rispondi sempre in italiano con informazioni precise.
                    Quando mostri codice, usa i blocchi markdown.
                    """,
                tools: filteredTools);

            var thread = agent.GetNewThread();

            Console.WriteLine("Esempi di domande:");
            Console.WriteLine("   - Quali sono gli ultimi commit di microsoft/semantic-kernel?");
            Console.WriteLine("   - Cerca repository su 'agent framework'");
            Console.WriteLine("   - Mostra il README di anthropics/anthropic-cookbook");
            Console.WriteLine();
            Console.WriteLine("Scrivi 'exit' per uscire.");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Tu: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                Console.WriteLine();
                Console.Write("Agente: ");

                await foreach (var chunk in agent.RunStreamingAsync(input, thread))
                {
                    Console.Write(chunk);
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Assicurati che:");
            Console.WriteLine("   1. Node.js e npx siano installati");
            Console.WriteLine("   2. GITHUB_TOKEN sia configurato (opzionale ma consigliato)");
        }
    }

    // ========================================================================
    // DEMO 3: EVERYTHING SERVER (Test)
    // ========================================================================
    //
    // L'Everything Server è un server di test che espone vari tool
    // per testare l'integrazione MCP. Include:
    // - echo: ripete l'input
    // - add: somma numeri
    // - longRunningOperation: operazione lunga
    // - sampleLLM: richiede sampling dal client
    // ========================================================================

    private static async Task RunEverythingDemo(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Demo: Everything MCP Server (Test)");

        Console.WriteLine();
        Console.WriteLine("Questo è un server di TEST con vari tool per sperimentare.");
        Console.WriteLine();

        try
        {
            Console.WriteLine("Connessione all'Everything MCP Server...");

            await using var mcpClient = await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "everything",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-everything"]
                }));

            Console.WriteLine("Connesso!");
            Console.WriteLine();

            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine($"Tool disponibili ({mcpTools.Count}):");
            foreach (var tool in mcpTools)
            {
                Console.WriteLine($"   - {tool.Name}: {tool.Description}");
            }
            Console.WriteLine();

            // Filtriamo i tool essenziali per evitare context overflow
            var filteredTools = mcpTools.Take(5).Cast<AITool>().ToList();

            Console.WriteLine($"Tool filtrati per il modello: {filteredTools.Count}");
            Console.WriteLine();

            var agent = chatClient.CreateAIAgent(
                instructions: """
                    Sei un assistente di test per MCP.
                    Hai accesso a vari tool di test.
                    Rispondi in italiano e spiega cosa stai facendo.
                    """,
                tools: filteredTools);

            var thread = agent.GetNewThread();

            Console.WriteLine("Esempi:");
            Console.WriteLine("   - Usa echo per ripetere 'ciao mondo'");
            Console.WriteLine("   - Somma 42 e 58");
            Console.WriteLine("   - Qual è la data di oggi?");
            Console.WriteLine();
            Console.WriteLine("Scrivi 'exit' per uscire.");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Tu: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                Console.WriteLine();
                Console.Write("Agente: ");

                await foreach (var chunk in agent.RunStreamingAsync(input, thread))
                {
                    Console.Write(chunk);
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore: {ex.Message}");
        }
    }

    // ========================================================================
    // DEMO 4: ESPLORA TOOL (senza eseguire)
    // ========================================================================
    //
    // Mostra come esplorare i tool disponibili su vari MCP server
    // senza effettivamente usarli. Utile per capire le capability.
    // ========================================================================

    private static async Task RunExploreToolsDemo()
    {
        ConsoleHelper.WriteSeparator("Demo: Esplora Tool MCP");

        Console.WriteLine();
        Console.WriteLine("Questa demo mostra i tool disponibili su diversi MCP server.");
        Console.WriteLine("Non esegue operazioni, solo discovery.");
        Console.WriteLine();

        var servers = new[]
        {
            ("filesystem", "npx", new[] { "-y", "@modelcontextprotocol/server-filesystem", Path.GetTempPath() }),
            ("everything", "npx", new[] { "-y", "@modelcontextprotocol/server-everything" }),
        };

        foreach (var (name, command, args) in servers)
        {
            Console.WriteLine($"--- Server: {name} ---");

            try
            {
                await using var mcpClient = await McpClient.CreateAsync(
                    new StdioClientTransport(new StdioClientTransportOptions
                    {
                        Name = name,
                        Command = command,
                        Arguments = args
                    }));

                var tools = await mcpClient.ListToolsAsync();

                Console.WriteLine($"Tool ({tools.Count}):");
                foreach (var tool in tools)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"   {tool.Name}");
                    Console.ResetColor();

                    if (!string.IsNullOrEmpty(tool.Description))
                    {
                        // Tronca descrizione lunga
                        var desc = tool.Description.Length > 60
                            ? tool.Description[..57] + "..."
                            : tool.Description;
                        Console.WriteLine($": {desc}");
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }

                // Prova anche a listare le risorse se disponibili
                try
                {
                    var resources = await mcpClient.ListResourcesAsync();
                    if (resources.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Resources ({resources.Count()}):");
                        foreach (var resource in resources.Take(5))
                        {
                            Console.WriteLine($"   - {resource.Name}: {resource.Uri}");
                        }
                    }
                }
                catch
                {
                    // Server potrebbe non supportare resources
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Errore: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("Esplorazione completata!");
        Console.WriteLine();
        Console.WriteLine("Per altri server MCP, visita:");
        Console.WriteLine("   https://github.com/modelcontextprotocol/servers");
    }

    // ========================================================================
    // HELPER: Verifica Node.js
    // ========================================================================

    private static async Task<bool> CheckNodeInstalled()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
