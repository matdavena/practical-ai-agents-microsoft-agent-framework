// ============================================================================
// CHAT AGENTS API CLIENT
// ============================================================================
// Test client for the Chat Agents Web API.
// Demonstrates:
// - Multi-user simulation
// - Testing all API endpoints
// - Interactive chat sessions
// - Conversation persistence verification
// ============================================================================

using System.Text;
using Spectre.Console;
using WebApi.ChatAgents.Client;

Console.OutputEncoding = Encoding.UTF8;

// Configuration
const string DEFAULT_API_URL = "http://localhost:5200";

// ============================================================================
// STARTUP
// ============================================================================

AnsiConsole.Write(new FigletText("Chat API Client").Color(Color.Cyan1));
AnsiConsole.MarkupLine("[grey]Test client for Chat Agents Web API[/]");
AnsiConsole.WriteLine();

// Get API URL
var apiUrl = AnsiConsole.Prompt(
    new TextPrompt<string>("API Base URL:")
        .DefaultValue(DEFAULT_API_URL)
        .AllowEmpty());

if (string.IsNullOrEmpty(apiUrl)) apiUrl = DEFAULT_API_URL;

// Test connection
AnsiConsole.MarkupLine($"[yellow]Connecting to {apiUrl}...[/]");

try
{
    using var testClient = new ChatApiClient(apiUrl, "test");
    var agents = await testClient.GetAgentsAsync();
    AnsiConsole.MarkupLine($"[green]Connected! Found {agents.Count} agents.[/]");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Failed to connect: {ex.Message}[/]");
    AnsiConsole.MarkupLine("[yellow]Make sure the API server is running:[/]");
    AnsiConsole.MarkupLine("[grey]  cd core/12.WebApi.ChatAgents[/]");
    AnsiConsole.MarkupLine("[grey]  dotnet run[/]");
    return;
}

AnsiConsole.WriteLine();

// ============================================================================
// MAIN MENU
// ============================================================================

while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[cyan]What would you like to do?[/]")
            .AddChoices(
                "1. Quick Demo (automated multi-user test)",
                "2. Interactive Chat (single user)",
                "3. Multi-User Simulation",
                "4. API Explorer (test individual endpoints)",
                "5. Exit"));

    AnsiConsole.WriteLine();

    switch (choice[0])
    {
        case '1':
            await RunQuickDemo(apiUrl);
            break;
        case '2':
            await RunInteractiveChat(apiUrl);
            break;
        case '3':
            await RunMultiUserSimulation(apiUrl);
            break;
        case '4':
            await RunApiExplorer(apiUrl);
            break;
        case '5':
            AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
            return;
    }

    AnsiConsole.WriteLine();
}

// ============================================================================
// QUICK DEMO - Automated Multi-User Test
// ============================================================================

async Task RunQuickDemo(string apiUrl)
{
    AnsiConsole.Write(new Rule("[cyan]Quick Demo - Multi-User Test[/]"));
    AnsiConsole.WriteLine();

    // Create multiple users
    var users = new[] { "alice", "bob", "charlie" };
    var clients = users.Select(u => new ChatApiClient(apiUrl, u)).ToList();

    try
    {
        // Step 1: Show available agents
        AnsiConsole.MarkupLine("[yellow]Step 1: Fetching available agents...[/]");
        var agents = await clients[0].GetAgentsAsync();

        var agentTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Key")
            .AddColumn("Name")
            .AddColumn("Description");

        foreach (var agent in agents)
        {
            agentTable.AddRow(agent.Key, agent.Name, agent.Description);
        }
        AnsiConsole.Write(agentTable);
        AnsiConsole.WriteLine();

        // Step 2: Each user starts a conversation with a different agent
        AnsiConsole.MarkupLine("[yellow]Step 2: Each user chats with a different agent...[/]");
        AnsiConsole.WriteLine();

        var conversations = new Dictionary<string, string>(); // userId -> conversationId

        var userMessages = new[]
        {
            ("alice", "assistant", "Hello! What can you help me with?"),
            ("bob", "coder", "How do I create a REST API in C#?"),
            ("charlie", "translator", "Translate 'Hello, how are you?' to Italian")
        };

        foreach (var (userId, agentKey, message) in userMessages)
        {
            var client = clients.First(c => c.UserId == userId);

            AnsiConsole.MarkupLine($"[blue][[{userId}]][/] -> [green]{agentKey}[/]: {message}");

            var response = await client.ChatAsync(agentKey, message);
            if (response != null)
            {
                conversations[userId] = response.ConversationId;
                var preview = response.Message.Length > 100
                    ? response.Message[..100] + "..."
                    : response.Message;
                AnsiConsole.MarkupLine($"[grey]  Response: {Markup.Escape(preview)}[/]");
            }
            AnsiConsole.WriteLine();
        }

        // Step 3: Continue conversations
        AnsiConsole.MarkupLine("[yellow]Step 3: Users continue their conversations...[/]");
        AnsiConsole.WriteLine();

        var followUps = new[]
        {
            ("alice", "Tell me more about AI agents"),
            ("bob", "Can you show me an example with controllers?"),
            ("charlie", "Now translate it to French")
        };

        foreach (var (userId, message) in followUps)
        {
            var client = clients.First(c => c.UserId == userId);
            var convId = conversations[userId];

            AnsiConsole.MarkupLine($"[blue][[{userId}]][/] (continuing): {message}");

            var response = await client.ChatAsync("assistant", message, convId);
            if (response != null)
            {
                var preview = response.Message.Length > 100
                    ? response.Message[..100] + "..."
                    : response.Message;
                AnsiConsole.MarkupLine($"[grey]  Response: {Markup.Escape(preview)}[/]");
            }
            AnsiConsole.WriteLine();
        }

        // Step 4: Show all conversations per user
        AnsiConsole.MarkupLine("[yellow]Step 4: Listing conversations per user...[/]");
        AnsiConsole.WriteLine();

        foreach (var client in clients)
        {
            var convs = await client.GetConversationsAsync();
            AnsiConsole.MarkupLine($"[blue][[{client.UserId}]][/] has {convs.Count} conversation(s):");

            foreach (var conv in convs)
            {
                AnsiConsole.MarkupLine($"  - {conv.Id[..8]}... | {conv.AgentKey} | \"{conv.Title}\" | {conv.MessageCount} msgs");
            }
            AnsiConsole.WriteLine();
        }

        // Step 5: Verify user isolation
        AnsiConsole.MarkupLine("[yellow]Step 5: Verifying user isolation...[/]");
        AnsiConsole.WriteLine();

        // Alice tries to access Bob's conversation
        var aliceClient = clients.First(c => c.UserId == "alice");
        var bobConvId = conversations["bob"];

        var alienConv = await aliceClient.GetConversationAsync(bobConvId);
        if (alienConv == null)
        {
            AnsiConsole.MarkupLine("[green]User isolation working correctly - Alice cannot access Bob's conversation[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]WARNING: User isolation failed![/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Demo completed successfully![/]");
    }
    finally
    {
        foreach (var client in clients)
        {
            client.Dispose();
        }
    }
}

// ============================================================================
// INTERACTIVE CHAT
// ============================================================================

async Task RunInteractiveChat(string apiUrl)
{
    AnsiConsole.Write(new Rule("[cyan]Interactive Chat[/]"));
    AnsiConsole.WriteLine();

    // Get user ID
    var userId = AnsiConsole.Prompt(
        new TextPrompt<string>("Enter your user ID:")
            .DefaultValue("demo-user"));

    using var client = new ChatApiClient(apiUrl, userId);

    // Select agent
    var agents = await client.GetAgentsAsync();
    var agentKey = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Select an agent:")
            .AddChoices(agents.Select(a => $"{a.Key} - {a.Name}")))
        .Split(" - ")[0];

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[green]Chatting with {agentKey} as {userId}[/]");
    AnsiConsole.MarkupLine("[grey]Type 'exit' to quit, 'new' for new conversation, 'history' to see messages[/]");
    AnsiConsole.WriteLine();

    string? conversationId = null;

    while (true)
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]You:[/]")
                .AllowEmpty());

        if (string.IsNullOrEmpty(input)) continue;

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        if (input.Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            conversationId = null;
            AnsiConsole.MarkupLine("[yellow]Starting new conversation...[/]");
            continue;
        }

        if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
        {
            if (conversationId != null)
            {
                var conv = await client.GetConversationAsync(conversationId);
                if (conv != null)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[yellow]Conversation: {conv.Conversation.Title}[/]");
                    foreach (var msg in conv.Messages)
                    {
                        var color = msg.Role == "user" ? "blue" : "green";
                        AnsiConsole.MarkupLine($"[{color}]{msg.Role}:[/] {Markup.Escape(msg.Content)}");
                    }
                    AnsiConsole.WriteLine();
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]No active conversation[/]");
            }
            continue;
        }

        try
        {
            var response = await client.ChatAsync(agentKey, input, conversationId);
            if (response != null)
            {
                conversationId = response.ConversationId;
                AnsiConsole.MarkupLine($"[green]Agent:[/] {Markup.Escape(response.Message)}");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
    }
}

// ============================================================================
// MULTI-USER SIMULATION
// ============================================================================

async Task RunMultiUserSimulation(string apiUrl)
{
    AnsiConsole.Write(new Rule("[cyan]Multi-User Simulation[/]"));
    AnsiConsole.WriteLine();

    var userCount = AnsiConsole.Prompt(
        new TextPrompt<int>("Number of users to simulate:")
            .DefaultValue(3)
            .Validate(n => n > 0 && n <= 10
                ? ValidationResult.Success()
                : ValidationResult.Error("Enter 1-10")));

    var messagesPerUser = AnsiConsole.Prompt(
        new TextPrompt<int>("Messages per user:")
            .DefaultValue(3)
            .Validate(n => n > 0 && n <= 10
                ? ValidationResult.Success()
                : ValidationResult.Error("Enter 1-10")));

    AnsiConsole.WriteLine();

    var clients = Enumerable.Range(1, userCount)
        .Select(i => new ChatApiClient(apiUrl, $"user-{i}"))
        .ToList();

    var agents = await clients[0].GetAgentsAsync();
    var agentKeys = agents.Select(a => a.Key).ToArray();

    var testMessages = new[]
    {
        "Hello, can you help me?",
        "What's your specialty?",
        "Tell me something interesting",
        "Can you explain that further?",
        "Thanks for your help!",
        "One more question...",
        "How does that work?",
        "Can you give an example?",
        "What do you recommend?",
        "That's very helpful!"
    };

    try
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[cyan]Simulating users...[/]", maxValue: userCount * messagesPerUser);

                foreach (var client in clients)
                {
                    var agentKey = agentKeys[Random.Shared.Next(agentKeys.Length)];
                    string? convId = null;

                    for (int i = 0; i < messagesPerUser; i++)
                    {
                        var message = testMessages[Random.Shared.Next(testMessages.Length)];

                        try
                        {
                            var response = await client.ChatAsync(agentKey, message, convId);
                            convId = response?.ConversationId;
                        }
                        catch
                        {
                            // Ignore errors in simulation
                        }

                        task.Increment(1);
                    }
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Simulation complete![/]");
        AnsiConsole.WriteLine();

        // Show summary
        var summaryTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("User")
            .AddColumn("Conversations")
            .AddColumn("Total Messages");

        foreach (var client in clients)
        {
            var convs = await client.GetConversationsAsync();
            var totalMsgs = convs.Sum(c => c.MessageCount);
            summaryTable.AddRow(client.UserId, convs.Count.ToString(), totalMsgs.ToString());
        }

        AnsiConsole.Write(summaryTable);
    }
    finally
    {
        foreach (var client in clients)
        {
            client.Dispose();
        }
    }
}

// ============================================================================
// API EXPLORER
// ============================================================================

async Task RunApiExplorer(string apiUrl)
{
    AnsiConsole.Write(new Rule("[cyan]API Explorer[/]"));
    AnsiConsole.WriteLine();

    var userId = AnsiConsole.Prompt(
        new TextPrompt<string>("User ID:")
            .DefaultValue("explorer-user"));

    using var client = new ChatApiClient(apiUrl, userId);

    while (true)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]API Explorer (as {userId})[/]")
                .AddChoices(
                    "1. GET /api/agents - List agents",
                    "2. GET /api/agents/{key} - Get agent info",
                    "3. POST /api/chat/{agent} - Send message",
                    "4. GET /api/conversations - List conversations",
                    "5. GET /api/conversations/{id} - Get conversation",
                    "6. DELETE /api/conversations/{id} - Delete conversation",
                    "7. Back to main menu"));

        AnsiConsole.WriteLine();

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

        try
        {
            switch (choice[0])
            {
                case '1':
                    var agents = await client.GetAgentsAsync();
                    PrintJson(System.Text.Json.JsonSerializer.Serialize(agents, jsonOptions));
                    break;

                case '2':
                    var agentKey = AnsiConsole.Ask<string>("Agent key:");
                    var agent = await client.GetAgentAsync(agentKey);
                    if (agent != null)
                        PrintJson(System.Text.Json.JsonSerializer.Serialize(agent, jsonOptions));
                    else
                        AnsiConsole.MarkupLine("[red]Agent not found[/]");
                    break;

                case '3':
                    var chatAgent = AnsiConsole.Ask<string>("Agent key:");
                    var message = AnsiConsole.Ask<string>("Message:");
                    var convId = AnsiConsole.Prompt(new TextPrompt<string>("Conversation ID (empty for new):").AllowEmpty());
                    var response = await client.ChatAsync(chatAgent, message, string.IsNullOrEmpty(convId) ? null : convId);
                    if (response != null)
                        PrintJson(System.Text.Json.JsonSerializer.Serialize(response, jsonOptions));
                    break;

                case '4':
                    var convs = await client.GetConversationsAsync();
                    PrintJson(System.Text.Json.JsonSerializer.Serialize(convs, jsonOptions));
                    break;

                case '5':
                    var getConvId = AnsiConsole.Ask<string>("Conversation ID:");
                    var conv = await client.GetConversationAsync(getConvId);
                    if (conv != null)
                        PrintJson(System.Text.Json.JsonSerializer.Serialize(conv, jsonOptions));
                    else
                        AnsiConsole.MarkupLine("[red]Conversation not found or access denied[/]");
                    break;

                case '6':
                    var delConvId = AnsiConsole.Ask<string>("Conversation ID to delete:");
                    var deleted = await client.DeleteConversationAsync(delConvId);
                    AnsiConsole.MarkupLine(deleted ? "[green]Deleted successfully[/]" : "[red]Delete failed[/]");
                    break;

                case '7':
                    return;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
    }
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

void PrintJson(string json)
{
    // Escape markup characters in JSON ([ and ] are interpreted as markup by Spectre.Console)
    var escapedJson = Markup.Escape(json);
    var panel = new Panel(escapedJson)
        .Header("[cyan]JSON Response[/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Grey);
    AnsiConsole.Write(panel);
}
