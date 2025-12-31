// ============================================================================
// 10. MCP CUSTOM SERVER
// FILE: Program.cs (Server)
// ============================================================================
//
// OBJECTIVE:
// Create a custom MCP Server in C# that exposes tools for calculations and strings.
//
// ARCHITECTURE:
//
//    ┌─────────────────────────────────────────────────────────────────────┐
//    │                    MCP SERVER (this project)                        │
//    │                                                                     │
//    │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                │
//    │   │ Calculator  │  │   String    │  │   (Others)  │                │
//    │   │   Tools     │  │   Tools     │  │   Tools     │                │
//    │   └─────────────┘  └─────────────┘  └─────────────┘                │
//    │                          │                                         │
//    │                    ┌─────┴─────┐                                   │
//    │                    │ MCP HTTP  │                                   │
//    │                    │ Transport │                                   │
//    │                    └─────┬─────┘                                   │
//    └──────────────────────────┼─────────────────────────────────────────┘
//                               │
//                          HTTP/SSE
//                               │
//                               ▼
//                    ┌─────────────────────┐
//                    │   MCP Client        │
//                    │   (AI Agent)        │
//                    └─────────────────────┘
//
// ENDPOINT:
//   - http://localhost:5100/mcp - MCP endpoint for clients
//
// RUN WITH: dotnet run --project core/10.MCP.CustomServer/Server
// ============================================================================

using Server.Tools;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// MCP SERVER CONFIGURATION
// ============================================================================
//
// AddMcpServer() registers MCP services in the DI container
// WithHttpTransport() enables HTTP transport (Server-Sent Events)
// WithTools<T>() registers classes that contain tools
//
// Each class with [McpServerToolType] exposes its [McpServerTool] methods
// as tools accessible via MCP protocol.

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<CalculatorTools>()    // Mathematical tools
    .WithTools<StringTools>();       // String tools

// Configure the port
builder.WebHost.UseUrls("http://localhost:5100");

var app = builder.Build();

// ============================================================================
// MCP ENDPOINT
// ============================================================================
// MapMcp() exposes the /mcp endpoint that clients will use to connect.
// The MCP protocol over HTTP uses Server-Sent Events (SSE) for streaming.

app.MapMcp("/mcp");

// Health check endpoint
app.MapGet("/", () => Results.Ok(new
{
    name = "MCP Custom Server",
    version = "1.0.0",
    description = "MCP server with tools for calculations and string manipulation",
    mcpEndpoint = "/mcp",
    tools = new[]
    {
        "add", "subtract", "multiply", "divide",
        "power", "sqrt", "percentage", "average",
        "reverse_string", "to_uppercase", "to_lowercase", "to_title_case",
        "count_chars", "find_replace", "extract_numbers",
        "slugify", "truncate"
    }
}));

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║           MCP CUSTOM SERVER - Learning Agent Framework       ║");
Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
Console.WriteLine("║  MCP Server listening on:  http://localhost:5100             ║");
Console.WriteLine("║  MCP Endpoint:             http://localhost:5100/mcp         ║");
Console.WriteLine("║                                                              ║");
Console.WriteLine("║  Available tools:                                            ║");
Console.WriteLine("║  - Calculator: add, subtract, multiply, divide, power, etc.  ║");
Console.WriteLine("║  - String: reverse, uppercase, count_chars, slugify, etc.    ║");
Console.WriteLine("║                                                              ║");
Console.WriteLine("║  Press Ctrl+C to terminate                                   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

app.Run();
