// ============================================================================
// 10. MCP CUSTOM SERVER
// FILE: Program.cs (Client)
// ============================================================================
//
// OBIETTIVO:
// Client che si connette al nostro MCP Server custom e usa i tool
// attraverso un agente AI (Microsoft Agent Framework).
//
// ARCHITETTURA:
//
//    ┌────────────────────────────────────────────────────────────────────┐
//    │                         CLIENT (questo progetto)                   │
//    │                                                                    │
//    │   ┌─────────────┐      ┌─────────────┐      ┌─────────────┐        │
//    │   │   OpenAI    │ ──►  │   Agente    │ ──►  │ MCP Client  │        │
//    │   │   (LLM)     │      │     AI      │      │  (HTTP)     │        │
//    │   └─────────────┘      └─────────────┘      └──────┬──────┘        │
//    └────────────────────────────────────────────────────┼───────────────┘
//                                                         │
//                                                    HTTP/SSE
//                                                         │
//                                                         ▼
//                                              ┌─────────────────────┐
//                                              │   MCP Server        │
//                                              │   (localhost:5100)  │
//                                              └─────────────────────┘
//
// PREREQUISITO: Avvia prima il Server!
//   dotnet run --project core/10.MCP.CustomServer/Server
//
// ESEGUI CON: dotnet run --project core/10.MCP.CustomServer/Client
// ============================================================================

using System.Text;
using Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

namespace Client;

public static class Program
{
    private const string ChatModel = "gpt-4o-mini";
    private const string McpServerUrl = "http://localhost:5100/mcp";

    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleHelper.WriteTitle("10. MCP Custom Server");
        ConsoleHelper.WriteSubtitle("Client con Agente AI");

        // ====================================================================
        // VERIFICA SERVER
        // ====================================================================
        ConsoleHelper.WriteSeparator("Verifica Server MCP");

        Console.WriteLine($"Tentativo di connessione a: {McpServerUrl}");
        Console.WriteLine();
        Console.WriteLine("NOTA: Il server deve essere in esecuzione!");
        Console.WriteLine("      Esegui prima: dotnet run --project core/10.MCP.CustomServer/Server");
        Console.WriteLine();

        try
        {
            // ================================================================
            // CONNESSIONE AL MCP SERVER VIA HTTP
            // ================================================================
            // HttpClientTransport si connette a server MCP via HTTP/SSE
            // A differenza di StdioClientTransport (processi locali),
            // questo permette di connettersi a server remoti.

            Console.WriteLine("Connessione al MCP Server...");

            await using var mcpClient = await McpClient.CreateAsync(
                new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(McpServerUrl),
                    Name = "CustomServer",
                    ConnectionTimeout = TimeSpan.FromSeconds(10)
                }));

            Console.WriteLine("Connesso!");
            Console.WriteLine();

            // ================================================================
            // DISCOVERY DEI TOOL
            // ================================================================

            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine($"Tool disponibili dal server ({mcpTools.Count}):");
            foreach (var tool in mcpTools)
            {
                Console.WriteLine($"   - {tool.Name}: {tool.Description}");
            }
            Console.WriteLine();

            // ================================================================
            // SETUP OPENAI + AGENTE
            // ================================================================
            ConsoleHelper.WriteSeparator("Setup Agente AI");

            var apiKey = ConfigurationHelper.GetOpenAiApiKey();
            var openAiClient = new OpenAIClient(apiKey);
            var chatClient = openAiClient.GetChatClient(ChatModel).AsIChatClient();

            // Creiamo l'agente con i tool MCP
            var agent = chatClient.CreateAIAgent(
                instructions: """
                    Sei un assistente utile che può eseguire calcoli matematici
                    e manipolare stringhe usando i tool disponibili.

                    Usa SEMPRE i tool quando l'utente chiede calcoli o operazioni su stringhe.
                    Rispondi in italiano.
                    Mostra i risultati in modo chiaro.
                    """,
                tools: [.. mcpTools.Cast<AITool>()]);

            Console.WriteLine("Agente AI creato con tool MCP!");
            Console.WriteLine();

            // ================================================================
            // CHAT INTERATTIVA
            // ================================================================
            ConsoleHelper.WriteSeparator("Chat con Agente");

            Console.WriteLine("Esempi di comandi:");
            Console.WriteLine("   - Quanto fa 125 + 37?");
            Console.WriteLine("   - Calcola il 15% di 200");
            Console.WriteLine("   - Inverti la stringa 'ciao mondo'");
            Console.WriteLine("   - Converti 'HELLO WORLD' in minuscolo");
            Console.WriteLine("   - Quanti caratteri ha la frase 'test di conteggio'?");
            Console.WriteLine("   - Crea uno slug da 'Articolo di Prova!'");
            Console.WriteLine();
            Console.WriteLine("Scrivi 'exit' per uscire.");
            Console.WriteLine();

            var thread = agent.GetNewThread();

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

                try
                {
                    await foreach (var chunk in agent.RunStreamingAsync(input, thread))
                    {
                        Console.Write(chunk);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nErrore: {ex.Message}");
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Errore di connessione: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Assicurati che il server MCP sia in esecuzione:");
            Console.WriteLine("   dotnet run --project core/10.MCP.CustomServer/Server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore: {ex.Message}");
        }

        // ====================================================================
        // RIEPILOGO
        // ====================================================================
        ConsoleHelper.WriteSeparator("Riepilogo");

        Console.WriteLine("In questo progetto hai imparato:");
        Console.WriteLine("   1. Creare un MCP Server custom in C#");
        Console.WriteLine("   2. Definire tool con [McpServerTool] e [McpServerToolType]");
        Console.WriteLine("   3. Esporre tool via HTTP usando AddMcpServer().WithHttpTransport()");
        Console.WriteLine("   4. Connettersi al server con HttpClientTransport");
        Console.WriteLine("   5. Integrare tool MCP con agenti Microsoft Agent Framework");
        Console.WriteLine();
        Console.WriteLine("Questo completa il ciclo: hai creato sia il SERVER che il CLIENT MCP!");
    }
}
