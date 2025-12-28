/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                    01b. HELLO AGENT - MULTIPLE PROVIDERS                      ║
 * ║            Using Microsoft Agent Framework with Different LLMs                ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                               ║
 * ║  PROJECT GOAL:                                                                ║
 * ║  Demonstrate that Microsoft Agent Framework is provider-agnostic.             ║
 * ║  The same agent code works with OpenAI, Azure, Anthropic, Google, and Ollama. ║
 * ║                                                                               ║
 * ║  WHAT YOU'LL LEARN:                                                           ║
 * ║  1. How to use OpenAI (direct API) - cloud, paid per token                    ║
 * ║  2. How to use Azure OpenAI - enterprise, managed by Azure                    ║
 * ║  3. How to use Anthropic Claude - excellent reasoning                         ║
 * ║  4. How to use Google Gemini - multimodal capabilities                        ║
 * ║  5. How to use Ollama - local models, free, private                           ║
 * ║                                                                               ║
 * ║  KEY CONCEPTS:                                                                ║
 * ║  - Provider abstraction: same ChatClientAgent, different backends             ║
 * ║  - Each provider has unique strengths and trade-offs                          ║
 * ║  - IChatClient is the common interface for all providers                      ║
 * ║                                                                               ║
 * ║  SUPPORTED PROVIDERS:                                                         ║
 * ║  ┌──────────────┬─────────────────┬────────────┬──────────────────┐           ║
 * ║  │ Provider     │ Location        │ Cost       │ Best For         │           ║
 * ║  ├──────────────┼─────────────────┼────────────┼──────────────────┤           ║
 * ║  │ OpenAI       │ Cloud           │ Per token  │ Best models      │           ║
 * ║  │ Azure OpenAI │ Azure Cloud     │ Per token  │ Enterprise       │           ║
 * ║  │ Anthropic    │ Cloud           │ Per token  │ Reasoning        │           ║
 * ║  │ Google Gemini│ Cloud           │ Per token  │ Multimodal       │           ║
 * ║  │ Ollama       │ Local           │ Free       │ Privacy/Offline  │           ║
 * ║  └──────────────┴─────────────────┴────────────┴──────────────────┘           ║
 * ║                                                                               ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.ClientModel;
using System.Text;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Azure.AI.OpenAI;
using Azure.Identity;
using Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mscc.GenerativeAI.Microsoft;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;  // Required for the CreateAIAgent() extension method

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * STEP 0: CONSOLE CONFIGURATION
 * ═══════════════════════════════════════════════════════════════════════════════
 */
Console.OutputEncoding = Encoding.UTF8;

ConsoleHelper.WriteTitle("Hello Agent - Multiple Providers");
ConsoleHelper.WriteSubtitle("Same agent, different LLM backends");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * MAIN MENU
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Let the user choose which provider to test.
 * Each provider has different requirements and configuration.
 */

while (true)
{
    ConsoleHelper.WriteSeparator("Select LLM Provider");

    Console.WriteLine("""
        AVAILABLE PROVIDERS:

        1. OpenAI          - Cloud API (requires OPENAI_API_KEY)
        2. Azure OpenAI    - Azure deployment (requires Azure subscription)
        3. Anthropic Claude- Claude models (requires ANTHROPIC_API_KEY)
        4. Google Gemini   - Gemini models (requires GOOGLE_API_KEY)
        5. Ollama          - Local models (requires Ollama running)
        6. Compare All     - Run the same prompt on all available providers

        0. Exit
        """);
    Console.WriteLine();

    Console.Write("Choice: ");
    var choice = Console.ReadLine();

    Console.WriteLine();

    switch (choice)
    {
        case "1":
            await RunOpenAIDemo();
            break;

        case "2":
            await RunAzureOpenAIDemo();
            break;

        case "3":
            await RunAnthropicDemo();
            break;

        case "4":
            await RunGeminiDemo();
            break;

        case "5":
            await RunOllamaDemo();
            break;

        case "6":
            await CompareAllProviders();
            break;

        case "0":
            goto exit;

        default:
            Console.WriteLine("Invalid choice!");
            break;
    }
}

exit:
ConsoleHelper.WriteSystemMessage("Goodbye!");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * DEMO 1: OPENAI (Direct API)
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * OpenAI is the original provider, offering the most capable models.
 *
 * PROS:
 * - Access to latest models (GPT-4o, o1, etc.)
 * - Best performance and capabilities
 * - Simple setup with just an API key
 *
 * CONS:
 * - Pay per token
 * - Data sent to OpenAI servers
 * - Rate limits on free tier
 *
 * REQUIRED CONFIGURATION:
 * - Environment variable: OPENAI_API_KEY
 * - Optional: OPENAI_MODEL (default: gpt-4o-mini)
 */
async Task RunOpenAIDemo()
{
    ConsoleHelper.WritePanel(
        "OpenAI Provider",
        """
        Using OpenAI's direct API.
        Models: GPT-4o, GPT-4o-mini, o1, o3, etc.
        """
    );

    // Check configuration
    string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        try
        {
            apiKey = ConfigurationHelper.GetOpenAiApiKey();
        }
        catch
        {
            ConsoleHelper.WriteError("OPENAI_API_KEY not configured!");
            ConsoleHelper.WriteInfo("Set environment variable or use user secrets.");
            WaitForKey();
            return;
        }
    }

    string model = ConfigurationHelper.GetOpenAiModel();

    ConsoleHelper.WriteInfo($"Model: {model}");
    ConsoleHelper.WriteInfo("Creating OpenAI agent...");

    /*
     * OPENAI CLIENT CREATION
     *
     * The simplest setup: just pass the API key.
     * The client handles authentication and HTTP communication.
     */
    OpenAIClient openAiClient = new(apiKey);

    ChatClientAgent agent = openAiClient
        .GetChatClient(model)
        .CreateAIAgent(
            instructions: """
                You are a helpful assistant demonstrating the OpenAI provider.
                Keep responses concise (2-3 sentences max).
                Always mention that you're powered by OpenAI.
                """,
            name: "OpenAI-Agent"
        );

    ConsoleHelper.WriteSuccess("Agent created!");

    await RunInteractiveChat(agent, "OpenAI");
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * DEMO 2: AZURE OPENAI
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Azure OpenAI provides the same models as OpenAI but through Azure's
 * enterprise infrastructure.
 *
 * PROS:
 * - Enterprise SLA and compliance (GDPR, HIPAA, etc.)
 * - Azure Active Directory integration
 * - Regional data residency (your data stays in your region)
 * - Managed by your Azure subscription
 *
 * CONS:
 * - Requires Azure subscription
 * - More complex setup (deploy a resource, configure RBAC)
 * - Models may be slightly behind OpenAI's latest
 *
 * REQUIRED CONFIGURATION:
 * - Environment variable: AZURE_OPENAI_ENDPOINT (e.g., https://myresource.openai.azure.com/)
 * - Environment variable: AZURE_OPENAI_DEPLOYMENT (your deployment name)
 * - Authentication: AZURE_OPENAI_API_KEY or Azure CLI login (az login)
 */
async Task RunAzureOpenAIDemo()
{
    ConsoleHelper.WritePanel(
        "Azure OpenAI Provider",
        """
        Using Azure's managed OpenAI service.
        Enterprise-grade with Azure compliance.
        """
    );

    // Check configuration
    string? endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    string? deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
                          ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");
    string? apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

    if (string.IsNullOrWhiteSpace(endpoint))
    {
        ConsoleHelper.WriteError("AZURE_OPENAI_ENDPOINT not configured!");
        ConsoleHelper.WriteInfo("Set environment variable to your Azure OpenAI endpoint.");
        ConsoleHelper.WriteInfo("Example: https://myresource.openai.azure.com/");
        WaitForKey();
        return;
    }

    if (string.IsNullOrWhiteSpace(deploymentName))
    {
        ConsoleHelper.WriteError("AZURE_OPENAI_DEPLOYMENT not configured!");
        ConsoleHelper.WriteInfo("Set environment variable to your model deployment name.");
        WaitForKey();
        return;
    }

    ConsoleHelper.WriteInfo($"Endpoint: {endpoint}");
    ConsoleHelper.WriteInfo($"Deployment: {deploymentName}");

    /*
     * AZURE OPENAI CLIENT CREATION
     *
     * Two authentication options:
     * 1. API Key (simpler, but key can be leaked)
     * 2. Azure Identity (more secure, uses Azure AD)
     *
     * We try API key first, then fall back to Azure CLI credential.
     */
    AzureOpenAIClient azureClient;

    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        ConsoleHelper.WriteInfo("Auth: API Key");
        azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey)
        );
    }
    else
    {
        ConsoleHelper.WriteInfo("Auth: Azure CLI (az login)");
        ConsoleHelper.WriteInfo("Make sure you're logged in with: az login");

        /*
         * AzureCliCredential uses your Azure CLI session.
         * Run 'az login' before using this option.
         *
         * Other options include:
         * - DefaultAzureCredential (tries multiple methods)
         * - ManagedIdentityCredential (for Azure-hosted apps)
         * - EnvironmentCredential (AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID)
         */
        azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureCliCredential()
        );
    }

    ConsoleHelper.WriteInfo("Creating Azure OpenAI agent...");

    ChatClientAgent agent = azureClient
        .GetChatClient(deploymentName)
        .CreateAIAgent(
            instructions: """
                You are a helpful assistant demonstrating the Azure OpenAI provider.
                Keep responses concise (2-3 sentences max).
                Always mention that you're powered by Azure OpenAI.
                """,
            name: "AzureOpenAI-Agent"
        );

    ConsoleHelper.WriteSuccess("Agent created!");

    await RunInteractiveChat(agent, "Azure OpenAI");
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * DEMO 3: OLLAMA (Local Models)
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Ollama runs LLMs locally on your machine. Perfect for development,
 * privacy-sensitive applications, and offline use.
 *
 * PROS:
 * - Completely free (no per-token cost)
 * - Total privacy (data never leaves your machine)
 * - Works offline
 * - Many models: Llama, Gemma, Mistral, Phi, etc.
 *
 * CONS:
 * - Requires local compute (GPU recommended)
 * - Models are less capable than GPT-4
 * - Slower than cloud APIs (depends on hardware)
 *
 * PREREQUISITES:
 * 1. Install Ollama: https://ollama.ai
 * 2. Start Ollama service (runs on http://localhost:11434)
 * 3. Pull a model: ollama pull gemma3:1b (or llama3.2, mistral, etc.)
 *
 * REQUIRED CONFIGURATION:
 * - Environment variable: OLLAMA_MODEL (default: gemma3:1b)
 * - Optional: OLLAMA_ENDPOINT (default: http://localhost:11434)
 */
async Task RunOllamaDemo()
{
    ConsoleHelper.WritePanel(
        "Ollama Provider (Local)",
        """
        Using local LLM via Ollama.
        Free, private, works offline.
        """
    );

    string endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT")
                   ?? "http://localhost:11434";
    string modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL")
                    ?? "gemma3:1b";

    ConsoleHelper.WriteInfo($"Endpoint: {endpoint}");
    ConsoleHelper.WriteInfo($"Model: {modelName}");
    ConsoleHelper.WriteInfo("Connecting to Ollama...");

    /*
     * OLLAMA CLIENT CREATION
     *
     * OllamaApiClient implements IChatClient, which is the interface
     * that Microsoft Agent Framework uses.
     *
     * This is the key to provider abstraction: any IChatClient
     * can be used to create a ChatClientAgent.
     */
    IChatClient ollamaClient;

    try
    {
        ollamaClient = new OllamaApiClient(new Uri(endpoint), modelName);

        // Test connection by listing models
        var ollama = new OllamaApiClient(new Uri(endpoint));
        var models = await ollama.ListLocalModelsAsync();

        ConsoleHelper.WriteSuccess($"Connected! Found {models.Count()} models.");

        // Check if the requested model is available
        if (!models.Any(m => m.Name.StartsWith(modelName.Split(':')[0])))
        {
            ConsoleHelper.WriteWarning($"Model '{modelName}' not found locally.");
            ConsoleHelper.WriteInfo($"Available models: {string.Join(", ", models.Select(m => m.Name))}");
            ConsoleHelper.WriteInfo($"Pull the model with: ollama pull {modelName}");
            WaitForKey();
            return;
        }
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Cannot connect to Ollama: {ex.Message}");
        ConsoleHelper.WriteInfo("Make sure Ollama is running:");
        ConsoleHelper.WriteInfo("1. Install from https://ollama.ai");
        ConsoleHelper.WriteInfo("2. Start Ollama (it runs as a service)");
        ConsoleHelper.WriteInfo($"3. Pull a model: ollama pull {modelName}");
        WaitForKey();
        return;
    }

    ConsoleHelper.WriteInfo("Creating Ollama agent...");

    /*
     * Creating an agent from Ollama is slightly different.
     * Since OllamaApiClient implements IChatClient directly,
     * we wrap it in a ChatClientAgent.
     *
     * Note: We use the ChatClientAgent constructor directly
     * instead of the CreateAIAgent() extension method.
     */
    ChatClientAgent agent = new(
        ollamaClient,
        instructions: """
            You are a helpful assistant running locally via Ollama.
            Keep responses concise (2-3 sentences max).
            Always mention that you're a local model for privacy.
            """,
        name: "Ollama-Agent"
    );

    ConsoleHelper.WriteSuccess("Agent created!");

    await RunInteractiveChat(agent, "Ollama");
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * DEMO 4: ANTHROPIC CLAUDE
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Anthropic's Claude models are known for excellent reasoning and analysis.
 *
 * PROS:
 * - Exceptional reasoning and analytical capabilities
 * - Very long context window (up to 200K tokens)
 * - Strong ethical alignment and safety
 * - Great at following complex instructions
 *
 * CONS:
 * - Pay per token
 * - Fewer integrations than OpenAI
 * - No image generation
 *
 * REQUIRED CONFIGURATION:
 * - Environment variable: ANTHROPIC_API_KEY
 * - Optional: ANTHROPIC_MODEL (default: claude-3-5-haiku-latest)
 */
async Task RunAnthropicDemo()
{
    ConsoleHelper.WritePanel(
        "Anthropic Claude Provider",
        """
        Using Anthropic's Claude models.
        Known for reasoning and analysis.
        """
    );

    string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    string model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL")
                ?? AnthropicModels.Claude35Haiku;

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        ConsoleHelper.WriteError("ANTHROPIC_API_KEY not configured!");
        ConsoleHelper.WriteInfo("Get your API key from: https://console.anthropic.com/");
        WaitForKey();
        return;
    }

    ConsoleHelper.WriteInfo($"Model: {model}");
    ConsoleHelper.WriteInfo("Creating Anthropic client...");

    /*
     * ANTHROPIC CLIENT CREATION
     *
     * Anthropic.SDK provides direct access to Claude models.
     * The client implements IChatClient via the Messages API.
     */
    try
    {
        var anthropicClient = new AnthropicClient(new APIAuthentication(apiKey));

        /*
         * We need to build an IChatClient from the Anthropic client.
         * The AsBuilder().Build() pattern creates an IChatClient wrapper.
         */
        IChatClient chatClient = anthropicClient.Messages
            .AsBuilder()
            .Build();

        /*
         * ChatClientAgentRunOptions allows us to specify model-specific options
         * that are passed to the provider on each request.
         */
        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            ModelId = model,
            MaxOutputTokens = 1024
        });

        ChatClientAgent agent = new(
            chatClient,
            instructions: """
                You are a helpful assistant demonstrating Anthropic Claude.
                Keep responses concise (2-3 sentences max).
                Always mention that you're powered by Anthropic Claude.
                """,
            name: "Claude-Agent"
        );

        ConsoleHelper.WriteSuccess("Agent created!");

        await RunInteractiveChatWithOptions(agent, "Anthropic Claude", runOptions);
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error creating Anthropic client: {ex.Message}");
        WaitForKey();
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * DEMO 5: GOOGLE GEMINI
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Google's Gemini models offer strong multimodal capabilities.
 *
 * PROS:
 * - Excellent multimodal support (text, images, video, audio)
 * - Gemini Flash is very fast and economical
 * - Good integration with Google Cloud services
 * - Generous free tier
 *
 * CONS:
 * - Slightly less capable than GPT-4 for complex reasoning
 * - Less widespread adoption than OpenAI
 *
 * REQUIRED CONFIGURATION:
 * - Environment variable: GOOGLE_API_KEY (from Google AI Studio)
 * - Optional: GOOGLE_MODEL (default: gemini-2.0-flash)
 */
async Task RunGeminiDemo()
{
    ConsoleHelper.WritePanel(
        "Google Gemini Provider",
        """
        Using Google's Gemini models.
        Strong multimodal capabilities.
        """
    );

    string? apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    string model = Environment.GetEnvironmentVariable("GOOGLE_MODEL")
                ?? "gemini-2.0-flash";

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        ConsoleHelper.WriteError("GOOGLE_API_KEY not configured!");
        ConsoleHelper.WriteInfo("Get your API key from: https://aistudio.google.com/apikey");
        WaitForKey();
        return;
    }

    ConsoleHelper.WriteInfo($"Model: {model}");
    ConsoleHelper.WriteInfo("Creating Gemini client...");

    /*
     * GOOGLE GEMINI CLIENT CREATION
     *
     * We use the Mscc.GenerativeAI.Microsoft package which provides
     * an IChatClient implementation for Gemini.
     */
    try
    {
        IChatClient geminiClient = new GeminiChatClient(apiKey: apiKey, model: model);

        ChatClientAgent agent = new(
            geminiClient,
            instructions: """
                You are a helpful assistant demonstrating Google Gemini.
                Keep responses concise (2-3 sentences max).
                Always mention that you're powered by Google Gemini.
                """,
            name: "Gemini-Agent"
        );

        ConsoleHelper.WriteSuccess("Agent created!");

        await RunInteractiveChat(agent, "Google Gemini");
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error creating Gemini client: {ex.Message}");
        WaitForKey();
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * DEMO 6: COMPARE ALL PROVIDERS
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Run the same prompt on all available providers to compare:
 * - Response quality
 * - Response time
 * - Style differences
 */
async Task CompareAllProviders()
{
    ConsoleHelper.WritePanel(
        "Compare All Providers",
        "Running the same prompt on all available providers."
    );

    const string testPrompt = "Explain what makes you unique as an AI assistant in exactly 2 sentences.";

    Console.WriteLine($"Test prompt: \"{testPrompt}\"");
    Console.WriteLine();

    // Try OpenAI
    ConsoleHelper.WriteSeparator("OpenAI Response");
    try
    {
        string apiKey = ConfigurationHelper.GetOpenAiApiKey();
        string model = ConfigurationHelper.GetOpenAiModel();

        var agent = new OpenAIClient(apiKey)
            .GetChatClient(model)
            .CreateAIAgent(name: "OpenAI");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await agent.RunAsync(testPrompt);
        sw.Stop();

        ConsoleHelper.WriteAgentMessage(response.ToString());
        ConsoleHelper.WriteInfo($"Time: {sw.ElapsedMilliseconds}ms");
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteWarning($"OpenAI not available: {ex.Message}");
    }

    Console.WriteLine();

    // Try Azure OpenAI
    ConsoleHelper.WriteSeparator("Azure OpenAI Response");
    string? azureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    string? azureDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
    string? azureKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

    if (!string.IsNullOrWhiteSpace(azureEndpoint) && !string.IsNullOrWhiteSpace(azureDeployment))
    {
        try
        {
            AzureOpenAIClient azureClient = !string.IsNullOrWhiteSpace(azureKey)
                ? new AzureOpenAIClient(new Uri(azureEndpoint), new ApiKeyCredential(azureKey))
                : new AzureOpenAIClient(new Uri(azureEndpoint), new AzureCliCredential());

            var agent = azureClient
                .GetChatClient(azureDeployment)
                .CreateAIAgent(name: "AzureOpenAI");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await agent.RunAsync(testPrompt);
            sw.Stop();

            ConsoleHelper.WriteAgentMessage(response.ToString());
            ConsoleHelper.WriteInfo($"Time: {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Azure OpenAI error: {ex.Message}");
        }
    }
    else
    {
        ConsoleHelper.WriteWarning("Azure OpenAI not configured (AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_DEPLOYMENT)");
    }

    Console.WriteLine();

    // Try Anthropic Claude
    ConsoleHelper.WriteSeparator("Anthropic Claude Response");
    string? anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    string anthropicModel = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? AnthropicModels.Claude35Haiku;

    if (!string.IsNullOrWhiteSpace(anthropicKey))
    {
        try
        {
            var anthropicClient = new AnthropicClient(new APIAuthentication(anthropicKey));
            IChatClient chatClient = anthropicClient.Messages.AsBuilder().Build();

            var runOptions = new ChatClientAgentRunOptions(new ChatOptions
            {
                ModelId = anthropicModel,
                MaxOutputTokens = 256
            });

            ChatClientAgent agent = new(chatClient, name: "Claude");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await agent.RunAsync(testPrompt, options: runOptions);
            sw.Stop();

            ConsoleHelper.WriteAgentMessage(response.ToString());
            ConsoleHelper.WriteInfo($"Time: {sw.ElapsedMilliseconds}ms | Model: {anthropicModel}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Anthropic error: {ex.Message}");
        }
    }
    else
    {
        ConsoleHelper.WriteWarning("Anthropic not configured (ANTHROPIC_API_KEY)");
    }

    Console.WriteLine();

    // Try Google Gemini
    ConsoleHelper.WriteSeparator("Google Gemini Response");
    string? googleKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    string googleModel = Environment.GetEnvironmentVariable("GOOGLE_MODEL") ?? "gemini-2.0-flash";

    if (!string.IsNullOrWhiteSpace(googleKey))
    {
        try
        {
            IChatClient geminiClient = new GeminiChatClient(apiKey: googleKey, model: googleModel);
            ChatClientAgent agent = new(geminiClient, name: "Gemini");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await agent.RunAsync(testPrompt);
            sw.Stop();

            ConsoleHelper.WriteAgentMessage(response.ToString());
            ConsoleHelper.WriteInfo($"Time: {sw.ElapsedMilliseconds}ms | Model: {googleModel}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Gemini error: {ex.Message}");
        }
    }
    else
    {
        ConsoleHelper.WriteWarning("Google Gemini not configured (GOOGLE_API_KEY)");
    }

    Console.WriteLine();

    // Try Ollama
    ConsoleHelper.WriteSeparator("Ollama Response");
    string ollamaEndpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
    string ollamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "gemma3:1b";

    try
    {
        IChatClient ollamaClient = new OllamaApiClient(new Uri(ollamaEndpoint), ollamaModel);
        ChatClientAgent agent = new(ollamaClient, name: "Ollama");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await agent.RunAsync(testPrompt);
        sw.Stop();

        ConsoleHelper.WriteAgentMessage(response.ToString());
        ConsoleHelper.WriteInfo($"Time: {sw.ElapsedMilliseconds}ms | Model: {ollamaModel}");
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteWarning($"Ollama not available: {ex.Message}");
    }

    Console.WriteLine();
    WaitForKey();
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * HELPER: INTERACTIVE CHAT
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Common chat loop used by all providers.
 * Demonstrates that the agent interface is identical regardless of provider.
 */
async Task RunInteractiveChat(ChatClientAgent agent, string providerName)
{
    ConsoleHelper.WriteSeparator($"Chat with {providerName}");
    ConsoleHelper.WriteInfo("Type 'exit' to return to menu, 'clear' to reset conversation.");
    Console.WriteLine();

    AgentThread thread = agent.GetNewThread();

    while (true)
    {
        string userInput = ConsoleHelper.AskInput();

        if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        if (userInput.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            thread = agent.GetNewThread();
            ConsoleHelper.WriteSystemMessage("Conversation reset!");
            continue;
        }

        if (string.IsNullOrWhiteSpace(userInput))
            continue;

        ConsoleHelper.WriteUserMessage(userInput);
        ConsoleHelper.WriteAgentHeader();

        try
        {
            await foreach (var update in agent.RunStreamingAsync(userInput, thread))
            {
                ConsoleHelper.WriteStreamChunk(update.ToString());
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
        }

        ConsoleHelper.EndStreamLine();
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * HELPER: INTERACTIVE CHAT WITH OPTIONS (for Anthropic)
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Some providers like Anthropic require additional options on each request.
 */
async Task RunInteractiveChatWithOptions(ChatClientAgent agent, string providerName, ChatClientAgentRunOptions options)
{
    ConsoleHelper.WriteSeparator($"Chat with {providerName}");
    ConsoleHelper.WriteInfo("Type 'exit' to return to menu, 'clear' to reset conversation.");
    Console.WriteLine();

    AgentThread thread = agent.GetNewThread();

    while (true)
    {
        string userInput = ConsoleHelper.AskInput();

        if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        if (userInput.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            thread = agent.GetNewThread();
            ConsoleHelper.WriteSystemMessage("Conversation reset!");
            continue;
        }

        if (string.IsNullOrWhiteSpace(userInput))
            continue;

        ConsoleHelper.WriteUserMessage(userInput);
        ConsoleHelper.WriteAgentHeader();

        try
        {
            await foreach (var update in agent.RunStreamingAsync(userInput, thread, options))
            {
                ConsoleHelper.WriteStreamChunk(update.ToString());
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
        }

        ConsoleHelper.EndStreamLine();
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * HELPER: WAIT FOR KEY
 * ═══════════════════════════════════════════════════════════════════════════════
 */
void WaitForKey()
{
    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * SUMMARY
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * IN THIS PROJECT WE LEARNED:
 *
 * 1. PROVIDER ABSTRACTION
 *    - ChatClientAgent works identically with any IChatClient
 *    - Same code, different backends
 *    - Switch providers without changing agent logic
 *
 * 2. OPENAI (Direct)
 *    - new OpenAIClient(apiKey)
 *    - Best models (GPT-4o, o1, o3)
 *    - Simple setup
 *
 * 3. AZURE OPENAI
 *    - new AzureOpenAIClient(endpoint, credential)
 *    - Enterprise compliance (GDPR, HIPAA)
 *    - Azure AD integration
 *
 * 4. ANTHROPIC CLAUDE
 *    - new AnthropicClient(apiAuthentication)
 *    - Excellent reasoning and analysis
 *    - Long context (200K tokens)
 *
 * 5. GOOGLE GEMINI
 *    - new GeminiChatClient(apiKey, model)
 *    - Strong multimodal (text, images, video)
 *    - Fast and economical (Flash)
 *
 * 6. OLLAMA (Local)
 *    - new OllamaApiClient(endpoint, model)
 *    - Free, private, offline
 *    - Requires local setup
 *
 * CHOOSING A PROVIDER:
 * ┌──────────────┬───────────────────────────────────────────────────────────┐
 * │ Use Case     │ Recommended Provider                                      │
 * ├──────────────┼───────────────────────────────────────────────────────────┤
 * │ Development  │ Ollama (free) or Gemini Flash (cheap)                     │
 * │ Production   │ Azure OpenAI (enterprise) or OpenAI (startup)             │
 * │ Reasoning    │ Anthropic Claude (best analysis)                          │
 * │ Multimodal   │ Google Gemini (images, video, audio)                      │
 * │ Privacy      │ Ollama (data stays local)                                 │
 * │ Offline      │ Ollama (no internet required)                             │
 * │ Best Quality │ OpenAI GPT-4o or Anthropic Claude Opus                    │
 * └──────────────┴───────────────────────────────────────────────────────────┘
 *
 * ═══════════════════════════════════════════════════════════════════════════════
 */
