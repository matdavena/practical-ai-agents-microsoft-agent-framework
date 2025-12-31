// ============================================================================
// 05. CODE REVIEWER - RAG
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBJECTIVE OF THIS PROJECT:
// Learn how to implement RAG (Retrieval-Augmented Generation) to give
// the agent access to a knowledge base of external documents.
//
// SCENARIO:
// An AI Code Reviewer that knows programming best practices
// loaded from a knowledge base of markdown documents.
//
// KEY CONCEPTS:
//
// 1. RAG (Retrieval-Augmented Generation):
//    ┌─────────────────────────────────────────────────────────────────┐
//    │  PROBLEM: LLM only has knowledge from training                 │
//    │  SOLUTION: Retrieves relevant information before responding    │
//    └─────────────────────────────────────────────────────────────────┘
//
// 2. RAG Pipeline:
//    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌───────────┐
//    │  User    │ ─► │ Retrieval│ ─► │ Augment  │ ─► │ Generate  │
//    │  query   │    │ (search) │    │ (enrich) │    │ (respond) │
//    └──────────┘    └──────────┘    └──────────┘    └───────────┘
//
// 3. Implemented components:
//    - TextSearchDocument: model for document chunks
//    - TextSearchStore: loading, chunking, embedding, search
//    - KnowledgeBaseProvider: AIContextProvider for agent integration
//
// RAG ADVANTAGES:
// ✅ Updatable knowledge without re-training
// ✅ Responses based on specific and citable sources
// ✅ Lower costs compared to fine-tuning
// ✅ Transparency: you know where the information comes from
//
// RUN WITH: dotnet run --project core/05.CodeReviewer.RAG
// ============================================================================

using System.Text;
using System.ClientModel;
using CodeReviewer.RAG.Rag;
using Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI;
using OpenAI.Chat;

namespace CodeReviewer.RAG;

public static class Program
{
    // ========================================================================
    // CONFIGURAZIONE
    // ========================================================================

    /// <summary>
    /// Chat model (gpt-4o-mini is economical and capable).
    /// </summary>
    private const string ChatModel = "gpt-4o-mini";

    /// <summary>
    /// Model for embeddings.
    /// text-embedding-3-small: 1536 dimensions, excellent quality/price ratio.
    /// </summary>
    private const string EmbeddingModel = "text-embedding-3-small";

    /// <summary>
    /// Knowledge base path relative to the executable.
    /// Files are copied during build (see csproj).
    /// </summary>
    private const string KnowledgeBasePath = "KnowledgeBase";

    // ========================================================================
    // ENTRY POINT
    // ========================================================================

    public static async Task Main()
    {
        // Important to correctly display emojis and special characters
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleHelper.WriteTitle("05. CodeReviewer RAG");
        ConsoleHelper.WriteSubtitle("Knowledge Base per Best Practices");

        // ====================================================================
        // STEP 1: SETUP OPENAI CLIENT
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 1: Setup OpenAI Client");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey));

        Console.WriteLine($"✅ OpenAI client configured");
        Console.WriteLine($"   Chat Model: {ChatModel}");
        Console.WriteLine($"   Embedding Model: {EmbeddingModel}");

        // ====================================================================
        // STEP 2: SETUP VECTOR STORE AND EMBEDDING GENERATOR
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 2: Setup Vector Store");

        // Create the embedding generator using the AsIEmbeddingGenerator extension method
        // This converts EmbeddingClient to IEmbeddingGenerator<string, Embedding<float>>
        var embeddingGenerator = openAiClient
            .GetEmbeddingClient(EmbeddingModel)
            .AsIEmbeddingGenerator();

        Console.WriteLine("✅ Embedding generator configured");

        // Create the in-memory vector store
        // InMemoryVectorStore is great for demos and testing
        // In production you would use: Azure AI Search, Qdrant, Pinecone, etc.
        var vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
        {
            EmbeddingGenerator = embeddingGenerator
        });

        Console.WriteLine("✅ In-memory vector store created");

        // ====================================================================
        // STEP 3: LOAD THE KNOWLEDGE BASE
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 3: Loading Knowledge Base");

        // Verify that the folder exists
        if (!Directory.Exists(KnowledgeBasePath))
        {
            ConsoleHelper.WriteError($"Knowledge base folder not found: {KnowledgeBasePath}");
            ConsoleHelper.WriteError("Make sure the .md files are in the KnowledgeBase/ folder");
            return;
        }

        // Create the store for searching
        // The vectorStore already has the EmbeddingGenerator configured
        var searchStore = new TextSearchStore(vectorStore);

        // Load all markdown documents
        // This process:
        // 1. Reads each .md file
        // 2. Splits into chunks (manageable pieces)
        // 3. Generates embeddings for each chunk (automatically!)
        // 4. Stores in the vector store
        var totalChunks = await searchStore.LoadKnowledgeBaseAsync(KnowledgeBasePath);

        if (totalChunks == 0)
        {
            ConsoleHelper.WriteError("No chunks loaded. Check the files in the knowledge base.");
            return;
        }

        // ====================================================================
        // STEP 4: CREATE THE AGENT WITH RAG
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 4: Creating Code Reviewer Agent");

        // Create the RAG provider
        // This automatically injects context from the knowledge base
        var knowledgeBaseProvider = new KnowledgeBaseProvider(
            searchStore,
            topK: 3,      // Retrieve the 3 most relevant chunks
            minScore: 0.3f // Minimum similarity score
        );

        // System prompt for the Code Reviewer
        // The agent is instructed to:
        // 1. Be a code review expert
        // 2. Use the injected knowledge base
        // 3. Cite sources in responses
        const string systemPrompt = """
            Sei un Code Reviewer esperto specializzato in C# e .NET.

            Il tuo compito è aiutare gli sviluppatori a scrivere codice migliore,
            seguendo le best practices e i principi di clean code.

            ISTRUZIONI IMPORTANTI:
            1. Usa le informazioni dalla knowledge base quando disponibili
            2. Cita sempre la fonte (nome del documento) quando usi informazioni dalla knowledge base
            3. Se non trovi informazioni rilevanti, usa la tua conoscenza generale
            4. Fornisci esempi di codice quando appropriato
            5. Spiega il "perché" dietro ogni raccomandazione

            Formato per le citazioni:
            "Secondo [Nome Documento], ..." oppure "(fonte: nome-documento.md)"
            """;

        // Create the agent with the RAG provider
        var chatClient = openAiClient.GetChatClient(ChatModel);
        ChatClientAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Name = "CodeReviewer",
            // The factory creates the provider for each thread/conversation
            // In this case we always use the same provider
            AIContextProviderFactory = _ => knowledgeBaseProvider,
            // Options for the model
            ChatOptions = new ChatOptions
            {
                Temperature = 0.7f // A bit of creativity, but not too much
            }
        });

        Console.WriteLine("✅ Code Reviewer Agent created with RAG");

        // ====================================================================
        // STEP 5: INTERACTIVE DEMO
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 5: Code Review Demo with RAG");

        Console.WriteLine();
        Console.WriteLine("🎯 Ask the Code Reviewer anything about:");
        Console.WriteLine("   - Naming conventions in C#");
        Console.WriteLine("   - SOLID principles");
        Console.WriteLine("   - Exception handling");
        Console.WriteLine("   - Async/await best practices");
        Console.WriteLine();
        Console.WriteLine("The agent will use the knowledge base to respond!");
        Console.WriteLine("Type 'exit' to quit.");
        Console.WriteLine();

        // Create a conversation thread
        AgentThread thread = agent.GetNewThread();

        // Counter to know if it's the first message
        // (AgentThread doesn't expose Messages directly)
        int messageCount = 0;

        // Conversation loop
        while (true)
        {
            Console.Write("Tu: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("👋 Goodbye!");
                break;
            }

            Console.WriteLine();

            try
            {
                // For the first message, we include the instructions as context
                var promptWithContext = messageCount == 0
                    ? $"[System context: {systemPrompt}]\n\n{userInput}"
                    : userInput;

                // Invoke the agent with streaming
                // The provider's InvokingAsync is called automatically,
                // searching the knowledge base and injecting the context
                ConsoleHelper.WriteAgentHeader();

                await foreach (var update in agent.RunStreamingAsync(promptWithContext, thread))
                {
                    ConsoleHelper.WriteStreamChunk(update.ToString());
                }

                ConsoleHelper.EndStreamLine();

                // Increment the message counter
                messageCount++;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error: {ex.Message}");
            }
        }

        // ====================================================================
        // SUMMARY
        // ====================================================================
        ConsoleHelper.WriteSeparator("Summary");

        Console.WriteLine("📚 In this project you learned:");
        Console.WriteLine("   1. How to implement RAG with Microsoft Agent Framework");
        Console.WriteLine("   2. Document chunking for efficient embedding");
        Console.WriteLine("   3. Semantic search with vector store");
        Console.WriteLine("   4. AIContextProvider to inject dynamic knowledge");
        Console.WriteLine("   5. Citing sources in responses");
        Console.WriteLine();
        Console.WriteLine("🔜 In the next project: Task Planner with goals!");
    }
}
