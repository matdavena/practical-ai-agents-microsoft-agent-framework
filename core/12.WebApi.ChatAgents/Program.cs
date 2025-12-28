// ============================================================================
// 12. WEB API - CHAT AGENTS
// ============================================================================
// This project demonstrates integrating Microsoft Agent Framework with:
//
// 1. DEPENDENCY INJECTION (DI)
//    - ASP.NET Core's built-in DI container
//    - Service registration patterns (Singleton, Scoped, Transient)
//    - Resolving dependencies in controllers
//
// 2. KEYED SERVICES (.NET 8+)
//    - Registering multiple implementations of the same interface
//    - Using service keys to distinguish between agents
//    - Resolving keyed services at runtime
//
// 3. PER-USER CONVERSATION MANAGEMENT
//    - Thread state serialization and deserialization
//    - Conversation persistence for resumable chats
//    - User isolation (each user sees only their conversations)
//
// 4. CHATGPT-STYLE REST API
//    - OpenAPI/Swagger documentation
//    - Standard REST patterns
//    - React/frontend-friendly endpoints
// ============================================================================

using OpenAI;
using OpenAI.Chat;
using Microsoft.Agents.AI;
using WebApi.ChatAgents.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURATION
// ============================================================================
// Load settings from environment variables and configuration files

var openAiApiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException(
        "OpenAI API key not configured. Set OPENAI_API_KEY environment variable.");

var openAiModel = builder.Configuration["OpenAI:Model"]
    ?? Environment.GetEnvironmentVariable("OPENAI_MODEL")
    ?? "gpt-4o-mini";

// ============================================================================
// DEPENDENCY INJECTION SETUP
// ============================================================================

// ----------------------------------------------------------------------------
// 1. OpenAI Client (Singleton)
// ----------------------------------------------------------------------------
// The OpenAI client is expensive to create and thread-safe, so we register
// it as a Singleton. All agents will share the same client instance.
builder.Services.AddSingleton(new OpenAIClient(openAiApiKey));

// ----------------------------------------------------------------------------
// 2. Conversation Store (Singleton)
// ----------------------------------------------------------------------------
// In-memory store for demo. In production, replace with:
// - AddSingleton<IConversationStore, SqlConversationStore>()
// - AddSingleton<IConversationStore, RedisConversationStore>()
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();

// ----------------------------------------------------------------------------
// 3. KEYED SERVICES - Multiple Agents with Different Configurations
// ----------------------------------------------------------------------------
// Keyed Services (.NET 8+) allow registering multiple implementations
// of the same type with different "keys". This is perfect for agents
// with different system prompts or configurations.
//
// Pattern: AddKeyedSingleton<TService>(key, factory)
// Usage:   serviceProvider.GetRequiredKeyedService<TService>(key)

// Agent 1: General Assistant
builder.Services.AddKeyedSingleton<ChatClientAgent>("assistant", (sp, key) =>
{
    var client = sp.GetRequiredService<OpenAIClient>();

    return client
        .GetChatClient(openAiModel)
        .CreateAIAgent(
            instructions: """
                You are a helpful, friendly AI assistant.
                - Be concise but thorough in your responses
                - Use clear, simple language
                - If you don't know something, admit it honestly
                - Format responses nicely with markdown when appropriate
                """,
            name: "Assistant");
});

// Agent 2: Code Assistant
builder.Services.AddKeyedSingleton<ChatClientAgent>("coder", (sp, key) =>
{
    var client = sp.GetRequiredService<OpenAIClient>();

    return client
        .GetChatClient(openAiModel)
        .CreateAIAgent(
            instructions: """
                You are an expert programmer and code assistant.

                Your specialties:
                - Writing clean, efficient, well-documented code
                - Explaining complex programming concepts
                - Debugging and code review
                - Best practices and design patterns
                - Multiple programming languages (C#, Python, JavaScript, etc.)

                Guidelines:
                - Always include code examples when helpful
                - Explain your reasoning and trade-offs
                - Follow language-specific conventions and best practices
                - Use proper code formatting with syntax highlighting
                """,
            name: "Coder");
});

// Agent 3: Translator
builder.Services.AddKeyedSingleton<ChatClientAgent>("translator", (sp, key) =>
{
    var client = sp.GetRequiredService<OpenAIClient>();

    return client
        .GetChatClient(openAiModel)
        .CreateAIAgent(
            instructions: """
                You are an expert multilingual translator.

                Your capabilities:
                - Translate text between any languages
                - Preserve tone, style, and nuance
                - Handle idioms and cultural references appropriately
                - Provide alternative translations when relevant

                Format:
                - If language is not specified, ask for clarification
                - Provide the translation clearly marked
                - Add notes about nuances or alternatives when helpful
                - For ambiguous phrases, explain different interpretations
                """,
            name: "Translator");
});

// ----------------------------------------------------------------------------
// 4. ASP.NET Core Services
// ----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Chat Agents API",
        Version = "v1",
        Description = """
            REST API for interacting with AI chat agents.

            ## Features
            - Multiple specialized agents (assistant, coder, translator)
            - Per-user conversation management
            - Persistent conversations (resumable chats)
            - ChatGPT-style API design

            ## Authentication
            For demo purposes, use the `X-User-Id` header to identify users.
            In production, replace with proper JWT/OAuth authentication.
            """
    });
});

// CORS for React/frontend applications
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================================
// BUILD AND CONFIGURE PIPELINE
// ============================================================================

var app = builder.Build();

// Swagger UI (available in all environments for demo)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat Agents API v1");
    options.RoutePrefix = string.Empty; // Swagger at root URL
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// ============================================================================
// STARTUP INFORMATION
// ============================================================================

var urls = app.Urls.Count > 0
    ? string.Join(", ", app.Urls)
    : "http://localhost:5000";

Console.WriteLine("""

    ╔══════════════════════════════════════════════════════════════════════════╗
    ║                      12. WEB API - CHAT AGENTS                           ║
    ╠══════════════════════════════════════════════════════════════════════════╣
    ║  This project demonstrates:                                              ║
    ║  • Dependency Injection with Microsoft Agent Framework                   ║
    ║  • Keyed Services for multiple agent configurations                      ║
    ║  • Per-user conversation management                                      ║
    ║  • ChatGPT-style REST API endpoints                                      ║
    ╠══════════════════════════════════════════════════════════════════════════╣
    ║  AVAILABLE AGENTS:                                                       ║
    ║  • assistant - General-purpose helpful AI assistant                      ║
    ║  • coder     - Specialized programming and code assistant                ║
    ║  • translator - Multilingual translation assistant                       ║
    ╠══════════════════════════════════════════════════════════════════════════╣
    ║  API ENDPOINTS:                                                          ║
    ║  GET  /api/agents              - List available agents                   ║
    ║  POST /api/chat/{agentKey}     - Send message to agent                   ║
    ║  GET  /api/conversations       - List user's conversations               ║
    ║  GET  /api/conversations/{id}  - Get conversation with history           ║
    ║  DELETE /api/conversations/{id} - Delete a conversation                  ║
    ╠══════════════════════════════════════════════════════════════════════════╣
    ║  Swagger UI: {urls}/                                                     ║
    ╚══════════════════════════════════════════════════════════════════════════╝

    """.Replace("{urls}", urls));

app.Run();
