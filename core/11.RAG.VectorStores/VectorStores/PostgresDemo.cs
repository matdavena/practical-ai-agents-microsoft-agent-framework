// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// FILE: VectorStores/PostgresDemo.cs
// ============================================================================
//
// DEMO RAG CON POSTGRESQL + PGVECTOR
//
// PostgreSQL con l'estensione pgvector è la soluzione open source più
// popolare per aggiungere capacità di ricerca vettoriale a un database
// relazionale.
//
// PERCHÉ PGVECTOR È COSÌ POPOLARE:
// - Open source e gratuito (nessuna licenza)
// - PostgreSQL è già ampiamente usato e conosciuto
// - Estensione matura con community attiva
// - Supportato da AWS RDS, Azure, Google Cloud, Supabase, Neon, etc.
// - Permette di combinare query SQL tradizionali con ricerca vettoriale
//
// TIPI DI INDICE SUPPORTATI:
// - HNSW: Hierarchical Navigable Small World (veloce, approssimato)
// - IVFFlat: Inverted File con Flat (buono per dataset molto grandi)
//
// NUGET PACKAGE:
// - Microsoft.SemanticKernel.Connectors.PgVector
// - NOTA: Rinominato da "Connectors.Postgres" a maggio 2025
//
// PREREQUISITI:
// 1. Docker Desktop in esecuzione
// 2. Container PostgreSQL avviato: docker compose up -d postgres
//
// CONNESSIONE:
// - Host: localhost
// - Port: 5433 (per evitare conflitti con PostgreSQL locale)
// - Database: vectorstore
// - User: postgres
// - Password: VectorStore123!
//
// ============================================================================

using _11.RAG.VectorStores.Data;
using Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.PgVector;
using Npgsql;
using OpenAI;

namespace _11.RAG.VectorStores.VectorStores;

/// <summary>
/// Dimostra l'utilizzo di PostgreSQL + pgvector come vector store per RAG.
/// </summary>
public static class PostgresDemo
{
    // Configurazione
    // NOTA: Porta 5433 per evitare conflitti con PostgreSQL locale (5432)
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=vectorstore;Username=postgres;Password=VectorStore123!";

    private const string CollectionName = "document_chunks";
    private const string EmbeddingModel = "text-embedding-3-small";
    private const string ChatModel = "gpt-4o-mini";

    public static async Task RunAsync()
    {
        ConsoleHelper.WriteTitle("RAG con PostgreSQL + pgvector");

        // ====================================================================
        // STEP 1: VERIFICA CONNESSIONE A POSTGRESQL
        // ====================================================================
        ConsoleHelper.WriteSeparator("1. Connessione a PostgreSQL");

        Console.WriteLine("Connessione a PostgreSQL (Docker, porta 5433)...");
        Console.WriteLine();

        NpgsqlDataSource dataSource;

        try
        {
            // Creiamo un NpgsqlDataSource per gestire le connessioni
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);

            // IMPORTANTE: Registriamo il tipo vector per pgvector
            // Questo permette a Npgsql di gestire correttamente i vettori
            dataSourceBuilder.UseVector();

            dataSource = dataSourceBuilder.Build();

            // Test connessione
            await using var testConn = await dataSource.OpenConnectionAsync();

            // Verifica versione PostgreSQL
            await using var versionCmd = testConn.CreateCommand();
            versionCmd.CommandText = "SELECT version()";
            var version = await versionCmd.ExecuteScalarAsync();

            Console.WriteLine("PostgreSQL raggiungibile!");
            Console.WriteLine($"Versione: {TruncateVersion(version?.ToString() ?? "")}");
            Console.WriteLine();

            // Verifica che pgvector sia installato
            await using var extensionCmd = testConn.CreateCommand();
            extensionCmd.CommandText = "SELECT extversion FROM pg_extension WHERE extname = 'vector'";
            var pgvectorVersion = await extensionCmd.ExecuteScalarAsync();

            if (pgvectorVersion != null)
            {
                Console.WriteLine($"pgvector installato! Versione: {pgvectorVersion}");
            }
            else
            {
                // Prova a creare l'estensione
                Console.WriteLine("pgvector non trovato, tentativo di installazione...");
                await using var createExtCmd = testConn.CreateCommand();
                createExtCmd.CommandText = "CREATE EXTENSION IF NOT EXISTS vector";
                await createExtCmd.ExecuteNonQueryAsync();
                Console.WriteLine("Estensione pgvector creata con successo!");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRORE: Impossibile connettersi a PostgreSQL!");
            Console.WriteLine($"Dettaglio: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Assicurati che:");
            Console.WriteLine("1. Docker Desktop sia in esecuzione");
            Console.WriteLine("2. Il container PostgreSQL sia avviato:");
            Console.WriteLine("   docker compose up -d postgres");
            Console.WriteLine("3. Attendi qualche secondo che PostgreSQL sia pronto");
            Console.WriteLine();
            Console.WriteLine("Premi un tasto per tornare al menu...");
            Console.ReadKey();
            return;
        }

        // ====================================================================
        // STEP 2: SETUP OPENAI EMBEDDING GENERATOR
        // ====================================================================
        ConsoleHelper.WriteSeparator("2. Setup Embedding Generator");

        var apiKey = ConfigurationHelper.GetOpenAiApiKey();
        var openAiClient = new OpenAIClient(apiKey);

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

        // PostgresVectorStore implementa IVectorStore
        // Usa tabelle PostgreSQL con colonne VECTOR per memorizzare gli embedding
        // Nota: la nuova API (maggio 2025) accetta direttamente la connection string
        var vectorStore = new PostgresVectorStore(ConnectionString);

        // Otteniamo la collezione (sarà una tabella in PostgreSQL)
        var collection = vectorStore.GetCollection<Guid, DocumentChunkPostgres>(CollectionName);

        // Crea la tabella se non esiste
        // La struttura viene inferita dagli attributi [VectorStore*] della classe
        await collection.EnsureCollectionExistsAsync();

        Console.WriteLine($"Collezione '{CollectionName}' pronta!");
        Console.WriteLine("(Corrisponde a una tabella PostgreSQL con colonna VECTOR)");
        Console.WriteLine();

        // Mostra la struttura della tabella
        await ShowTableStructureAsync(dataSource);

        // ====================================================================
        // STEP 4: INDICIZZAZIONE DOCUMENTI
        // ====================================================================
        ConsoleHelper.WriteSeparator("4. Indicizzazione Documenti");

        var chunks = SampleDocuments.GetChunksForPostgres().ToList();
        Console.WriteLine($"Documenti da indicizzare: {SampleDocuments.Documents.Length}");
        Console.WriteLine($"Chunk totali: {chunks.Count}");
        Console.WriteLine();

        Console.WriteLine("Generazione embedding...");

        // Generiamo gli embedding per tutti i chunk in batch
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

        var testQueries = new[]
        {
            "Come funziona LINQ?",
            "Cosa sono gli indici in un database?",
            "Come si implementa RAG?"
        };

        foreach (var query in testQueries)
        {
            Console.WriteLine($"Query: \"{query}\"");
            Console.WriteLine();

            var queryEmbedding = await embeddingGenerator.GenerateAsync(query);

            var searchOptions = new VectorSearchOptions<DocumentChunkPostgres>
            {
                IncludeVectors = false
            };

            var searchResults = collection.SearchAsync(queryEmbedding.Vector, 3, searchOptions);

            Console.WriteLine("Risultati:");
            await foreach (var result in searchResults)
            {
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
        Console.WriteLine("I chunk rilevanti verranno recuperati da PostgreSQL.");
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

            var questionEmbedding = await embeddingGenerator.GenerateAsync(question);

            var searchOptions = new VectorSearchOptions<DocumentChunkPostgres>
            {
                IncludeVectors = false
            };

            var relevantChunks = collection.SearchAsync(questionEmbedding.Vector, 3, searchOptions);

            var context = new List<string>();
            Console.WriteLine("Chunk recuperati da PostgreSQL:");
            await foreach (var result in relevantChunks)
            {
                context.Add(result.Record.Content);
                Console.WriteLine($"   - {result.Record.Title} (score: {result.Score:F4})");
            }
            Console.WriteLine();

            var ragPrompt = $"""
                Usa SOLO le seguenti informazioni per rispondere alla domanda.
                Se le informazioni non sono sufficienti, dillo chiaramente.

                CONTESTO:
                {string.Join("\n\n---\n\n", context)}

                DOMANDA: {question}

                RISPOSTA:
                """;

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

        Console.Write("Vuoi eliminare la tabella? (s/n): ");
        var delete = Console.ReadLine();

        if (delete?.Equals("s", StringComparison.OrdinalIgnoreCase) == true)
        {
            await collection.EnsureCollectionDeletedAsync();
            Console.WriteLine($"Tabella '{CollectionName}' eliminata!");
        }
        else
        {
            Console.WriteLine("Tabella mantenuta per usi futuri.");
            Console.WriteLine("Puoi esaminarla con pgAdmin o psql.");
        }

        Console.WriteLine();
        Console.WriteLine("Premi un tasto per tornare al menu...");
        Console.ReadKey();
    }

    /// <summary>
    /// Mostra la struttura della tabella creata.
    /// </summary>
    private static async Task ShowTableStructureAsync(NpgsqlDataSource dataSource)
    {
        Console.WriteLine("Struttura tabella PostgreSQL:");
        Console.WriteLine();

        try
        {
            await using var conn = await dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT column_name, data_type, character_maximum_length
                FROM information_schema.columns
                WHERE table_name = '{CollectionName}'
                ORDER BY ordinal_position
                """;

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var colName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLen = reader.IsDBNull(2) ? "" : $"({reader.GetInt32(2)})";

                // Evidenzia la colonna VECTOR
                if (dataType == "USER-DEFINED")
                {
                    Console.WriteLine($"   - {colName}: vector(1536)  <-- embedding!");
                }
                else
                {
                    Console.WriteLine($"   - {colName}: {dataType}{maxLen}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Impossibile leggere struttura: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static string Truncate(string text, int maxLength)
    {
        var clean = text.Replace("\n", " ").Replace("\r", "").Trim();
        while (clean.Contains("  "))
            clean = clean.Replace("  ", " ");

        return clean.Length <= maxLength
            ? clean
            : clean[..(maxLength - 3)] + "...";
    }

    private static string TruncateVersion(string version)
    {
        var firstLine = version.Split('\n')[0].Trim();
        return firstLine.Length > 70 ? firstLine[..70] + "..." : firstLine;
    }
}
