// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// FILE: VectorStores/SqlServerDemo.cs
// ============================================================================
//
// DEMO RAG CON SQL SERVER
//
// SQL Server 2022 introduce supporto nativo per i vettori:
// - Tipo di dato VECTOR per memorizzare embedding
// - Funzione VECTOR_DISTANCE per calcolare similarità
// - Supporto per cosine, euclidean e dot product
//
// VANTAGGI di usare SQL Server come vector store:
// - Nessun database aggiuntivo da gestire
// - Transazioni ACID su dati + vettori
// - Join con tabelle relazionali esistenti
// - Familiarità per chi già usa SQL Server
//
// PREREQUISITI:
// 1. Docker Desktop in esecuzione
// 2. Container SQL Server avviato: docker compose up -d sqlserver
//
// CONNESSIONE:
// - Server: localhost,1434 (porta 1434 per evitare conflitti)
// - User: sa
// - Password: VectorStore123!
//
// ============================================================================

using _11.RAG.VectorStores.Data;
using Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using OpenAI;

namespace _11.RAG.VectorStores.VectorStores;

/// <summary>
/// Dimostra l'utilizzo di SQL Server come vector store per RAG.
/// </summary>
public static class SqlServerDemo
{
    // Configurazione
    // NOTA: Porta 1434 per evitare conflitti con SQL Server locale (1433)
    private const string MasterConnectionString =
        "Server=localhost,1434;Database=master;User Id=sa;Password=VectorStore123!;TrustServerCertificate=True;";

    private const string ConnectionString =
        "Server=localhost,1434;Database=VectorStoreDemo;User Id=sa;Password=VectorStore123!;TrustServerCertificate=True;";

    private const string DatabaseName = "VectorStoreDemo";

    private const string CollectionName = "DocumentChunks";
    private const string EmbeddingModel = "text-embedding-3-small";
    private const string ChatModel = "gpt-4o-mini";

    public static async Task RunAsync()
    {
        ConsoleHelper.WriteTitle("RAG con SQL Server Vector Store");

        // ====================================================================
        // AVVISO IMPORTANTE
        // ====================================================================
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ATTENZIONE: SQL Server VECTOR richiede SQL Server 2025!     ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  Il tipo VECTOR nativo è disponibile SOLO in SQL Server 2025 ║");
        Console.WriteLine("║  che è attualmente in preview privata (non pubblicamente     ║");
        Console.WriteLine("║  disponibile su Docker Hub).                                 ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║  SQL Server 2022 NON supporta il tipo VECTOR.                ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║  Per testare vector store con database relazionali,          ║");
        Console.WriteLine("║  considera PostgreSQL con pgvector come alternativa.         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.Write("Vuoi continuare comunque? (s/n): ");
        var continueAnyway = Console.ReadLine();
        if (!continueAnyway?.Equals("s", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }
        Console.WriteLine();

        // ====================================================================
        // STEP 1: VERIFICA CONNESSIONE A SQL SERVER
        // ====================================================================
        ConsoleHelper.WriteSeparator("1. Connessione a SQL Server");

        Console.WriteLine("Connessione a SQL Server (Docker, porta 1434)...");
        Console.WriteLine();

        try
        {
            // Prima ci connettiamo a master per verificare SQL Server e creare il database
            await using var masterConn = new SqlConnection(MasterConnectionString);
            await masterConn.OpenAsync();

            // Verifica versione
            await using var versionCmd = masterConn.CreateCommand();
            versionCmd.CommandText = "SELECT @@VERSION";
            var version = await versionCmd.ExecuteScalarAsync();

            Console.WriteLine("SQL Server raggiungibile!");
            Console.WriteLine($"Versione: {TruncateVersion(version?.ToString() ?? "")}");
            Console.WriteLine();

            // Verifica che sia SQL Server 2022 o superiore
            var versionStr = version?.ToString() ?? "";
            if (!versionStr.Contains("2022") && !versionStr.Contains("2025"))
            {
                Console.WriteLine("ATTENZIONE: Il supporto vettoriale nativo richiede SQL Server 2022+");
                Console.WriteLine("Il connector potrebbe usare un'implementazione alternativa.");
                Console.WriteLine();
            }

            // Crea il database se non esiste
            Console.WriteLine($"Verifica database '{DatabaseName}'...");
            await using var createDbCmd = masterConn.CreateCommand();
            createDbCmd.CommandText = $"""
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{DatabaseName}')
                BEGIN
                    CREATE DATABASE [{DatabaseName}]
                    PRINT 'Database creato'
                END
                ELSE
                BEGIN
                    PRINT 'Database esistente'
                END
                """;
            await createDbCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Database '{DatabaseName}' pronto!");
            Console.WriteLine();
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"ERRORE: Impossibile connettersi a SQL Server!");
            Console.WriteLine($"Dettaglio: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Assicurati che:");
            Console.WriteLine("1. Docker Desktop sia in esecuzione");
            Console.WriteLine("2. Il container SQL Server sia avviato:");
            Console.WriteLine("   docker compose up -d sqlserver");
            Console.WriteLine("3. Attendi 30-60 secondi dopo l'avvio (SQL Server impiega tempo)");
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

        // SqlServerVectorStore implementa IVectorStore
        // Usa una tabella SQL per memorizzare i record con i vettori
        var vectorStore = new SqlServerVectorStore(ConnectionString);

        // Otteniamo la collezione (sarà una tabella in SQL Server)
        // Usiamo DocumentChunkSqlServer che ha indice Flat (SQL Server non supporta HNSW)
        var collection = vectorStore.GetCollection<Guid, DocumentChunkSqlServer>(CollectionName);

        // Crea la tabella se non esiste, con le colonne appropriate:
        // - Id (chiave primaria)
        // - Title, Category, Content (campi dati)
        // - ChunkIndex (intero)
        // - EmbeddingText_Embedding (tipo VECTOR o fallback)
        await collection.EnsureCollectionExistsAsync();

        Console.WriteLine($"Collezione '{CollectionName}' pronta!");
        Console.WriteLine("(Corrisponde a una tabella SQL Server)");
        Console.WriteLine();

        // ====================================================================
        // STEP 4: INDICIZZAZIONE DOCUMENTI
        // ====================================================================
        ConsoleHelper.WriteSeparator("4. Indicizzazione Documenti");

        var chunks = SampleDocuments.GetChunksForSqlServer().ToList();
        Console.WriteLine($"Documenti da indicizzare: {SampleDocuments.Documents.Length}");
        Console.WriteLine($"Chunk totali: {chunks.Count}");
        Console.WriteLine();

        // IMPORTANTE: Per SQL Server (e Qdrant) dobbiamo generare gli embedding
        // manualmente PRIMA dell'inserimento.

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

        // Mostra la struttura della tabella creata
        await ShowTableStructureAsync();

        // ====================================================================
        // STEP 5: RICERCA SEMANTICA
        // ====================================================================
        ConsoleHelper.WriteSeparator("5. Test Ricerca Semantica");

        var testQueries = new[]
        {
            "Come funziona la programmazione asincrona?",
            "Quali sono i tipi di indici in SQL Server?",
            "Come si implementa un sistema RAG?"
        };

        foreach (var query in testQueries)
        {
            Console.WriteLine($"Query: \"{query}\"");
            Console.WriteLine();

            var queryEmbedding = await embeddingGenerator.GenerateAsync(query);

            var searchOptions = new VectorSearchOptions<DocumentChunkSqlServer>
            {
                IncludeVectors = false
            };

            // SearchAsync esegue una query che calcola la distanza
            // tra il vettore della query e i vettori nel database
            // SearchAsync(vector, topK, options) - topK è un parametro separato
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
        Console.WriteLine("I chunk rilevanti verranno recuperati da SQL Server.");
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

            var searchOptions = new VectorSearchOptions<DocumentChunkSqlServer>
            {
                IncludeVectors = false
            };

            var relevantChunks = collection.SearchAsync(questionEmbedding.Vector, 3, searchOptions);

            var context = new List<string>();
            Console.WriteLine("Chunk recuperati da SQL Server:");
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
            Console.WriteLine("Puoi esaminarla con SSMS o Azure Data Studio.");
        }

        Console.WriteLine();
        Console.WriteLine("Premi un tasto per tornare al menu...");
        Console.ReadKey();
    }

    /// <summary>
    /// Mostra la struttura della tabella creata dal vector store.
    /// </summary>
    private static async Task ShowTableStructureAsync()
    {
        Console.WriteLine("Struttura tabella SQL Server:");
        Console.WriteLine();

        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = '{CollectionName}'
                ORDER BY ORDINAL_POSITION
                """;

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var colName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLen = reader.IsDBNull(2) ? "" : $"({reader.GetInt32(2)})";

                Console.WriteLine($"   - {colName}: {dataType}{maxLen}");
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
        // Prendi solo la prima riga della versione
        var firstLine = version.Split('\n')[0].Trim();
        return firstLine.Length > 80 ? firstLine[..80] + "..." : firstLine;
    }
}
