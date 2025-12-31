/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║              04. DEV ASSISTANT - LONG TERM MEMORY (RAG)                      ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  OBJECTIVE: Understand semantic memory and the RAG pattern                   ║
 * ║                                                                              ║
 * ║  KEY CONCEPTS:                                                               ║
 * ║                                                                              ║
 * ║  1. EMBEDDINGS                                                               ║
 * ║     - Convert text into numerical vectors (e.g. 1536 or 3072 dimensions)     ║
 * ║     - Similar texts have similar vectors (closeness in space)                ║
 * ║     - Enable semantic similarity search                                      ║
 * ║                                                                              ║
 * ║  2. VECTOR STORE                                                             ║
 * ║     - Database optimized for vectors                                         ║
 * ║     - Enables efficient similarity searches                                  ║
 * ║     - Examples: Pinecone, Qdrant, Weaviate, InMemory                         ║
 * ║                                                                              ║
 * ║  3. RAG (Retrieval Augmented Generation)                                     ║
 * ║     - First RETRIEVE relevant information from the vector store              ║
 * ║     - Then PROVIDE it as context to the LLM                                  ║
 * ║     - The LLM GENERATES an informed response                                 ║
 * ║                                                                              ║
 * ║  4. ChatHistoryMemoryProvider                                                ║
 * ║     - Built-in implementation of the RAG pattern                             ║
 * ║     - Automatically saves messages to the vector store                       ║
 * ║     - Retrieves relevant messages from previous conversations                ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.Text;
using Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI;
using OpenAI.Chat;

// ═══════════════════════════════════════════════════════════════════════════════
// INITIAL CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════════

Console.OutputEncoding = Encoding.UTF8;

ConsoleHelper.WriteTitle("04. LongTermMemory");
ConsoleHelper.WriteSubtitle("Memoria Semantica e RAG con Vector Store");

var apiKey = ConfigurationHelper.GetOpenAiApiKey();
var chatModel = ConfigurationHelper.GetOpenAiModel();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * EMBEDDING MODEL CONFIGURATION
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Embeddings are the foundation of semantic memory.
 *
 * OPENAI EMBEDDING MODELS:
 * - text-embedding-3-small: 1536 dimensions, economical, good for most uses
 * - text-embedding-3-large: 3072 dimensions, more precise, more expensive
 * - text-embedding-ada-002: 1536 dimensions, legacy model
 *
 * HOW THEY WORK:
 * "Ciao, come stai?" → [0.023, -0.156, 0.891, ..., 0.234] (1536 numbers)
 * "Salve, tutto bene?" → [0.021, -0.148, 0.887, ..., 0.231] (similar vector!)
 */

ConsoleHelper.WriteSeparator("Step 1: Configurazione Embeddings");

// Embedding model - text-embedding-3-small is a good compromise
const string embeddingModel = "text-embedding-3-small";
const int vectorDimensions = 1536;  // Vector dimensions for text-embedding-3-small

ConsoleHelper.WriteInfo($"Embedding Model: {embeddingModel}");
ConsoleHelper.WriteInfo($"Vector Dimensions: {vectorDimensions}");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * VECTOR STORE CREATION
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * The VectorStore is where we save our embeddings.
 *
 * TYPES OF VECTOR STORE:
 * - InMemoryVectorStore: For development/testing, data lost on restart
 * - Pinecone: Cloud-based, scalable, managed
 * - Qdrant: Open source, self-hosted or cloud
 * - Weaviate: Open source, schema-based
 * - Azure AI Search: Integrated with Azure
 *
 * For this example we use Semantic Kernel's InMemoryVectorStore.
 * In production, you would use a persistent vector store.
 */

ConsoleHelper.WriteSeparator("Step 2: Creazione Vector Store");

// Create the OpenAI client
var openAiClient = new OpenAIClient(apiKey);

/*
 * EMBEDDING GENERATOR CREATION:
 *
 * The EmbeddingGenerator converts text into vectors.
 * We use OpenAI's embedding API through Microsoft.Extensions.AI.
 *
 * The AsIEmbeddingGenerator() method converts OpenAI's EmbeddingClient
 * into the standard IEmbeddingGenerator interface used by the framework.
 */
var embeddingGenerator = openAiClient
    .GetEmbeddingClient(embeddingModel)
    .AsIEmbeddingGenerator();

ConsoleHelper.WriteInfo("Embedding Generator creato");

/*
 * VECTOR STORE CREATION:
 *
 * InMemoryVectorStore is an in-memory implementation of VectorStore.
 * Perfect for development and testing, but data is lost on restart.
 *
 * NOTE: The EmbeddingGenerator is passed to the VectorStore to
 * automatically generate embeddings when we save data.
 */
VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator
});

ConsoleHelper.WriteSuccess("InMemory Vector Store creato");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * AGENT CREATION WITH ChatHistoryMemoryProvider
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * ChatHistoryMemoryProvider is an AIContextProvider that:
 * 1. SAVES every message to the vector store automatically (InvokedAsync)
 * 2. RETRIEVES relevant messages before each invocation (InvokingAsync)
 * 3. PROVIDES the retrieved messages as context to the LLM
 *
 * SCOPE (search scope):
 * - StorageScope: where to save messages (UserId + ThreadId)
 * - SearchScope: where to search (can be broader, e.g. only UserId)
 *
 * This allows remembering information across different conversations!
 */

ConsoleHelper.WriteSeparator("Step 3: Creazione Agente con Memoria Semantica");

// Generate a fixed user ID (in production: from authentication)
const string userId = "dev_user_001";

// Unique thread ID for this session
string threadId = Guid.NewGuid().ToString();

string agentInstructions = """
    Sei DevAssistant, un assistente tecnico con memoria a lungo termine.

    CAPACITÀ DI MEMORIA:
    - Ricordi conversazioni precedenti con lo stesso utente
    - Puoi richiamare informazioni condivise in sessioni passate
    - Personalizzi le risposte in base alla storia con l'utente

    COMPORTAMENTO:
    - Se l'utente menziona informazioni personali o preferenze, ricordalo
    - Quando appropriato, fai riferimento a conversazioni precedenti
    - Sii proattivo nel collegare le nuove domande con il contesto storico

    Sii conciso ma completo nelle risposte.
    """;

/*
 * AGENT CREATION WITH ChatHistoryMemoryProvider:
 *
 * The AIContextProviderFactory is called when creating a new thread.
 * It returns a ChatHistoryMemoryProvider instance configured to:
 * - Save messages with scope (userId, threadId)
 * - Search messages only by userId (finds from all threads!)
 */
ChatClientAgent agent = openAiClient
    .GetChatClient(chatModel)
    .CreateAIAgent(new ChatClientAgentOptions
    {
        Name = "DevAssistant",
        ChatOptions = new ChatOptions
        {
            Instructions = agentInstructions
        },
        /*
         * FACTORY FOR ChatHistoryMemoryProvider:
         *
         * This factory is called for each new thread.
         * Configures semantic memory with:
         * - vectorStore: where to save/search embeddings
         * - collectionName: name of the "table" in the vector store
         * - vectorDimensions: vector dimensions (must match the embedding model)
         * - storageScope: where to save (userId + specific threadId)
         * - searchScope: where to search (only userId - finds from ALL threads!)
         */
        AIContextProviderFactory = ctx => new ChatHistoryMemoryProvider(
            vectorStore,
            collectionName: "devassistant_memory",
            vectorDimensions: vectorDimensions,
            // Save with userId AND specific threadId
            storageScope: new ChatHistoryMemoryProviderScope
            {
                UserId = userId,
                ThreadId = threadId
            },
            // But search only by userId - finds messages from ALL threads!
            searchScope: new ChatHistoryMemoryProviderScope
            {
                UserId = userId
                // ThreadId = null means "search in all threads"
            })
    });

ConsoleHelper.WriteSuccess("Agente con ChatHistoryMemoryProvider creato");
ConsoleHelper.WriteInfo($"User ID: {userId}");
ConsoleHelper.WriteInfo($"Thread ID: {threadId}");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CONVERSATION LOOP
 * ═══════════════════════════════════════════════════════════════════════════════
 */

ConsoleHelper.WriteSeparator("Step 4: Avvio Conversazione");

// Create the thread for this session
AgentThread thread = agent.GetNewThread();

Console.WriteLine();
Console.WriteLine("COMANDI SPECIALI:");
Console.WriteLine("  'esci' o 'quit'  - Termina la sessione");
Console.WriteLine("  'nuovo'          - Inizia un nuovo thread (stessa memoria)");
Console.WriteLine("  'info'           - Mostra informazioni sulla sessione");
Console.WriteLine();

Console.WriteLine("PROVA QUESTO:");
Console.WriteLine("  1. Di' all'agente qualcosa di personale (es. 'Mi chiamo Marco e lavoro con Python')");
Console.WriteLine("  2. Chiudi con 'esci' e riavvia il programma");
Console.WriteLine("  3. Chiedi 'Cosa sai di me?' - l'agente ricorderà!");
Console.WriteLine();

while (true)
{
    ConsoleHelper.WriteUserMessage("");
    Console.Write("> ");
    var userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    var command = userInput.Trim().ToLowerInvariant();

    if (command is "esci" or "quit" or "exit")
    {
        Console.WriteLine();
        ConsoleHelper.WriteInfo("La memoria semantica persiste nel vector store (in questo caso in memoria).");
        ConsoleHelper.WriteInfo("In produzione, i dati sarebbero salvati in un vector store persistente.");
        ConsoleHelper.WriteInfo("Alla prossima!");
        break;
    }

    if (command == "nuovo")
    {
        // Create a new thread but keep the same memory
        threadId = Guid.NewGuid().ToString();
        thread = agent.GetNewThread();
        Console.WriteLine();
        ConsoleHelper.WriteSuccess("Nuovo thread creato!");
        ConsoleHelper.WriteInfo($"Nuovo Thread ID: {threadId}");
        ConsoleHelper.WriteInfo("La memoria semantica rimane - l'agente ricorda le conversazioni precedenti.");
        Console.WriteLine();
        continue;
    }

    if (command == "info")
    {
        Console.WriteLine();
        Console.WriteLine("=== INFORMAZIONI SESSIONE ===");
        Console.WriteLine($"  User ID: {userId}");
        Console.WriteLine($"  Thread ID: {threadId}");
        Console.WriteLine($"  Chat Model: {chatModel}");
        Console.WriteLine($"  Embedding Model: {embeddingModel}");
        Console.WriteLine($"  Vector Dimensions: {vectorDimensions}");
        Console.WriteLine();
        continue;
    }

    try
    {
        /*
         * WHAT HAPPENS DURING INVOCATION:
         *
         * 1. The user message is processed
         * 2. ChatHistoryMemoryProvider.InvokingAsync():
         *    - Generates embedding of the message
         *    - Searches for similar messages in the vector store (by userId)
         *    - Adds found messages as context
         * 3. The LLM receives: instructions + context (previous messages) + message
         * 4. The LLM generates the response
         * 5. ChatHistoryMemoryProvider.InvokedAsync():
         *    - Saves the new message (user + assistant) to the vector store
         *    - With scope (userId, threadId)
         */

        ConsoleHelper.WriteAgentHeader();

        await foreach (var update in agent.RunStreamingAsync(userInput, thread))
        {
            ConsoleHelper.WriteStreamChunk(update.ToString());
        }

        ConsoleHelper.EndStreamLine();
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Errore: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.WriteLine($"  Dettaglio: {ex.InnerException.Message}");
        }
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * SUMMARY OF CONCEPTS LEARNED
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * 1. EMBEDDINGS:
 *    - Numerical representation of semantic meaning
 *    - Enable similarity comparisons
 *    - Generated by specialized models (text-embedding-3-*)
 *
 * 2. VECTOR STORE:
 *    - Database for vectors with similarity search
 *    - InMemoryVectorStore for development
 *    - Pinecone, Qdrant, etc. for production
 *
 * 3. ChatHistoryMemoryProvider:
 *    - Built-in AIContextProvider for semantic memory
 *    - Automatically saves messages
 *    - Retrieves relevant messages
 *    - Configurable with storage/search scope
 *
 * 4. RAG PATTERN:
 *    - Retrieve: search for relevant information
 *    - Augment: add to context
 *    - Generate: the LLM uses the context
 *
 * DIFFERENCE FROM PROJECT 03:
 * - Project 03: Explicit extraction of facts (name, project)
 * - Project 04: Semantic memory (finds by similarity)
 * - Both useful! Combine them in production.
 *
 * IN THE NEXT PROJECT:
 * - RAG with external documents (knowledge base)
 * - Loading and chunking documents
 * - TextSearchProvider for full-text search
 */
