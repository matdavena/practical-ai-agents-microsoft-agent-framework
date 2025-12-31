// ============================================================================
// 09. MCP INTEGRATION
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBJECTIVE OF THIS PROJECT:
// Learn how AI agents can use MCP (Model Context Protocol)
// to access external tools and resources in a standardized way.
//
// ============================================================================
// WHAT IS MCP (MODEL CONTEXT PROTOCOL)?
// ============================================================================
//
// MCP is a STANDARD and OPEN protocol that allows LLMs to:
//
//    ┌─────────────────────────────────────────────────────────────────────┐
//    │                         AI AGENT                                    │
//    │                    (uses tools via MCP)                             │
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
// WHAT AN MCP SERVER EXPOSES:
// - Tools: functions that the LLM can call (e.g.: read_file, search_repos)
// - Resources: data and context (e.g.: file content, repo info)
// - Prompts: predefined templates for common tasks
//
// SUPPORTED TRANSPORTS:
// - Stdio: communication via stdin/stdout (local processes)
// - HTTP/SSE: remote servers with Server-Sent Events
// - WebSocket: bidirectional real-time communication
//
// ADVANTAGES:
// - Open standard: any LLM can use any MCP server
// - Security: granular control over which tools are available
// - Extensibility: easy to add new capabilities
// - Ecosystem: thousands of MCP servers already available
//
// RUN WITH: dotnet run --project core/09.MCP.Integration
//
// PREREQUISITES:
// - Node.js installed (for npx)
// - GITHUB_TOKEN env var (optional, for higher rate limits)
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
    // CONFIGURATION
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
        // VERIFY PREREQUISITES
        // ====================================================================
        ConsoleHelper.WriteSeparator("Verify Prerequisites");

        if (!await CheckNodeInstalled())
        {
            Console.WriteLine("Node.js not found. Install Node.js to use MCP npm servers.");
            Console.WriteLine("https://nodejs.org/");
            return;
        }

        Console.WriteLine("Node.js: OK");

        var hasGitHubToken = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_TOKEN"));
        Console.WriteLine($"GITHUB_TOKEN: {(hasGitHubToken ? "Configured" : "Not configured (reduced rate limits)")}");

        // ====================================================================
        // SETUP
        // ====================================================================
        ConsoleHelper.WriteSeparator("Setup");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(ChatModel).AsIChatClient();

        Console.WriteLine("OpenAI Client initialized.");
        Console.WriteLine();

        // ====================================================================
        // MCP DEMO MENU
        // ====================================================================
        ConsoleHelper.WriteSeparator("Choose MCP Demo");

        Console.WriteLine();
        Console.WriteLine("Available demos:");
        Console.WriteLine("   [1] Filesystem Server - Read/write local files");
        Console.WriteLine("   [2] GitHub Server - Query GitHub repositories");
        Console.WriteLine("   [3] Everything Server - Test server with various tools");
        Console.WriteLine("   [4] Explore Tools - Show available tools without execution");
        Console.WriteLine();
        Console.Write("Choose (1-4): ");

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
                Console.WriteLine("Invalid choice, running Explore Tools...");
                await RunExploreToolsDemo();
                break;
        }

        // ====================================================================
        // SUMMARY
        // ====================================================================
        ConsoleHelper.WriteSeparator("Summary");

        Console.WriteLine("In this project you learned:");
        Console.WriteLine("   1. MCP (Model Context Protocol) - standard for external tools");
        Console.WriteLine("   2. McpClient.CreateAsync() - connecting to MCP servers");
        Console.WriteLine("   3. StdioClientTransport - transport via stdin/stdout");
        Console.WriteLine("   4. ListToolsAsync() - dynamic tool discovery");
        Console.WriteLine("   5. Integrating MCP tools with AI agents");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("   - Explore other MCP servers: https://github.com/modelcontextprotocol/servers");
        Console.WriteLine("   - Create your own custom MCP server");
        Console.WriteLine("   - Use HTTP/SSE for remote servers");
    }

    // ========================================================================
    // DEMO 1: FILESYSTEM SERVER
    // ========================================================================
    //
    // The Filesystem MCP Server allows the agent to:
    // - Read files
    // - Write files
    // - List directories
    // - Search files
    //
    // SECURITY: The server limits access to specific directories!
    // ========================================================================

    private static async Task RunFilesystemDemo(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Demo: Filesystem MCP Server");

        Console.WriteLine();
        Console.WriteLine("This server allows the agent to operate on the filesystem.");
        Console.WriteLine("For security, we limit access to the temp directory.");
        Console.WriteLine();

        // Directory that the server will have access to
        var allowedDir = Path.GetTempPath();
        Console.WriteLine($"Allowed directory: {allowedDir}");
        Console.WriteLine();

        // Create a test file
        var testFile = Path.Combine(allowedDir, "mcp_test.txt");
        await File.WriteAllTextAsync(testFile, "This is a test file created for the MCP demo.\nIt contains important information.");
        Console.WriteLine($"Test file created: {testFile}");
        Console.WriteLine();

        try
        {
            // ================================================================
            // CONNECTION TO MCP SERVER
            // ================================================================
            // StdioClientTransport starts the server as a child process
            // and communicates via stdin/stdout (standard MCP transport)

            Console.WriteLine("Connecting to Filesystem MCP Server...");

            await using var mcpClient = await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "filesystem",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-filesystem", allowedDir]
                }));

            Console.WriteLine("Connected!");
            Console.WriteLine();

            // ================================================================
            // TOOL DISCOVERY
            // ================================================================
            // The client queries the server to get the list of tools

            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine($"Available tools ({mcpTools.Count}):");
            foreach (var tool in mcpTools)
            {
                Console.WriteLine($"   - {tool.Name}: {tool.Description}");
            }
            Console.WriteLine();

            // ================================================================
            // CREATE AGENT WITH MCP TOOLS
            // ================================================================
            // MCP tools are converted to AITool and passed to the agent
            // NOTE: We filter only essential tools to avoid exceeding
            // the model's context limit (MCP descriptions are verbose!)

            var essentialTools = new[] {
                "read_file", "write_file", "list_directory",
                "create_directory", "search_files"
            };

            var filteredTools = mcpTools
                .Where(t => essentialTools.Contains(t.Name))
                .Cast<AITool>()
                .ToList();

            Console.WriteLine($"Filtered tools for the model: {filteredTools.Count}");
            Console.WriteLine();

            var agent = chatClient.CreateAIAgent(
                instructions: $"""
                    You are an assistant that can operate on files in the directory: {allowedDir}
                    You can read, write, list and search files.
                    Always respond in Italian.
                    """,
                tools: filteredTools);

            Console.WriteLine("Agent created with MCP tools!");
            Console.WriteLine();

            // Chat loop
            var thread = agent.GetNewThread();

            Console.WriteLine("Command examples:");
            Console.WriteLine("   - Read the file mcp_test.txt");
            Console.WriteLine("   - What files are in the directory?");
            Console.WriteLine("   - Create a file called note.txt with 'Hello world' inside");
            Console.WriteLine();
            Console.WriteLine("Type 'exit' to quit.");
            Console.WriteLine();

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
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure that Node.js and npx are installed.");
        }
    }

    // ========================================================================
    // DEMO 2: GITHUB SERVER
    // ========================================================================
    //
    // The GitHub MCP Server allows the agent to:
    // - Search repositories
    // - Read files from repos
    // - View commits and pull requests
    // - Analyze code
    //
    // NOTE: Requires GITHUB_TOKEN for higher rate limits
    // ========================================================================

    private static async Task RunGitHubDemo(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Demo: GitHub MCP Server");

        Console.WriteLine();
        Console.WriteLine("This server allows the agent to query GitHub.");
        Console.WriteLine();

        try
        {
            // ================================================================
            // CONNECTION TO GITHUB MCP SERVER
            // ================================================================

            Console.WriteLine("Connecting to GitHub MCP Server...");

            await using var mcpClient = await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "github",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-github"]
                }));

            Console.WriteLine("Connected!");
            Console.WriteLine();

            // Tool discovery
            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine($"Available tools ({mcpTools.Count}):");
            foreach (var tool in mcpTools.Take(10)) // Show only the first 10
            {
                Console.WriteLine($"   - {tool.Name}");
            }
            if (mcpTools.Count > 10)
            {
                Console.WriteLine($"   ... and {mcpTools.Count - 10} more tools");
            }
            Console.WriteLine();

            // Filter essential tools to avoid context overflow
            var essentialTools = new[] {
                "search_repositories", "get_file_contents", "list_commits",
                "get_issue", "search_code"
            };

            var filteredTools = mcpTools
                .Where(t => essentialTools.Contains(t.Name))
                .Cast<AITool>()
                .ToList();

            // If we don't find the filtered tools, use the first 5
            if (filteredTools.Count == 0)
            {
                filteredTools = mcpTools.Take(5).Cast<AITool>().ToList();
            }

            Console.WriteLine($"Filtered tools for the model: {filteredTools.Count}");
            Console.WriteLine();

            // Create agent
            var agent = chatClient.CreateAIAgent(
                instructions: """
                    You are a GitHub expert assistant.
                    You can search repositories, read code, view commits and PRs.
                    Always respond in Italian with precise information.
                    When showing code, use markdown blocks.
                    """,
                tools: filteredTools);

            var thread = agent.GetNewThread();

            Console.WriteLine("Question examples:");
            Console.WriteLine("   - What are the latest commits of microsoft/semantic-kernel?");
            Console.WriteLine("   - Search repositories for 'agent framework'");
            Console.WriteLine("   - Show the README of anthropics/anthropic-cookbook");
            Console.WriteLine();
            Console.WriteLine("Type 'exit' to quit.");
            Console.WriteLine();

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
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure that:");
            Console.WriteLine("   1. Node.js and npx are installed");
            Console.WriteLine("   2. GITHUB_TOKEN is configured (optional but recommended)");
        }
    }

    // ========================================================================
    // DEMO 3: EVERYTHING SERVER (Test)
    // ========================================================================
    //
    // The Everything Server is a test server that exposes various tools
    // to test MCP integration. Includes:
    // - echo: repeats the input
    // - add: adds numbers
    // - longRunningOperation: long operation
    // - sampleLLM: requests sampling from the client
    // ========================================================================

    private static async Task RunEverythingDemo(IChatClient chatClient)
    {
        ConsoleHelper.WriteSeparator("Demo: Everything MCP Server (Test)");

        Console.WriteLine();
        Console.WriteLine("This is a TEST server with various tools to experiment.");
        Console.WriteLine();

        try
        {
            Console.WriteLine("Connecting to Everything MCP Server...");

            await using var mcpClient = await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "everything",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-everything"]
                }));

            Console.WriteLine("Connected!");
            Console.WriteLine();

            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine($"Available tools ({mcpTools.Count}):");
            foreach (var tool in mcpTools)
            {
                Console.WriteLine($"   - {tool.Name}: {tool.Description}");
            }
            Console.WriteLine();

            // Filter essential tools to avoid context overflow
            var filteredTools = mcpTools.Take(5).Cast<AITool>().ToList();

            Console.WriteLine($"Filtered tools for the model: {filteredTools.Count}");
            Console.WriteLine();

            var agent = chatClient.CreateAIAgent(
                instructions: """
                    You are a test assistant for MCP.
                    You have access to various test tools.
                    Respond in Italian and explain what you're doing.
                    """,
                tools: filteredTools);

            var thread = agent.GetNewThread();

            Console.WriteLine("Examples:");
            Console.WriteLine("   - Use echo to repeat 'hello world'");
            Console.WriteLine("   - Add 42 and 58");
            Console.WriteLine("   - What is today's date?");
            Console.WriteLine();
            Console.WriteLine("Type 'exit' to quit.");
            Console.WriteLine();

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
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    // ========================================================================
    // DEMO 4: EXPLORE TOOLS (without execution)
    // ========================================================================
    //
    // Shows how to explore available tools on various MCP servers
    // without actually using them. Useful for understanding capabilities.
    // ========================================================================

    private static async Task RunExploreToolsDemo()
    {
        ConsoleHelper.WriteSeparator("Demo: Explore MCP Tools");

        Console.WriteLine();
        Console.WriteLine("This demo shows the available tools on different MCP servers.");
        Console.WriteLine("It does not execute operations, only discovery.");
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

                Console.WriteLine($"Tools ({tools.Count}):");
                foreach (var tool in tools)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"   {tool.Name}");
                    Console.ResetColor();

                    if (!string.IsNullOrEmpty(tool.Description))
                    {
                        // Truncate long description
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

                // Also try to list resources if available
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
                    // Server might not support resources
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("Exploration completed!");
        Console.WriteLine();
        Console.WriteLine("For other MCP servers, visit:");
        Console.WriteLine("   https://github.com/modelcontextprotocol/servers");
    }

    // ========================================================================
    // HELPER: Verify Node.js
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
