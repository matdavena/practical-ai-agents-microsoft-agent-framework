// ============================================================================
// DependencyInjection Extensions
// ============================================================================
// Extension methods for registering ExpenseTracker services with DI container.
// ============================================================================

using ExpenseTracker.Core.Abstractions;
using ExpenseTracker.Core.Services;
using ExpenseTracker.Infrastructure.Data;
using ExpenseTracker.Infrastructure.Repositories;
using ExpenseTracker.Infrastructure.VectorStore;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace ExpenseTracker.Infrastructure;

/// <summary>
/// Extension methods for registering ExpenseTracker services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds ExpenseTracker infrastructure services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExpenseTrackerInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        // Database connection factory
        var connectionFactory = SqliteConnectionFactory.CreateForFile(databasePath);
        services.AddSingleton<IDbConnectionFactory>(connectionFactory);

        // Database initializer
        services.AddSingleton<DatabaseInitializer>();

        // Repositories
        services.AddSingleton<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IExpenseRepository, ExpenseRepository>();
        services.AddSingleton<IBudgetRepository, BudgetRepository>();

        return services;
    }

    /// <summary>
    /// Adds ExpenseTracker core services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExpenseTrackerServices(this IServiceCollection services)
    {
        // Business services
        services.AddSingleton<IExpenseService, ExpenseService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IBudgetService, BudgetService>();

        return services;
    }

    /// <summary>
    /// Adds all ExpenseTracker services (infrastructure + core).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExpenseTracker(
        this IServiceCollection services,
        string databasePath)
    {
        services.AddExpenseTrackerInfrastructure(databasePath);
        services.AddExpenseTrackerServices();

        return services;
    }

    /// <summary>
    /// Adds Qdrant vector store for semantic search.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="openAIClient">OpenAI client for embeddings.</param>
    /// <param name="qdrantHost">Qdrant host (default: localhost).</param>
    /// <param name="qdrantPort">Qdrant gRPC port (default: 6334).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExpenseTrackerVectorStore(
        this IServiceCollection services,
        OpenAIClient openAIClient,
        string qdrantHost = "localhost",
        int qdrantPort = 6334)
    {
        var vectorStore = new QdrantVectorStore(
            qdrantHost,
            qdrantPort,
            openAIClient);

        services.AddSingleton<IVectorStore>(vectorStore);

        return services;
    }

    /// <summary>
    /// Adds a null vector store (no semantic search).
    /// Use when Qdrant is not available.
    /// </summary>
    public static IServiceCollection AddExpenseTrackerNullVectorStore(this IServiceCollection services)
    {
        services.AddSingleton<IVectorStore, NullVectorStore>();
        return services;
    }

    /// <summary>
    /// Initializes the ExpenseTracker database.
    /// Call this during application startup.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static async Task InitializeExpenseTrackerAsync(this IServiceProvider serviceProvider)
    {
        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();

        // Initialize vector store if available
        var vectorStore = serviceProvider.GetService<IVectorStore>();
        if (vectorStore != null)
        {
            try
            {
                if (await vectorStore.IsAvailableAsync())
                {
                    await vectorStore.InitializeAsync();
                }
            }
            catch
            {
                // Vector store not available, continue without it
                Console.WriteLine("[Warning] Vector store not available, semantic search disabled");
            }
        }
    }
}
