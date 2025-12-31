/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                          CONFIGURATION HELPER                                 ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Helper for managing application configuration.                              ║
 * ║                                                                               ║
 * ║  CONFIGURATION SOURCES (in order of priority):                               ║
 * ║  1. Environment variables (highest priority)                                 ║
 * ║  2. appsettings.json (if present)                                            ║
 * ║                                                                               ║
 * ║  SECURITY:                                                                    ║
 * ║  - API keys should NEVER be committed to code                                ║
 * ║  - Always use environment variables for secrets                              ║
 * ║  - In production, use Azure Key Vault or similar                             ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using Microsoft.Extensions.Configuration;

namespace Common;

/// <summary>
/// Static helper for configuration management.
/// Centralizes access to settings and secrets.
/// </summary>
public static class ConfigurationHelper
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CONSTANTS
     * ═══════════════════════════════════════════════════════════════════════════
     * Environment variable names and configuration keys.
     */

    /// <summary>
    /// Name of the environment variable for the OpenAI API key.
    /// </summary>
    public const string OpenAiApiKeyEnvVar = "OPENAI_API_KEY";

    /// <summary>
    /// Default OpenAI model.
    /// GPT-4o-mini is a good balance between cost and capability for development.
    /// </summary>
    public const string DefaultOpenAiModel = "gpt-4o-mini";

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CONFIGURATION
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Lazy initialization of the configuration.
    /// Created only when needed (lazy loading).
    /// </summary>
    private static readonly Lazy<IConfiguration> _configuration = new(() =>
    {
        /*
         * ConfigurationBuilder is the standard pattern in .NET for
         * building configuration from multiple sources.
         *
         * The order of addition is important:
         * - Sources added later override previous ones
         * - Environment variables have the highest priority
         */
        var builder = new ConfigurationBuilder()
            // First load from JSON file (if it exists)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            // Then environment variables (override JSON)
            .AddEnvironmentVariables();

        return builder.Build();
    });

    /// <summary>
    /// Access to the configuration instance.
    /// </summary>
    public static IConfiguration Configuration => _configuration.Value;

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * OPENAI METHODS
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Gets the OpenAI API key from configuration.
    /// </summary>
    /// <returns>The API key if found.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the API key is not configured.
    /// </exception>
    /// <remarks>
    /// SEARCH ORDER:
    /// 1. Environment variable OPENAI_API_KEY
    /// 2. Key "OpenAI:ApiKey" in appsettings.json
    ///
    /// BEST PRACTICE:
    /// In development: use environment variable
    /// In production: use Azure Key Vault or similar secret manager
    /// </remarks>
    public static string GetOpenAiApiKey()
    {
        // First try the environment variable (most secure method)
        var apiKey = Environment.GetEnvironmentVariable(OpenAiApiKeyEnvVar);

        // If not found, try configuration
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Configuration["OpenAI:ApiKey"];
        }

        // If still not found, throw descriptive error
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"""
                ╔══════════════════════════════════════════════════════════════════╗
                ║                    API KEY NOT CONFIGURED                         ║
                ╠══════════════════════════════════════════════════════════════════╣
                ║  The OpenAI API key was not found.                               ║
                ║                                                                   ║
                ║  SOLUTION:                                                        ║
                ║  Set the environment variable {OpenAiApiKeyEnvVar}                ║
                ║                                                                   ║
                ║  Windows (PowerShell):                                            ║
                ║  $env:OPENAI_API_KEY = "sk-..."                                   ║
                ║                                                                   ║
                ║  Windows (CMD):                                                   ║
                ║  set OPENAI_API_KEY=sk-...                                        ║
                ║                                                                   ║
                ║  Linux/Mac:                                                       ║
                ║  export OPENAI_API_KEY=sk-...                                     ║
                ╚══════════════════════════════════════════════════════════════════╝
                """);
        }

        return apiKey;
    }

    /// <summary>
    /// Gets the OpenAI model to use.
    /// </summary>
    /// <param name="defaultModel">Fallback model if not configured.</param>
    /// <returns>The model name.</returns>
    /// <remarks>
    /// RECOMMENDED MODELS FOR DEVELOPMENT:
    /// - gpt-4o-mini: Economical, fast, good for testing
    /// - gpt-4o: More capable, but more expensive
    /// - gpt-4-turbo: Great for complex tasks
    ///
    /// RECOMMENDED MODELS FOR PRODUCTION:
    /// - gpt-4o: Good balance
    /// - o1/o3: For advanced reasoning
    /// </remarks>
    public static string GetOpenAiModel(string? defaultModel = null)
    {
        // First check the environment variable
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL");

        // Then the configuration
        if (string.IsNullOrWhiteSpace(model))
        {
            model = Configuration["OpenAI:Model"];
        }

        // Finally the default
        return model ?? defaultModel ?? DefaultOpenAiModel;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * UTILITY METHODS
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Checks if the OpenAI API key is configured.
    /// Useful for preventive checks without throwing exceptions.
    /// </summary>
    public static bool IsOpenAiConfigured()
    {
        var apiKey = Environment.GetEnvironmentVariable(OpenAiApiKeyEnvVar)
                     ?? Configuration["OpenAI:ApiKey"];

        return !string.IsNullOrWhiteSpace(apiKey);
    }

    /// <summary>
    /// Masks an API key for safe display in logs.
    /// Shows only the first and last 4 characters.
    /// </summary>
    /// <param name="apiKey">The API key to mask.</param>
    /// <returns>The masked API key (e.g., "sk-a...xyz1").</returns>
    public static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 12)
        {
            return "***";
        }

        // Show only prefix and suffix
        var prefix = apiKey[..4];
        var suffix = apiKey[^4..];
        return $"{prefix}...{suffix}";
    }

    /// <summary>
    /// Gets a dictionary with the current configuration (for debug/display).
    /// API keys are masked for security.
    /// </summary>
    public static Dictionary<string, string> GetDisplayConfiguration()
    {
        var apiKey = IsOpenAiConfigured()
            ? MaskApiKey(GetOpenAiApiKey())
            : "[NOT CONFIGURED]";

        return new Dictionary<string, string>
        {
            ["OpenAI API Key"] = apiKey,
            ["OpenAI Model"] = GetOpenAiModel(),
            ["API Key Source"] = Environment.GetEnvironmentVariable(OpenAiApiKeyEnvVar) != null
                ? "Environment variable"
                : "appsettings.json"
        };
    }
}
