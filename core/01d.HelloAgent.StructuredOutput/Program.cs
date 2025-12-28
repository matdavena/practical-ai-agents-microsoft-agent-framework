// ============================================================================
// 01d. HELLO AGENT - STRUCTURED OUTPUT
// ============================================================================
// This project demonstrates how to get typed, structured responses from LLMs.
// Instead of parsing free-form text, you get guaranteed JSON that deserializes
// to your C# classes.
//
// KEY BENEFITS:
// - No regex or string parsing needed
// - Guaranteed schema compliance
// - Type-safe access to response data
// - Works with complex nested objects
//
// METHODS DEMONSTRATED:
// 1. RunAsync<T>() - Generic method returns typed response
// 2. ChatResponseFormat.ForJsonSchema<T>() - Specify schema at agent creation
// 3. Manual deserialization with Deserialize<T>()
// ============================================================================

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using HelloAgent.StructuredOutput.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

// ============================================================================
// CONFIGURATION
// ============================================================================
Console.OutputEncoding = Encoding.UTF8;

string apiKey;
try
{
    apiKey = ConfigurationHelper.GetOpenAiApiKey();
}
catch (Exception ex)
{
    ConsoleHelper.WriteError("OpenAI API key not configured!");
    ConsoleHelper.WriteInfo("Set OPENAI_API_KEY environment variable.");
    Console.WriteLine(ex.Message);
    return;
}

string model = ConfigurationHelper.GetOpenAiModel();

// JSON serializer options for enum handling
JsonSerializerOptions jsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() }
};

// ============================================================================
// MAIN MENU
// ============================================================================
while (true)
{
    Console.Clear();
    ConsoleHelper.WriteTitle("Structured Output");
    ConsoleHelper.WriteSubtitle("Getting typed responses from LLMs");

    Console.WriteLine("Select a demo:");
    Console.WriteLine();
    Console.WriteLine("  [1] Person Extraction - Extract person info from text");
    Console.WriteLine("  [2] Recipe Generator - Generate structured recipes");
    Console.WriteLine("  [3] Sentiment Analysis - Analyze text sentiment");
    Console.WriteLine("  [4] Compare: Structured vs Unstructured");
    Console.WriteLine("  [5] Interactive: Ask anything with custom type");
    Console.WriteLine();
    Console.WriteLine("  [0] Exit");
    Console.WriteLine();

    Console.Write("Your choice: ");
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            await DemoPersonExtraction(apiKey, model);
            break;
        case "2":
            await DemoRecipeGenerator(apiKey, model);
            break;
        case "3":
            await DemoSentimentAnalysis(apiKey, model);
            break;
        case "4":
            await DemoCompareOutputs(apiKey, model);
            break;
        case "5":
            await DemoInteractive(apiKey, model);
            break;
        case "0":
            ConsoleHelper.WriteSuccess("Goodbye!");
            return;
        default:
            ConsoleHelper.WriteWarning("Invalid choice. Press any key to continue...");
            Console.ReadKey();
            break;
    }
}

// ============================================================================
// DEMO 1: PERSON EXTRACTION
// ============================================================================
async Task DemoPersonExtraction(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 1: Person Extraction");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Extract structured person information from any text.");
    ConsoleHelper.WriteInfo("The LLM returns a PersonInfo object, not free-form text.");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);
    ChatClient chatClient = client.GetChatClient(model);

    // Create agent
    ChatClientAgent agent = chatClient.CreateAIAgent(
        instructions: "You are an expert at extracting person information from text. Extract all available details about any person mentioned.",
        name: "PersonExtractor"
    );

    // Sample texts to analyze
    string[] sampleTexts =
    [
        "John Smith is a 35-year-old software engineer from Seattle. He specializes in C#, Python, and cloud architecture.",
        "Dr. Maria Garcia, 42, leads the AI research team at TechCorp in San Francisco. She has expertise in machine learning and NLP.",
        "Meet Alex Chen, a young entrepreneur aged 28 who founded a startup in Berlin. He's skilled in product design and business development."
    ];

    Console.WriteLine("Sample texts available:");
    for (int i = 0; i < sampleTexts.Length; i++)
    {
        Console.WriteLine($"  [{i + 1}] {sampleTexts[i][..Math.Min(60, sampleTexts[i].Length)]}...");
    }
    Console.WriteLine();

    Console.Write("Choose a sample (1-3) or enter your own text: ");
    string? input = Console.ReadLine();

    string textToAnalyze;
    if (int.TryParse(input, out int sampleIndex) && sampleIndex >= 1 && sampleIndex <= 3)
    {
        textToAnalyze = sampleTexts[sampleIndex - 1];
    }
    else if (!string.IsNullOrWhiteSpace(input))
    {
        textToAnalyze = input;
    }
    else
    {
        textToAnalyze = sampleTexts[0];
    }

    Console.WriteLine();
    ConsoleHelper.WriteUserMessage(textToAnalyze);
    Console.WriteLine();

    ConsoleHelper.WriteInfo("Extracting person information...");
    Console.WriteLine();

    // KEY FEATURE: RunAsync<T>() returns typed response
    AgentRunResponse<PersonInfo> response = await agent.RunAsync<PersonInfo>(
        $"Extract person information from this text: {textToAnalyze}"
    );

    // Access the structured result
    PersonInfo person = response.Result;

    ConsoleHelper.WriteSuccess("Extraction complete!");
    Console.WriteLine();

    // Display structured data
    Console.WriteLine("┌─────────────────────────────────────────────────────┐");
    Console.WriteLine("│              EXTRACTED PERSON INFO                  │");
    Console.WriteLine("├─────────────────────────────────────────────────────┤");
    Console.WriteLine($"│ Name:       {person.Name ?? "N/A",-39} │");
    Console.WriteLine($"│ Age:        {(person.Age?.ToString() ?? "N/A"),-39} │");
    Console.WriteLine($"│ Occupation: {person.Occupation ?? "N/A",-39} │");
    Console.WriteLine($"│ Location:   {person.Location ?? "N/A",-39} │");
    if (person.Skills != null && person.Skills.Length > 0)
    {
        Console.WriteLine($"│ Skills:     {string.Join(", ", person.Skills)[..Math.Min(39, string.Join(", ", person.Skills).Length)],-39} │");
    }
    Console.WriteLine("└─────────────────────────────────────────────────────┘");

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

// ============================================================================
// DEMO 2: RECIPE GENERATOR
// ============================================================================
async Task DemoRecipeGenerator(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 2: Recipe Generator");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Generate complete, structured recipes from a dish name.");
    ConsoleHelper.WriteInfo("Demonstrates nested objects, arrays, and enums.");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);
    ChatClient chatClient = client.GetChatClient(model);

    // Create agent with ResponseFormat specified at creation time
    ChatClientAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
    {
        Name = "ChefBot",
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a professional chef. Create detailed, accurate recipes with precise measurements and clear instructions.",
            ResponseFormat = ChatResponseFormat.ForJsonSchema<Recipe>(jsonOptions)
        }
    });

    Console.Write("Enter a dish name (or press Enter for 'Spaghetti Carbonara'): ");
    string? dishName = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(dishName))
    {
        dishName = "Spaghetti Carbonara";
    }

    Console.WriteLine();
    ConsoleHelper.WriteUserMessage($"Create a recipe for: {dishName}");
    Console.WriteLine();

    ConsoleHelper.WriteInfo("Generating recipe...");
    Console.WriteLine();

    // Run and get streaming response, then deserialize
    var updates = agent.RunStreamingAsync($"Create a complete recipe for {dishName}");
    AgentRunResponse fullResponse = await updates.ToAgentRunResponseAsync();
    Recipe recipe = fullResponse.Deserialize<Recipe>(jsonOptions);

    ConsoleHelper.WriteSuccess("Recipe generated!");
    Console.WriteLine();

    // Display structured recipe
    Console.WriteLine($"╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine($"║  {recipe.Name,-60} ║");
    Console.WriteLine($"╠═══════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  {recipe.Description[..Math.Min(60, recipe.Description.Length)],-60} ║");
    Console.WriteLine($"╠═══════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  Cuisine: {recipe.Cuisine,-15} Difficulty: {recipe.Difficulty,-20} ║");
    Console.WriteLine($"║  Servings: {recipe.Servings,-14} Prep: {recipe.PrepTimeMinutes}min  Cook: {recipe.CookTimeMinutes}min       ║");
    Console.WriteLine($"╠═══════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  INGREDIENTS                                                   ║");
    Console.WriteLine($"╠═══════════════════════════════════════════════════════════════╣");

    foreach (var ingredient in recipe.Ingredients)
    {
        string line = $"  - {ingredient.Quantity} {ingredient.Unit} {ingredient.Name}";
        Console.WriteLine($"║{line,-63}║");
    }

    Console.WriteLine($"╠═══════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  INSTRUCTIONS                                                  ║");
    Console.WriteLine($"╠═══════════════════════════════════════════════════════════════╣");

    foreach (var step in recipe.Steps)
    {
        string stepText = $"  {step.StepNumber}. {step.Instruction}";
        // Wrap long lines
        while (stepText.Length > 0)
        {
            int len = Math.Min(63, stepText.Length);
            Console.WriteLine($"║{stepText[..len],-63}║");
            stepText = stepText.Length > 63 ? "     " + stepText[63..] : "";
        }
    }

    if (recipe.Tips != null && recipe.Tips.Length > 0)
    {
        Console.WriteLine($"╠═══════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  TIPS                                                          ║");
        Console.WriteLine($"╠═══════════════════════════════════════════════════════════════╣");
        foreach (var tip in recipe.Tips)
        {
            Console.WriteLine($"║  * {tip[..Math.Min(59, tip.Length)],-59}║");
        }
    }

    Console.WriteLine($"╚═══════════════════════════════════════════════════════════════╝");

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

// ============================================================================
// DEMO 3: SENTIMENT ANALYSIS
// ============================================================================
async Task DemoSentimentAnalysis(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 3: Sentiment Analysis");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Analyze text sentiment and extract entities.");
    ConsoleHelper.WriteInfo("Demonstrates NLP tasks with structured output.");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);
    ChatClient chatClient = client.GetChatClient(model);

    ChatClientAgent agent = chatClient.CreateAIAgent(
        instructions: """
            You are an expert sentiment analyst. Analyze text and provide:
            - Overall sentiment classification
            - Confidence score (0.0 to 1.0)
            - Key phrases and entities
            - Emotional tone
            - Suggested actions if applicable
            """,
        name: "SentimentAnalyzer"
    );

    string[] sampleTexts =
    [
        "I absolutely love the new iPhone! The camera is amazing and the battery lasts all day. Best purchase I've made this year!",
        "The customer service at this restaurant was terrible. We waited 45 minutes for our food and when it arrived it was cold. Never going back.",
        "The quarterly report shows mixed results. Revenue increased by 5% but expenses also rose significantly. The board will meet next week to discuss strategy."
    ];

    Console.WriteLine("Sample texts:");
    for (int i = 0; i < sampleTexts.Length; i++)
    {
        Console.WriteLine($"  [{i + 1}] {sampleTexts[i][..Math.Min(70, sampleTexts[i].Length)]}...");
    }
    Console.WriteLine();

    Console.Write("Choose a sample (1-3) or enter your own text: ");
    string? input = Console.ReadLine();

    string textToAnalyze;
    if (int.TryParse(input, out int sampleIndex) && sampleIndex >= 1 && sampleIndex <= 3)
    {
        textToAnalyze = sampleTexts[sampleIndex - 1];
    }
    else if (!string.IsNullOrWhiteSpace(input))
    {
        textToAnalyze = input;
    }
    else
    {
        textToAnalyze = sampleTexts[0];
    }

    Console.WriteLine();
    ConsoleHelper.WriteUserMessage(textToAnalyze);
    Console.WriteLine();

    ConsoleHelper.WriteInfo("Analyzing sentiment...");
    Console.WriteLine();

    AgentRunResponse<SentimentAnalysis> response = await agent.RunAsync<SentimentAnalysis>(
        $"Analyze the sentiment of this text: {textToAnalyze}"
    );

    SentimentAnalysis analysis = response.Result;

    ConsoleHelper.WriteSuccess("Analysis complete!");
    Console.WriteLine();

    // Color-coded sentiment display
    string sentimentColor = analysis.Sentiment switch
    {
        Sentiment.VeryPositive => "green",
        Sentiment.Positive => "green",
        Sentiment.Neutral => "yellow",
        Sentiment.Negative => "red",
        Sentiment.VeryNegative => "red",
        _ => "white"
    };

    Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
    Console.WriteLine("│                    SENTIMENT ANALYSIS                        │");
    Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
    Console.WriteLine($"│ Sentiment:   {analysis.Sentiment,-46} │");
    Console.WriteLine($"│ Confidence:  {analysis.ConfidenceScore:P0,-46} │");
    Console.WriteLine($"│ Tone:        {analysis.EmotionalTone,-46} │");
    Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
    Console.WriteLine($"│ Summary: {analysis.Summary[..Math.Min(50, analysis.Summary.Length)],-50} │");
    Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
    Console.WriteLine("│ Key Phrases:                                                │");

    foreach (var phrase in analysis.KeyPhrases.Take(5))
    {
        Console.WriteLine($"│   - {phrase[..Math.Min(55, phrase.Length)],-55} │");
    }

    if (analysis.Entities != null && analysis.Entities.Length > 0)
    {
        Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
        Console.WriteLine("│ Entities Found:                                             │");
        foreach (var entity in analysis.Entities.Take(5))
        {
            Console.WriteLine($"│   [{entity.Type}] {entity.Text,-48} │");
        }
    }

    if (analysis.SuggestedActions != null && analysis.SuggestedActions.Length > 0)
    {
        Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
        Console.WriteLine("│ Suggested Actions:                                          │");
        foreach (var action in analysis.SuggestedActions.Take(3))
        {
            Console.WriteLine($"│   * {action[..Math.Min(55, action.Length)],-55} │");
        }
    }

    Console.WriteLine("└─────────────────────────────────────────────────────────────┘");

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

// ============================================================================
// DEMO 4: COMPARE STRUCTURED VS UNSTRUCTURED
// ============================================================================
async Task DemoCompareOutputs(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 4: Structured vs Unstructured");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Compare the same query with and without structured output.");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);
    ChatClient chatClient = client.GetChatClient(model);

    string query = "Describe John Smith, a 35-year-old software engineer from Seattle who knows C# and Python.";

    // --- UNSTRUCTURED OUTPUT ---
    Console.WriteLine("=== WITHOUT STRUCTURED OUTPUT ===");
    Console.WriteLine();

    ChatClientAgent unstructuredAgent = chatClient.CreateAIAgent(
        instructions: "Describe people based on the information given.",
        name: "UnstructuredAgent"
    );

    ConsoleHelper.WriteInfo("Response is free-form text (hard to parse programmatically):");
    Console.WriteLine();

    AgentRunResponse unstructuredResponse = await unstructuredAgent.RunAsync(query);
    Console.WriteLine(unstructuredResponse.ToString());

    Console.WriteLine();
    ConsoleHelper.WriteWarning("To use this data, you'd need regex or string parsing!");
    Console.WriteLine();

    // --- STRUCTURED OUTPUT ---
    Console.WriteLine("=== WITH STRUCTURED OUTPUT ===");
    Console.WriteLine();

    ChatClientAgent structuredAgent = chatClient.CreateAIAgent(
        instructions: "Extract person information into the specified format.",
        name: "StructuredAgent"
    );

    ConsoleHelper.WriteInfo("Response is a typed C# object:");
    Console.WriteLine();

    AgentRunResponse<PersonInfo> structuredResponse = await structuredAgent.RunAsync<PersonInfo>(query);
    PersonInfo person = structuredResponse.Result;

    // Direct property access - no parsing needed!
    Console.WriteLine($"  person.Name       = \"{person.Name}\"");
    Console.WriteLine($"  person.Age        = {person.Age}");
    Console.WriteLine($"  person.Occupation = \"{person.Occupation}\"");
    Console.WriteLine($"  person.Location   = \"{person.Location}\"");
    Console.WriteLine($"  person.Skills     = [{string.Join(", ", person.Skills?.Select(s => $"\"{s}\"") ?? [])}]");

    Console.WriteLine();
    ConsoleHelper.WriteSuccess("Direct property access - type-safe and easy to use!");

    Console.WriteLine();

    // Show the raw JSON
    Console.WriteLine("Raw JSON response:");
    Console.WriteLine(JsonSerializer.Serialize(person, jsonOptions));

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

// ============================================================================
// DEMO 5: INTERACTIVE
// ============================================================================
async Task DemoInteractive(string apiKey, string model)
{
    Console.Clear();
    ConsoleHelper.WriteSeparator("Demo 5: Interactive Structured Output");
    Console.WriteLine();
    ConsoleHelper.WriteInfo("Try different structured output scenarios.");
    Console.WriteLine();

    OpenAIClient client = new(apiKey);
    ChatClient chatClient = client.GetChatClient(model);

    Console.WriteLine("Choose output type:");
    Console.WriteLine("  [1] Person Info");
    Console.WriteLine("  [2] Recipe");
    Console.WriteLine("  [3] Sentiment Analysis");
    Console.WriteLine();

    Console.Write("Type (1-3): ");
    string? typeChoice = Console.ReadLine();

    Console.Write("Enter your query: ");
    string? query = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(query))
    {
        ConsoleHelper.WriteWarning("No query provided.");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        return;
    }

    Console.WriteLine();
    ConsoleHelper.WriteInfo("Processing...");
    Console.WriteLine();

    ChatClientAgent agent = chatClient.CreateAIAgent(
        instructions: "Extract or generate structured data based on the user's request.",
        name: "InteractiveAgent"
    );

    try
    {
        switch (typeChoice)
        {
            case "1":
                var personResponse = await agent.RunAsync<PersonInfo>(query);
                Console.WriteLine("Result (PersonInfo):");
                Console.WriteLine(JsonSerializer.Serialize(personResponse.Result, jsonOptions));
                break;

            case "2":
                var recipeResponse = await agent.RunAsync<Recipe>(query);
                Console.WriteLine("Result (Recipe):");
                Console.WriteLine(JsonSerializer.Serialize(recipeResponse.Result, jsonOptions));
                break;

            case "3":
                var sentimentResponse = await agent.RunAsync<SentimentAnalysis>(query);
                Console.WriteLine("Result (SentimentAnalysis):");
                Console.WriteLine(JsonSerializer.Serialize(sentimentResponse.Result, jsonOptions));
                break;

            default:
                ConsoleHelper.WriteWarning("Invalid type choice.");
                break;
        }
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}
