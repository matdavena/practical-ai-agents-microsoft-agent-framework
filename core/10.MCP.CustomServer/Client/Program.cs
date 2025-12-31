// ============================================================================
// 10. MCP CUSTOM SERVER
// FILE: Program.cs (Client)
// ============================================================================
//
// OBJECTIVE:
// Client that connects to our custom MCP Server and uses tools
// through an AI agent (Microsoft Agent Framework).
//
// ARCHITECTURE:
//
//    ┌────────────────────────────────────────────────────────────────────┐
//    │                         CLIENT (this project)                      │
//    │                                                                    │
//    │   ┌─────────────┐      ┌─────────────┐      ┌─────────────┐        │
//    │   │   OpenAI    │ ──►  │   Agent     │ ──►  │ MCP Client  │        │
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
// PREREQUISITE: Start the Server first!
//   dotnet run --project core/10.MCP.CustomServer/Server
//
// RUN WITH: dotnet run --project core/10.MCP.CustomServer/Client
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
        ConsoleHelper.WriteSubtitle("Client with AI Agent");

        // ====================================================================
        // SERVER VERIFICATION
        // ====================================================================
        ConsoleHelper.WriteSeparator("MCP Server Verification");

        Console.WriteLine($"Connection attempt to: {McpServerUrl}");
        Console.WriteLine();
        Console.WriteLine("NOTE: The server must be running!");
        Console.WriteLine("      Run first: dotnet run --project core/10.MCP.CustomServer/Server");
        Console.WriteLine();

        try
        {
            // ================================================================
            // CONNECTION TO MCP SERVER VIA HTTP
            // ================================================================
            // HttpClientTransport connects to MCP server via HTTP/SSE
            // Unlike StdioClientTransport (local processes),
            // this allows connecting to remote servers.

            Console.WriteLine("Connecting to MCP Server...");

            await using var mcpClient = await McpClient.CreateAsync(
                new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(McpServerUrl),
                    Name = "CustomServer",
                    ConnectionTimeout = TimeSpan.FromSeconds(10)
                }));

            Console.WriteLine("Connected!");
            Console.WriteLine();

            // ================================================================
            // TOOL DISCOVERY
            // ================================================================

            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine($"Available tools from server ({mcpTools.Count}):");
            foreach (var tool in mcpTools)
            {
                Console.WriteLine($"   - {tool.Name}: {tool.Description}");
            }
            Console.WriteLine();

            // ================================================================
            // OPENAI + AGENT SETUP
            // ================================================================
            ConsoleHelper.WriteSeparator("AI Agent Setup");

            var apiKey = ConfigurationHelper.GetOpenAiApiKey();
            var openAiClient = new OpenAIClient(apiKey);
            var chatClient = openAiClient.GetChatClient(ChatModel).AsIChatClient();

            // Create the agent with MCP tools
            var agent = chatClient.CreateAIAgent(
                instructions: """
                    You are a helpful assistant that can perform mathematical calculations
                    and manipulate strings using the available tools.

                    ALWAYS use the tools when the user asks for calculations or string operations.
                    Respond in Italian.
                    Show the results clearly.
                    """,
                tools: [.. mcpTools.Cast<AITool>()]);

            Console.WriteLine("AI Agent created with MCP tools!");
            Console.WriteLine();

            // ================================================================
            // INTERACTIVE CHAT
            // ================================================================
            ConsoleHelper.WriteSeparator("Chat with Agent");

            Console.WriteLine("Example commands:");
            Console.WriteLine("   - How much is 125 + 37?");
            Console.WriteLine("   - Calculate 15% of 200");
            Console.WriteLine("   - Reverse the string 'hello world'");
            Console.WriteLine("   - Convert 'HELLO WORLD' to lowercase");
            Console.WriteLine("   - How many characters does the phrase 'test count' have?");
            Console.WriteLine("   - Create a slug from 'Test Article!'");
            Console.WriteLine();
            Console.WriteLine("Type 'exit' to quit.");
            Console.WriteLine();

            var thread = agent.GetNewThread();

            while (true)
            {
                Console.Write("You: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                Console.WriteLine();
                Console.Write("Agent: ");

                try
                {
                    await foreach (var chunk in agent.RunStreamingAsync(input, thread))
                    {
                        Console.Write(chunk);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError: {ex.Message}");
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure the MCP server is running:");
            Console.WriteLine("   dotnet run --project core/10.MCP.CustomServer/Server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        // ====================================================================
        // SUMMARY
        // ====================================================================
        ConsoleHelper.WriteSeparator("Summary");

        Console.WriteLine("In this project you learned:");
        Console.WriteLine("   1. Create a custom MCP Server in C#");
        Console.WriteLine("   2. Define tools with [McpServerTool] and [McpServerToolType]");
        Console.WriteLine("   3. Expose tools via HTTP using AddMcpServer().WithHttpTransport()");
        Console.WriteLine("   4. Connect to the server with HttpClientTransport");
        Console.WriteLine("   5. Integrate MCP tools with Microsoft Agent Framework agents");
        Console.WriteLine();
        Console.WriteLine("This completes the cycle: you have created both the MCP SERVER and CLIENT!");
    }
}
