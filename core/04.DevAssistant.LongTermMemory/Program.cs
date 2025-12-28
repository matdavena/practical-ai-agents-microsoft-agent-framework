/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║              04. DEV ASSISTANT - LONG TERM MEMORY (RAG)                      ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  OBIETTIVO: Comprendere la memoria semantica e il pattern RAG                ║
 * ║                                                                              ║
 * ║  CONCETTI CHIAVE:                                                            ║
 * ║                                                                              ║
 * ║  1. EMBEDDINGS                                                               ║
 * ║     - Convertono testo in vettori numerici (es. 1536 o 3072 dimensioni)      ║
 * ║     - Testi simili hanno vettori simili (vicinanza nello spazio)             ║
 * ║     - Permettono la ricerca per similarità semantica                         ║
 * ║                                                                              ║
 * ║  2. VECTOR STORE                                                             ║
 * ║     - Database ottimizzato per vettori                                       ║
 * ║     - Permette ricerche per similarità efficienti                            ║
 * ║     - Esempi: Pinecone, Qdrant, Weaviate, InMemory                           ║
 * ║                                                                              ║
 * ║  3. RAG (Retrieval Augmented Generation)                                     ║
 * ║     - Prima RECUPERA informazioni rilevanti dal vector store                 ║
 * ║     - Poi le FORNISCE come contesto all'LLM                                  ║
 * ║     - L'LLM GENERA una risposta informata                                    ║
 * ║                                                                              ║
 * ║  4. ChatHistoryMemoryProvider                                                ║
 * ║     - Implementazione built-in del pattern RAG                               ║
 * ║     - Salva automaticamente i messaggi nel vector store                      ║
 * ║     - Recupera messaggi rilevanti da conversazioni precedenti                ║
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
// CONFIGURAZIONE INIZIALE
// ═══════════════════════════════════════════════════════════════════════════════

Console.OutputEncoding = Encoding.UTF8;

ConsoleHelper.WriteTitle("04. LongTermMemory");
ConsoleHelper.WriteSubtitle("Memoria Semantica e RAG con Vector Store");

var apiKey = ConfigurationHelper.GetOpenAiApiKey();
var chatModel = ConfigurationHelper.GetOpenAiModel();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CONFIGURAZIONE EMBEDDING MODEL
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Gli embeddings sono la base della memoria semantica.
 *
 * MODELLI EMBEDDING OPENAI:
 * - text-embedding-3-small: 1536 dimensioni, economico, buono per la maggior parte degli usi
 * - text-embedding-3-large: 3072 dimensioni, più preciso, più costoso
 * - text-embedding-ada-002: 1536 dimensioni, modello legacy
 *
 * COME FUNZIONANO:
 * "Ciao, come stai?" → [0.023, -0.156, 0.891, ..., 0.234] (1536 numeri)
 * "Salve, tutto bene?" → [0.021, -0.148, 0.887, ..., 0.231] (vettore simile!)
 */

ConsoleHelper.WriteSeparator("Step 1: Configurazione Embeddings");

// Modello di embedding - text-embedding-3-small è un buon compromesso
const string embeddingModel = "text-embedding-3-small";
const int vectorDimensions = 1536;  // Dimensioni del vettore per text-embedding-3-small

ConsoleHelper.WriteInfo($"Embedding Model: {embeddingModel}");
ConsoleHelper.WriteInfo($"Vector Dimensions: {vectorDimensions}");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CREAZIONE DEL VECTOR STORE
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Il VectorStore è dove salviamo i nostri embeddings.
 *
 * TIPI DI VECTOR STORE:
 * - InMemoryVectorStore: Per sviluppo/test, dati persi al riavvio
 * - Pinecone: Cloud-based, scalabile, managed
 * - Qdrant: Open source, self-hosted o cloud
 * - Weaviate: Open source, schema-based
 * - Azure AI Search: Integrato con Azure
 *
 * Per questo esempio usiamo InMemoryVectorStore di Semantic Kernel.
 * In produzione, useresti un vector store persistente.
 */

ConsoleHelper.WriteSeparator("Step 2: Creazione Vector Store");

// Creiamo il client OpenAI
var openAiClient = new OpenAIClient(apiKey);

/*
 * CREAZIONE DELL'EMBEDDING GENERATOR:
 *
 * L'EmbeddingGenerator converte testo in vettori.
 * Usiamo OpenAI's embedding API attraverso Microsoft.Extensions.AI.
 *
 * Il metodo AsIEmbeddingGenerator() converte l'EmbeddingClient di OpenAI
 * nell'interfaccia standard IEmbeddingGenerator usata dal framework.
 */
var embeddingGenerator = openAiClient
    .GetEmbeddingClient(embeddingModel)
    .AsIEmbeddingGenerator();

ConsoleHelper.WriteInfo("Embedding Generator creato");

/*
 * CREAZIONE DEL VECTOR STORE:
 *
 * InMemoryVectorStore è un'implementazione in memoria di VectorStore.
 * Perfetta per sviluppo e test, ma i dati si perdono al riavvio.
 *
 * NOTA: L'EmbeddingGenerator viene passato al VectorStore per
 * generare automaticamente gli embeddings quando salviamo i dati.
 */
VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator
});

ConsoleHelper.WriteSuccess("InMemory Vector Store creato");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CREAZIONE DELL'AGENTE CON ChatHistoryMemoryProvider
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * ChatHistoryMemoryProvider è un AIContextProvider che:
 * 1. SALVA automaticamente ogni messaggio nel vector store (InvokedAsync)
 * 2. RECUPERA messaggi rilevanti prima di ogni invocazione (InvokingAsync)
 * 3. FORNISCE i messaggi recuperati come contesto all'LLM
 *
 * SCOPE (ambito di ricerca):
 * - StorageScope: dove salvare i messaggi (UserId + ThreadId)
 * - SearchScope: dove cercare (può essere più ampio, es. solo UserId)
 *
 * Questo permette di ricordare informazioni tra conversazioni diverse!
 */

ConsoleHelper.WriteSeparator("Step 3: Creazione Agente con Memoria Semantica");

// Generiamo un ID utente fisso (in produzione: dall'autenticazione)
const string userId = "dev_user_001";

// ID thread unico per questa sessione
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
 * CREAZIONE DELL'AGENTE CON ChatHistoryMemoryProvider:
 *
 * La factory AIContextProviderFactory viene chiamata quando si crea un nuovo thread.
 * Restituisce un'istanza di ChatHistoryMemoryProvider configurata per:
 * - Salvare messaggi con scope (userId, threadId)
 * - Cercare messaggi solo per userId (trova da tutti i thread!)
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
         * FACTORY PER ChatHistoryMemoryProvider:
         *
         * Questa factory viene chiamata per ogni nuovo thread.
         * Configura la memoria semantica con:
         * - vectorStore: dove salvare/cercare gli embeddings
         * - collectionName: nome della "tabella" nel vector store
         * - vectorDimensions: dimensioni del vettore (deve matchare l'embedding model)
         * - storageScope: dove salvare (userId + threadId specifico)
         * - searchScope: dove cercare (solo userId - trova da TUTTI i thread!)
         */
        AIContextProviderFactory = ctx => new ChatHistoryMemoryProvider(
            vectorStore,
            collectionName: "devassistant_memory",
            vectorDimensions: vectorDimensions,
            // Salviamo con userId E threadId specifico
            storageScope: new ChatHistoryMemoryProviderScope
            {
                UserId = userId,
                ThreadId = threadId
            },
            // Ma cerchiamo solo per userId - trova messaggi da TUTTI i thread!
            searchScope: new ChatHistoryMemoryProviderScope
            {
                UserId = userId
                // ThreadId = null significa "cerca in tutti i thread"
            })
    });

ConsoleHelper.WriteSuccess("Agente con ChatHistoryMemoryProvider creato");
ConsoleHelper.WriteInfo($"User ID: {userId}");
ConsoleHelper.WriteInfo($"Thread ID: {threadId}");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * LOOP DI CONVERSAZIONE
 * ═══════════════════════════════════════════════════════════════════════════════
 */

ConsoleHelper.WriteSeparator("Step 4: Avvio Conversazione");

// Creiamo il thread per questa sessione
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
        // Creiamo un nuovo thread ma manteniamo la stessa memoria
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
         * COSA SUCCEDE DURANTE L'INVOCAZIONE:
         *
         * 1. Il messaggio utente viene processato
         * 2. ChatHistoryMemoryProvider.InvokingAsync():
         *    - Genera embedding del messaggio
         *    - Cerca messaggi simili nel vector store (per userId)
         *    - Aggiunge i messaggi trovati come contesto
         * 3. L'LLM riceve: istruzioni + contesto (messaggi precedenti) + messaggio
         * 4. L'LLM genera la risposta
         * 5. ChatHistoryMemoryProvider.InvokedAsync():
         *    - Salva il nuovo messaggio (user + assistant) nel vector store
         *    - Con scope (userId, threadId)
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
 * RIEPILOGO CONCETTI APPRESI
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * 1. EMBEDDINGS:
 *    - Rappresentazione numerica del significato semantico
 *    - Permettono confronti di similarità
 *    - Generati da modelli specializzati (text-embedding-3-*)
 *
 * 2. VECTOR STORE:
 *    - Database per vettori con ricerca per similarità
 *    - InMemoryVectorStore per sviluppo
 *    - Pinecone, Qdrant, etc. per produzione
 *
 * 3. ChatHistoryMemoryProvider:
 *    - AIContextProvider built-in per memoria semantica
 *    - Salva automaticamente i messaggi
 *    - Recupera messaggi rilevanti
 *    - Configurable con storage/search scope
 *
 * 4. RAG PATTERN:
 *    - Retrieve: cerca informazioni rilevanti
 *    - Augment: aggiungi al contesto
 *    - Generate: l'LLM usa il contesto
 *
 * DIFFERENZA DAL PROGETTO 03:
 * - Progetto 03: Estrazione esplicita di fatti (nome, progetto)
 * - Progetto 04: Memoria semantica (trova per similarità)
 * - Entrambi utili! Combinarli in produzione.
 *
 * NEL PROSSIMO PROGETTO:
 * - RAG con documenti esterni (knowledge base)
 * - Caricamento e chunking di documenti
 * - TextSearchProvider per ricerca full-text
 */
