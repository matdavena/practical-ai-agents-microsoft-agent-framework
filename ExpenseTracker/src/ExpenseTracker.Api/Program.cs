// ============================================================================
// EXPENSE TRACKER - Web API
// ============================================================================
// RESTful API for the AI-powered expense tracker.
// Provides endpoints for expenses, categories, reports, and AI chat.
//
// BOOK CHAPTER NOTE:
// This demonstrates:
// 1. ASP.NET Core Web API with OpenAPI/Swagger
// 2. Dependency Injection with shared services
// 3. RESTful endpoint design
// 4. AI chat endpoint integration
// ============================================================================

using ExpenseTracker.Infrastructure;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURATION
// ============================================================================

var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OPENAI_API_KEY is required");

var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL")
    ?? builder.Configuration["OpenAI:Model"]
    ?? "gpt-4o-mini";

var databasePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ExpenseTracker",
    "expenses.db");

// ============================================================================
// SERVICES
// ============================================================================

// Add Expense Tracker services
builder.Services.AddExpenseTracker(databasePath);

// Add OpenAI client
builder.Services.AddSingleton(new OpenAIClient(openAiApiKey));
builder.Services.AddSingleton(openAiModel);

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Expense Tracker API",
        Version = "v1",
        Description = "AI-powered expense tracking API with natural language support"
    });
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ============================================================================
// INITIALIZATION
// ============================================================================

// Initialize database
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.InitializeExpenseTrackerAsync();
}

// ============================================================================
// MIDDLEWARE PIPELINE
// ============================================================================

// Enable Swagger (always for this demo)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Tracker API v1");
    options.RoutePrefix = string.Empty; // Swagger at root
});

// CORS must be before other middleware
app.UseCors();

// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

// ============================================================================
// RUN
// ============================================================================

Console.WriteLine("============================================");
Console.WriteLine("  EXPENSE TRACKER - Web API");
Console.WriteLine("============================================");
Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"AI Model: {openAiModel}");
Console.WriteLine();
Console.WriteLine("Swagger UI available at the URL shown below");
Console.WriteLine("============================================");

app.Run();
