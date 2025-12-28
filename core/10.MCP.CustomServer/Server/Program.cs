// ============================================================================
// 10. MCP CUSTOM SERVER
// FILE: Program.cs (Server)
// ============================================================================
//
// OBIETTIVO:
// Creare un MCP Server custom in C# che espone tool per calcoli e stringhe.
//
// ARCHITETTURA:
//
//    ┌─────────────────────────────────────────────────────────────────────┐
//    │                    MCP SERVER (questo progetto)                     │
//    │                                                                     │
//    │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                │
//    │   │ Calculator  │  │   String    │  │   (Altri)   │                │
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
//                    │   (Agente AI)       │
//                    └─────────────────────┘
//
// ENDPOINT:
//   - http://localhost:5100/mcp - Endpoint MCP per i client
//
// ESEGUI CON: dotnet run --project core/10.MCP.CustomServer/Server
// ============================================================================

using Server.Tools;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURAZIONE MCP SERVER
// ============================================================================
//
// AddMcpServer() registra i servizi MCP nel container DI
// WithHttpTransport() abilita il trasporto HTTP (Server-Sent Events)
// WithTools<T>() registra le classi che contengono tool
//
// Ogni classe con [McpServerToolType] espone i suoi metodi [McpServerTool]
// come tool accessibili via protocollo MCP.

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<CalculatorTools>()    // Tool matematici
    .WithTools<StringTools>();       // Tool per stringhe

// Configura la porta
builder.WebHost.UseUrls("http://localhost:5100");

var app = builder.Build();

// ============================================================================
// ENDPOINT MCP
// ============================================================================
// MapMcp() espone l'endpoint /mcp che i client useranno per connettersi.
// Il protocollo MCP su HTTP usa Server-Sent Events (SSE) per streaming.

app.MapMcp("/mcp");

// Endpoint di health check
app.MapGet("/", () => Results.Ok(new
{
    name = "MCP Custom Server",
    version = "1.0.0",
    description = "Server MCP con tool per calcoli e manipolazione stringhe",
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
Console.WriteLine("║  Server MCP in ascolto su: http://localhost:5100             ║");
Console.WriteLine("║  Endpoint MCP:             http://localhost:5100/mcp         ║");
Console.WriteLine("║                                                              ║");
Console.WriteLine("║  Tool disponibili:                                           ║");
Console.WriteLine("║  - Calculator: add, subtract, multiply, divide, power, etc.  ║");
Console.WriteLine("║  - String: reverse, uppercase, count_chars, slugify, etc.    ║");
Console.WriteLine("║                                                              ║");
Console.WriteLine("║  Premi Ctrl+C per terminare                                  ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

app.Run();
