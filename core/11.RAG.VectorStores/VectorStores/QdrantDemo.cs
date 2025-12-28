// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// FILE: VectorStores/QdrantDemo.cs
// ============================================================================
//
// DEMO RAG CON QDRANT
//
// Qdrant è un vector database open-source ottimizzato per:
// - Ricerca semantica ad alte prestazioni
// - Supporto per miliardi di vettori
// - Filtering avanzato durante la ricerca
// - API REST e gRPC native
//
// PREREQUISITI:
// 1. Docker Desktop in esecuzione
// 2. Container Qdrant avviato: docker compose up -d qdrant
//
// ENDPOINT:
// - REST API: http://localhost:6333
// - Dashboard: http://localhost:6333/dashboard
// - gRPC: localhost:6334
//
// ============================================================================

using _11.RAG.VectorStores.Data;
using Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using Qdrant.Client;

namespace _11.RAG.VectorStores.VectorStores;

/// <summary>
/// Dimostra l'utilizzo di Qdrant come vector store per RAG.
/// </summary>
public static class QdrantDemo
{
    // Configurazione
    // NOTA: Porta 6334 per gRPC (il client C# usa gRPC)
    // La porta 6333 è per REST API e Dashboard web
    private const string QdrantHost = "localhost";
    private const int QdrantGrpcPort = 6334;
    private const string CollectionName = "learning-documents";
    private const string EmbeddingModel = "text-embedding-3-small";
    private const string ChatModel = "gpt-4o-mini";

    public static async Task RunAsync()
    {
        ConsoleHelper.WriteTitle("RAG con Qdrant Vector Store");

        // ====================================================================
        // STEP 1: VERIFICA CONNESSIONE A QDRANT
        // ====================================================================
        ConsoleHelper.WriteSeparator("1. Connessione a Qdrant");

        Console.WriteLine($"Connessione a Qdrant via gRPC: {QdrantHost}:{QdrantGrpcPort}");
        Console.WriteLine();

        // QdrantClient è il client ufficiale per comunicare con Qdrant
        // Usa gRPC sulla porta 6334 per performance migliori
        var qdrantClient = new QdrantClient(QdrantHost, QdrantGrpcPort);

        try
        {
            // Verifica che Qdrant sia raggiungibile
            var collections = await qdrantClient.ListCollectionsAsync();
            Console.WriteLine($"Qdrant raggiungibile! Collezioni esistenti: {collections.Count}");

            foreach (var col in collections)
            {
                Console.WriteLine($"   - {col}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRORE: Impossibile connettersi a Qdrant!");
            Console.WriteLine($"Dettaglio: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Assicurati che:");
            Console.WriteLine("1. Docker Desktop sia in esecuzione");
            Console.WriteLine("2. Il container Qdrant sia avviato:");
            Console.WriteLine("   docker compose up -d qdrant");
            Console.WriteLine();
            Console.WriteLine("Premi un tasto per tornare al menu...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine();

        // ====================================================================
        // STEP 2: SETUP OPENAI EMBEDDING GENERATOR
        // ====================================================================
        ConsoleHelper.WriteSeparator("2. Setup Embedding Generator");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);

        // IEmbeddingGenerator<string, Embedding<float>> è l'interfaccia standard
        // di Microsoft.Extensions.AI per generare embedding
        var embeddingGenerator = openAiClient
            .GetEmbeddingClient(EmbeddingModel)
            .AsIEmbeddingGenerator();

        Console.WriteLine($"Embedding model: {EmbeddingModel}");
        Console.WriteLine("Embedding generator pronto!");
        Console.WriteLine();

        // ====================================================================
        // STEP 3: CREAZIONE VECTOR STORE E COLLECTION
        // ====================================================================
        ConsoleHelper.WriteSeparator("3. Creazione Vector Store");

        // QdrantVectorStore implementa IVectorStore di Microsoft.Extensions.VectorData
        // Il secondo parametro (ownsClient) indica se il vector store deve
        // gestire il ciclo di vita del client Qdrant
        var vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: false);

        // GetCollection<TKey, TRecord> ottiene un riferimento alla collezione
        // NOTA: Qdrant richiede chiavi Guid o ulong, non string
        // Usiamo DocumentChunkQdrant che ha indice HNSW
        var collection = vectorStore.GetCollection<Guid, DocumentChunkQdrant>(CollectionName);

        // EnsureCollectionExistsAsync crea la collezione se non esiste
        // La struttura viene inferita dagli attributi [VectorStore*] della classe
        await collection.EnsureCollectionExistsAsync();

        Console.WriteLine($"Collezione '{CollectionName}' pronta!");
        Console.WriteLine();

        // ====================================================================
        // STEP 4: INDICIZZAZIONE DOCUMENTI
        // ====================================================================
        ConsoleHelper.WriteSeparator("4. Indicizzazione Documenti");

        // Otteniamo tutti i chunk dai documenti di esempio (versione Qdrant)
        var chunks = SampleDocuments.GetChunksForQdrant().ToList();
        Console.WriteLine($"Documenti da indicizzare: {SampleDocuments.Documents.Length}");
        Console.WriteLine($"Chunk totali: {chunks.Count}");
        Console.WriteLine();

        // IMPORTANTE: Per Qdrant (e SQL Server) dobbiamo generare gli embedding
        // manualmente PRIMA dell'inserimento. InMemoryVectorStore lo fa automaticamente,
        // ma i connector per database reali richiedono embedding pre-calcolati.

        Console.WriteLine("Generazione embedding...");

        // Generiamo gli embedding per tutti i chunk in batch (più efficiente)
        var textsForEmbedding = chunks.Select(c => c.GetTextForEmbedding()).ToList();
        var embeddings = await embeddingGenerator.GenerateAsync(textsForEmbedding);

        // Associamo ogni embedding al rispettivo chunk
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Embedding = embeddings[i].Vector;
        }

        Console.WriteLine($"   Generati {embeddings.Count} embedding!");
        Console.WriteLine();

        Console.WriteLine("Inserimento nel vector store...");

        var count = 0;
        foreach (var chunk in chunks)
        {
            // UpsertAsync inserisce o aggiorna se l'ID esiste già
            await collection.UpsertAsync(chunk);
            count++;
            Console.Write($"\r   Chunk inseriti: {count}/{chunks.Count}");
        }

        Console.WriteLine();
        Console.WriteLine("Indicizzazione completata!");
        Console.WriteLine();

        // ====================================================================
        // STEP 5: RICERCA SEMANTICA
        // ====================================================================
        ConsoleHelper.WriteSeparator("5. Test Ricerca Semantica");

        // Query di test
        var testQueries = new[]
        {
            "Come funziona LINQ in C#?",
            "Cosa sono le transazioni ACID?",
            "Cos'è il RAG e come funziona?"
        };

        foreach (var query in testQueries)
        {
            Console.WriteLine($"Query: \"{query}\"");
            Console.WriteLine();

            // Generiamo l'embedding della query manualmente
            var queryEmbedding = await embeddingGenerator.GenerateAsync(query);

            // Creiamo le opzioni di ricerca
            var searchOptions = new VectorSearchOptions<DocumentChunkQdrant>
            {
                IncludeVectors = false
            };

            // Cerchiamo i chunk più simili usando il vettore
            // SearchAsync(vector, topK, options) - topK è un parametro separato
            var searchResults = collection.SearchAsync(queryEmbedding.Vector, 3, searchOptions);

            Console.WriteLine("Risultati:");
            await foreach (var result in searchResults)
            {
                // Score indica la similarità (più alto = più simile per cosine)
                Console.WriteLine($"   [{result.Score:F4}] {result.Record.Title} (chunk {result.Record.ChunkIndex})");
                Console.WriteLine($"            {Truncate(result.Record.Content, 80)}");
            }

            Console.WriteLine();
        }

        // ====================================================================
        // STEP 6: RAG COMPLETO CON LLM
        // ====================================================================
        ConsoleHelper.WriteSeparator("6. RAG Completo con LLM");

        var chatClient = openAiClient.GetChatClient(ChatModel).AsIChatClient();

        Console.WriteLine("Ora puoi fare domande sui documenti indicizzati.");
        Console.WriteLine("I chunk rilevanti verranno recuperati e passati all'LLM.");
        Console.WriteLine("Scrivi 'exit' per tornare al menu.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Domanda: ");
            var question = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(question))
                continue;

            if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            Console.WriteLine();

            // 1. Genera embedding della domanda
            var questionEmbedding = await embeddingGenerator.GenerateAsync(question);

            // 2. Recupera i chunk rilevanti
            var searchOptions = new VectorSearchOptions<DocumentChunkQdrant>
            {
                IncludeVectors = false
            };

            var relevantChunks = collection.SearchAsync(questionEmbedding.Vector, 3, searchOptions);

            var context = new List<string>();
            Console.WriteLine("Chunk recuperati:");
            await foreach (var result in relevantChunks)
            {
                context.Add(result.Record.Content);
                Console.WriteLine($"   - {result.Record.Title} (score: {result.Score:F4})");
            }
            Console.WriteLine();

            // 3. Costruisci il prompt con contesto RAG
            var ragPrompt = $"""
                Usa SOLO le seguenti informazioni per rispondere alla domanda.
                Se le informazioni non sono sufficienti, dillo chiaramente.

                CONTESTO:
                {string.Join("\n\n---\n\n", context)}

                DOMANDA: {question}

                RISPOSTA:
                """;

            // 4. Genera risposta con LLM
            Console.Write("Risposta: ");
            await foreach (var chunk in chatClient.GetStreamingResponseAsync(ragPrompt))
            {
                Console.Write(chunk);
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        // ====================================================================
        // STEP 7: PULIZIA (OPZIONALE)
        // ====================================================================
        ConsoleHelper.WriteSeparator("7. Pulizia");

        Console.Write("Vuoi eliminare la collezione? (s/n): ");
        var delete = Console.ReadLine();

        if (delete?.Equals("s", StringComparison.OrdinalIgnoreCase) == true)
        {
            await collection.EnsureCollectionDeletedAsync();
            Console.WriteLine($"Collezione '{CollectionName}' eliminata!");
        }
        else
        {
            Console.WriteLine("Collezione mantenuta per usi futuri.");
            Console.WriteLine($"Dashboard: http://localhost:6333/dashboard");
        }

        Console.WriteLine();
        Console.WriteLine("Premi un tasto per tornare al menu...");
        Console.ReadKey();
    }

    private static string Truncate(string text, int maxLength)
    {
        // Rimuovi newline e spazi extra
        var clean = text.Replace("\n", " ").Replace("\r", "").Trim();
        while (clean.Contains("  "))
            clean = clean.Replace("  ", " ");

        return clean.Length <= maxLength
            ? clean
            : clean[..(maxLength - 3)] + "...";
    }
}
