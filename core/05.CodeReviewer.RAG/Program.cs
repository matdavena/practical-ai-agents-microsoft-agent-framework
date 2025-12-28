// ============================================================================
// 05. CODE REVIEWER - RAG
// LEARNING PATH: MICROSOFT AGENT FRAMEWORK
// ============================================================================
//
// OBIETTIVO DI QUESTO PROGETTO:
// Imparare a implementare RAG (Retrieval-Augmented Generation) per dare
// all'agente accesso a una knowledge base di documenti esterni.
//
// SCENARIO:
// Un Code Reviewer AI che conosce le best practices di programmazione
// caricate da una knowledge base di documenti markdown.
//
// CONCETTI CHIAVE:
//
// 1. RAG (Retrieval-Augmented Generation):
//    ┌─────────────────────────────────────────────────────────────────┐
//    │  PROBLEMA: L'LLM ha solo conoscenza dal training               │
//    │  SOLUZIONE: Recupera informazioni rilevanti prima di rispondere │
//    └─────────────────────────────────────────────────────────────────┘
//
// 2. Pipeline RAG:
//    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌───────────┐
//    │  Query   │ ─► │ Retrieval│ ─► │ Augment  │ ─► │ Generate  │
//    │  utente  │    │ (cerca)  │    │ (arricch)│    │ (rispondi)│
//    └──────────┘    └──────────┘    └──────────┘    └───────────┘
//
// 3. Componenti implementati:
//    - TextSearchDocument: modello per chunk di documenti
//    - TextSearchStore: caricamento, chunking, embedding, ricerca
//    - KnowledgeBaseProvider: AIContextProvider per integrazione agente
//
// VANTAGGI RAG:
// ✅ Knowledge aggiornabile senza re-training
// ✅ Risposte basate su fonti specifiche e citabili
// ✅ Costi inferiori rispetto al fine-tuning
// ✅ Trasparenza: sai da dove viene l'informazione
//
// ESEGUI CON: dotnet run --project core/05.CodeReviewer.RAG
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
    /// Modello per il chat (gpt-4o-mini è economico e capace).
    /// </summary>
    private const string ChatModel = "gpt-4o-mini";

    /// <summary>
    /// Modello per gli embeddings.
    /// text-embedding-3-small: 1536 dimensioni, ottimo rapporto qualità/prezzo.
    /// </summary>
    private const string EmbeddingModel = "text-embedding-3-small";

    /// <summary>
    /// Percorso della knowledge base relativo all'eseguibile.
    /// I file vengono copiati durante la build (vedi csproj).
    /// </summary>
    private const string KnowledgeBasePath = "KnowledgeBase";

    // ========================================================================
    // ENTRY POINT
    // ========================================================================

    public static async Task Main()
    {
        // Importante per visualizzare correttamente emoji e caratteri speciali
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleHelper.WriteTitle("05. CodeReviewer RAG");
        ConsoleHelper.WriteSubtitle("Knowledge Base per Best Practices");

        // ====================================================================
        // STEP 1: SETUP OPENAI CLIENT
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 1: Setup OpenAI Client");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey));

        Console.WriteLine($"✅ Client OpenAI configurato");
        Console.WriteLine($"   Chat Model: {ChatModel}");
        Console.WriteLine($"   Embedding Model: {EmbeddingModel}");

        // ====================================================================
        // STEP 2: SETUP VECTOR STORE E EMBEDDING GENERATOR
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 2: Setup Vector Store");

        // Crea il generatore di embeddings usando l'extension method AsIEmbeddingGenerator
        // Questo converte EmbeddingClient in IEmbeddingGenerator<string, Embedding<float>>
        var embeddingGenerator = openAiClient
            .GetEmbeddingClient(EmbeddingModel)
            .AsIEmbeddingGenerator();

        Console.WriteLine("✅ Embedding generator configurato");

        // Crea il vector store in memoria
        // InMemoryVectorStore è ottimo per demo e testing
        // In produzione useresti: Azure AI Search, Qdrant, Pinecone, etc.
        var vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
        {
            EmbeddingGenerator = embeddingGenerator
        });

        Console.WriteLine("✅ Vector store in memoria creato");

        // ====================================================================
        // STEP 3: CARICA LA KNOWLEDGE BASE
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 3: Caricamento Knowledge Base");

        // Verifica che la cartella esista
        if (!Directory.Exists(KnowledgeBasePath))
        {
            ConsoleHelper.WriteError($"Cartella knowledge base non trovata: {KnowledgeBasePath}");
            ConsoleHelper.WriteError("Assicurati che i file .md siano nella cartella KnowledgeBase/");
            return;
        }

        // Crea lo store per la ricerca
        // Il vectorStore ha già l'EmbeddingGenerator configurato
        var searchStore = new TextSearchStore(vectorStore);

        // Carica tutti i documenti markdown
        // Questo processo:
        // 1. Legge ogni file .md
        // 2. Divide in chunk (pezzi gestibili)
        // 3. Genera embedding per ogni chunk (automaticamente!)
        // 4. Memorizza nel vector store
        var totalChunks = await searchStore.LoadKnowledgeBaseAsync(KnowledgeBasePath);

        if (totalChunks == 0)
        {
            ConsoleHelper.WriteError("Nessun chunk caricato. Controlla i file nella knowledge base.");
            return;
        }

        // ====================================================================
        // STEP 4: CREA L'AGENTE CON RAG
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 4: Creazione Code Reviewer Agent");

        // Crea il provider RAG
        // Questo inietta automaticamente il contesto dalla knowledge base
        var knowledgeBaseProvider = new KnowledgeBaseProvider(
            searchStore,
            topK: 3,      // Recupera i 3 chunk più rilevanti
            minScore: 0.3f // Score minimo di similarità
        );

        // System prompt per il Code Reviewer
        // L'agente è istruito a:
        // 1. Essere un esperto di code review
        // 2. Usare la knowledge base iniettata
        // 3. Citare le fonti nelle risposte
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

        // Crea l'agente con il provider RAG
        var chatClient = openAiClient.GetChatClient(ChatModel);
        ChatClientAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Name = "CodeReviewer",
            // Il factory crea il provider per ogni thread/conversazione
            // In questo caso usiamo sempre lo stesso provider
            AIContextProviderFactory = _ => knowledgeBaseProvider,
            // Opzioni per il modello
            ChatOptions = new ChatOptions
            {
                Temperature = 0.7f // Un po' di creatività, ma non troppo
            }
        });

        Console.WriteLine("✅ Code Reviewer Agent creato con RAG");

        // ====================================================================
        // STEP 5: DEMO INTERATTIVA
        // ====================================================================
        ConsoleHelper.WriteSeparator("Step 5: Demo Code Review con RAG");

        Console.WriteLine();
        Console.WriteLine("🎯 Chiedi al Code Reviewer qualsiasi cosa su:");
        Console.WriteLine("   - Naming conventions in C#");
        Console.WriteLine("   - Principi SOLID");
        Console.WriteLine("   - Gestione delle eccezioni");
        Console.WriteLine("   - Async/await best practices");
        Console.WriteLine();
        Console.WriteLine("L'agente userà la knowledge base per rispondere!");
        Console.WriteLine("Scrivi 'exit' per uscire.");
        Console.WriteLine();

        // Crea un thread di conversazione
        AgentThread thread = agent.GetNewThread();

        // Contatore per sapere se è il primo messaggio
        // (AgentThread non espone Messages direttamente)
        int messageCount = 0;

        // Loop di conversazione
        while (true)
        {
            Console.Write("Tu: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("👋 Arrivederci!");
                break;
            }

            Console.WriteLine();

            try
            {
                // Per il primo messaggio, includiamo le istruzioni come contesto
                var promptWithContext = messageCount == 0
                    ? $"[Contesto sistema: {systemPrompt}]\n\n{userInput}"
                    : userInput;

                // Invoca l'agente con streaming
                // InvokingAsync del provider viene chiamato automaticamente,
                // cercando nella knowledge base e iniettando il contesto
                ConsoleHelper.WriteAgentHeader();

                await foreach (var update in agent.RunStreamingAsync(promptWithContext, thread))
                {
                    ConsoleHelper.WriteStreamChunk(update.ToString());
                }

                ConsoleHelper.EndStreamLine();

                // Incrementa il contatore messaggi
                messageCount++;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Errore: {ex.Message}");
            }
        }

        // ====================================================================
        // RIEPILOGO
        // ====================================================================
        ConsoleHelper.WriteSeparator("Riepilogo");

        Console.WriteLine("📚 In questo progetto hai imparato:");
        Console.WriteLine("   1. Come implementare RAG con Microsoft Agent Framework");
        Console.WriteLine("   2. Chunking dei documenti per embedding efficiente");
        Console.WriteLine("   3. Ricerca semantica con vector store");
        Console.WriteLine("   4. AIContextProvider per iniettare knowledge dinamica");
        Console.WriteLine("   5. Citazione delle fonti nelle risposte");
        Console.WriteLine();
        Console.WriteLine("🔜 Nel prossimo progetto: Task Planner con obiettivi!");
    }
}
