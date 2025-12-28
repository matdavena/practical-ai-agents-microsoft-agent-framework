/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                          CONFIGURATION HELPER                                 ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Helper per la gestione della configurazione dell'applicazione.              ║
 * ║                                                                               ║
 * ║  FONTI DI CONFIGURAZIONE (in ordine di priorità):                            ║
 * ║  1. Variabili d'ambiente (più alta priorità)                                 ║
 * ║  2. appsettings.json (se presente)                                           ║
 * ║                                                                               ║
 * ║  SICUREZZA:                                                                   ║
 * ║  - Le API key NON devono MAI essere committate nel codice                    ║
 * ║  - Usare sempre variabili d'ambiente per i secrets                           ║
 * ║  - In produzione, usare Azure Key Vault o simili                             ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using Microsoft.Extensions.Configuration;

namespace Common;

/// <summary>
/// Helper statico per la gestione della configurazione.
/// Centralizza l'accesso a settings e secrets.
/// </summary>
public static class ConfigurationHelper
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * COSTANTI
     * ═══════════════════════════════════════════════════════════════════════════
     * Nomi delle variabili d'ambiente e chiavi di configurazione.
     */

    /// <summary>
    /// Nome della variabile d'ambiente per la API key di OpenAI.
    /// </summary>
    public const string OpenAiApiKeyEnvVar = "OPENAI_API_KEY";

    /// <summary>
    /// Modello OpenAI di default.
    /// GPT-4o-mini è un buon compromesso tra costi e capacità per lo sviluppo.
    /// </summary>
    public const string DefaultOpenAiModel = "gpt-4o-mini";

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CONFIGURAZIONE
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Lazy initialization della configurazione.
    /// Viene creata solo quando serve (lazy loading).
    /// </summary>
    private static readonly Lazy<IConfiguration> _configuration = new(() =>
    {
        /*
         * ConfigurationBuilder è il pattern standard in .NET per
         * costruire la configurazione da multiple fonti.
         *
         * L'ordine di aggiunta è importante:
         * - Le fonti aggiunte dopo sovrascrivono quelle precedenti
         * - Le variabili d'ambiente hanno la priorità più alta
         */
        var builder = new ConfigurationBuilder()
            // Prima carichiamo da file JSON (se esiste)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            // Poi le variabili d'ambiente (sovrascrivono il JSON)
            .AddEnvironmentVariables();

        return builder.Build();
    });

    /// <summary>
    /// Accesso all'istanza di configurazione.
    /// </summary>
    public static IConfiguration Configuration => _configuration.Value;

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * METODI PER OPENAI
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Ottiene la API key di OpenAI dalla configurazione.
    /// </summary>
    /// <returns>La API key se trovata.</returns>
    /// <exception cref="InvalidOperationException">
    /// Lanciata se la API key non è configurata.
    /// </exception>
    /// <remarks>
    /// ORDINE DI RICERCA:
    /// 1. Variabile d'ambiente OPENAI_API_KEY
    /// 2. Chiave "OpenAI:ApiKey" in appsettings.json
    ///
    /// BEST PRACTICE:
    /// In sviluppo: usare variabile d'ambiente
    /// In produzione: usare Azure Key Vault o simili secret manager
    /// </remarks>
    public static string GetOpenAiApiKey()
    {
        // Prima proviamo la variabile d'ambiente (metodo più sicuro)
        var apiKey = Environment.GetEnvironmentVariable(OpenAiApiKeyEnvVar);

        // Se non trovata, proviamo la configurazione
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Configuration["OpenAI:ApiKey"];
        }

        // Se ancora non trovata, errore esplicativo
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"""
                ╔══════════════════════════════════════════════════════════════════╗
                ║                    API KEY NON CONFIGURATA                        ║
                ╠══════════════════════════════════════════════════════════════════╣
                ║  La API key di OpenAI non è stata trovata.                        ║
                ║                                                                   ║
                ║  SOLUZIONE:                                                       ║
                ║  Imposta la variabile d'ambiente {OpenAiApiKeyEnvVar}             ║
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
    /// Ottiene il modello OpenAI da usare.
    /// </summary>
    /// <param name="defaultModel">Modello di fallback se non configurato.</param>
    /// <returns>Il nome del modello.</returns>
    /// <remarks>
    /// MODELLI CONSIGLIATI PER LO SVILUPPO:
    /// - gpt-4o-mini: Economico, veloce, buono per test
    /// - gpt-4o: Più capace, ma più costoso
    /// - gpt-4-turbo: Ottimo per task complessi
    ///
    /// MODELLI CONSIGLIATI PER PRODUZIONE:
    /// - gpt-4o: Buon bilanciamento
    /// - o1/o3: Per reasoning avanzato
    /// </remarks>
    public static string GetOpenAiModel(string? defaultModel = null)
    {
        // Prima controlliamo la variabile d'ambiente
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL");

        // Poi la configurazione
        if (string.IsNullOrWhiteSpace(model))
        {
            model = Configuration["OpenAI:Model"];
        }

        // Infine il default
        return model ?? defaultModel ?? DefaultOpenAiModel;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * METODI DI UTILITÀ
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Verifica se la API key di OpenAI è configurata.
    /// Utile per controlli preventivi senza lanciare eccezioni.
    /// </summary>
    public static bool IsOpenAiConfigured()
    {
        var apiKey = Environment.GetEnvironmentVariable(OpenAiApiKeyEnvVar)
                     ?? Configuration["OpenAI:ApiKey"];

        return !string.IsNullOrWhiteSpace(apiKey);
    }

    /// <summary>
    /// Maschera una API key per la visualizzazione sicura nei log.
    /// Mostra solo i primi e ultimi 4 caratteri.
    /// </summary>
    /// <param name="apiKey">La API key da mascherare.</param>
    /// <returns>La API key mascherata (es. "sk-a...xyz1").</returns>
    public static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 12)
        {
            return "***";
        }

        // Mostra solo prefix e suffix
        var prefix = apiKey[..4];
        var suffix = apiKey[^4..];
        return $"{prefix}...{suffix}";
    }

    /// <summary>
    /// Ottiene un dizionario con la configurazione corrente (per debug/display).
    /// Le API key sono mascherate per sicurezza.
    /// </summary>
    public static Dictionary<string, string> GetDisplayConfiguration()
    {
        var apiKey = IsOpenAiConfigured()
            ? MaskApiKey(GetOpenAiApiKey())
            : "[NON CONFIGURATA]";

        return new Dictionary<string, string>
        {
            ["OpenAI API Key"] = apiKey,
            ["OpenAI Model"] = GetOpenAiModel(),
            ["Fonte API Key"] = Environment.GetEnvironmentVariable(OpenAiApiKeyEnvVar) != null
                ? "Variabile d'ambiente"
                : "appsettings.json"
        };
    }
}
