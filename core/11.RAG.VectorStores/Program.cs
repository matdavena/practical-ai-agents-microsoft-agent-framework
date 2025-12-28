// ============================================================================
// 11. RAG CON VECTOR STORES REALI
// ============================================================================
//
// Questo progetto dimostra l'utilizzo di vector store reali per RAG,
// in alternativa al InMemoryVectorStore visto nel progetto 05.
//
// VECTOR STORE DISPONIBILI:
//
// 1. QDRANT (porta 6333/6334)
//    - Database vettoriale nativo, open-source
//    - Ottimizzato per ricerca semantica ad alte prestazioni
//    - Dashboard web integrato: http://localhost:6333/dashboard
//    - Supporta miliardi di vettori con filtering avanzato
//
// 2. POSTGRESQL + PGVECTOR (porta 5433)
//    - PostgreSQL con estensione pgvector (open source)
//    - Soluzione più popolare per vector search in RDBMS
//    - Supporta indici HNSW e IVFFlat
//    - Porta 5433 per evitare conflitti con installazioni locali
//
// 3. SQL SERVER (porta 1434) - FUTURO
//    - Database relazionale con supporto vettoriale (SQL Server 2025)
//    - NOTA: Richiede SQL Server 2025 (non ancora pubblicamente disponibile)
//    - Porta 1434 per evitare conflitti con installazioni locali
//
// PREREQUISITI:
// 1. Docker Desktop installato e in esecuzione
// 2. Avvia i container con: docker compose up -d
// 3. Attendi qualche secondo che i database siano pronti
//
// VERIFICA CONTAINER:
// - docker compose ps
// - docker compose logs -f
//
// ============================================================================

using System.Text;
using _11.RAG.VectorStores.VectorStores;
using Common;

Console.OutputEncoding = Encoding.UTF8;

ConsoleHelper.WriteTitle("11. RAG con Vector Stores Reali");

// ============================================================================
// VERIFICA PREREQUISITI
// ============================================================================

Console.WriteLine("PREREQUISITI:");
Console.WriteLine("   1. Docker Desktop deve essere in esecuzione");
Console.WriteLine("   2. I container devono essere avviati:");
Console.WriteLine("      cd core/11.RAG.VectorStores");
Console.WriteLine("      docker compose up -d");
Console.WriteLine();
Console.WriteLine("CONTAINER CONFIGURATI:");
Console.WriteLine("   - Qdrant:      http://localhost:6333 (dashboard: /dashboard)");
Console.WriteLine("   - PostgreSQL:  localhost:5433 (postgres/VectorStore123!)");
Console.WriteLine("   - SQL Server:  localhost,1434 (sa/VectorStore123!) - SQL Server 2025");
Console.WriteLine();

// ============================================================================
// MENU PRINCIPALE
// ============================================================================

while (true)
{
    ConsoleHelper.WriteSeparator("Seleziona Vector Store");

    Console.WriteLine("1. Qdrant      - Vector database nativo (porta 6333)");
    Console.WriteLine("2. PostgreSQL  - pgvector, open source (porta 5433)");
    Console.WriteLine("3. SQL Server  - RICHIEDE SQL Server 2025 (non ancora pubblico)");
    Console.WriteLine("4. Confronto   - Mostra differenze tra gli approcci");
    Console.WriteLine("0. Esci");
    Console.WriteLine();

    Console.Write("Scelta: ");
    var choice = Console.ReadLine();

    Console.WriteLine();

    switch (choice)
    {
        case "1":
            await QdrantDemo.RunAsync();
            break;

        case "2":
            await PostgresDemo.RunAsync();
            break;

        case "3":
            await SqlServerDemo.RunAsync();
            break;

        case "4":
            ShowComparison();
            break;

        case "0":
            goto exit;

        default:
            Console.WriteLine("Scelta non valida!");
            break;
    }
}

exit:

// ============================================================================
// RIEPILOGO FINALE
// ============================================================================

ConsoleHelper.WriteSeparator("Riepilogo");

Console.WriteLine("In questo progetto hai imparato:");
Console.WriteLine();
Console.WriteLine("1. QDRANT - Vector Database Nativo:");
Console.WriteLine("   - Ottimizzato per ricerca semantica ad alte prestazioni");
Console.WriteLine("   - Dashboard web per visualizzare collezioni e vettori");
Console.WriteLine("   - API semplice: CreateCollection, Upsert, VectorSearch");
Console.WriteLine("   - Ideale per: grandi volumi, ricerca pura, filtering complesso");
Console.WriteLine();
Console.WriteLine("2. POSTGRESQL + PGVECTOR - RDBMS Open Source:");
Console.WriteLine("   - Estensione pgvector per PostgreSQL (open source)");
Console.WriteLine("   - Supporta indici HNSW e IVFFlat");
Console.WriteLine("   - Combina query SQL tradizionali con ricerca vettoriale");
Console.WriteLine("   - Ideale per: chi usa già PostgreSQL, soluzione open source");
Console.WriteLine();
Console.WriteLine("3. SQL SERVER 2025 - Vector Support Integrato (futuro):");
Console.WriteLine("   - Funzionalità vettoriali integrate nel database relazionale");
Console.WriteLine("   - Join tra vettori e dati strutturati");
Console.WriteLine("   - NOTA: Richiede SQL Server 2025 (non ancora disponibile)");
Console.WriteLine();
Console.WriteLine("4. CONCETTI CHIAVE:");
Console.WriteLine("   - [VectorStoreKey] - Identifica la chiave primaria");
Console.WriteLine("   - [VectorStoreData] - Campi dati/metadati");
Console.WriteLine("   - [VectorStoreVector] - Campo embedding con dimensioni");
Console.WriteLine("   - IVectorStore - Interfaccia comune per tutti i backend");
Console.WriteLine("   - SearchAsync() - Ricerca per similarità");
Console.WriteLine();
Console.WriteLine("5. DOCKER COMPOSE:");
Console.WriteLine("   - docker compose up -d    -> Avvia i container");
Console.WriteLine("   - docker compose ps       -> Verifica stato");
Console.WriteLine("   - docker compose down     -> Ferma container");
Console.WriteLine("   - docker compose down -v  -> Rimuove anche i volumi");
Console.WriteLine();

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

void ShowComparison()
{
    ConsoleHelper.WriteSeparator("Confronto Vector Stores");

    Console.WriteLine("""
        ┌────────────────────┬──────────────────────┬──────────────────────┬──────────────────────┐
        │ Caratteristica     │ Qdrant               │ PostgreSQL+pgvector  │ SQL Server 2025      │
        ├────────────────────┼──────────────────────┼──────────────────────┼──────────────────────┤
        │ Tipo               │ Vector DB nativo     │ RDBMS + estensione   │ RDBMS + vector       │
        │ Licenza            │ Open source          │ Open source          │ Commerciale          │
        │ Disponibilita      │ Disponibile ora      │ Disponibile ora      │ Non ancora pubblico  │
        │ Performance        │ Eccellente           │ Molto buona          │ Buona                │
        │ Scala              │ Miliardi di vettori  │ Milioni di vettori   │ Milioni di vettori   │
        │ Indici supportati  │ HNSW, Flat           │ HNSW, IVFFlat        │ Solo Flat            │
        │ Transazioni        │ Eventual consistency │ ACID completo        │ ACID completo        │
        │ Join con dati      │ No (solo metadati)   │ Si (SQL nativo)      │ Si (SQL nativo)      │
        │ Setup              │ Semplice (Docker)    │ Semplice (Docker)    │ Non disponibile      │
        │ Cloud support      │ Qdrant Cloud         │ AWS/Azure/GCP/Neon   │ Azure SQL            │
        └────────────────────┴──────────────────────┴──────────────────────┴──────────────────────┘

        STATO ATTUALE (Dicembre 2024):
        - Qdrant: Pronto per produzione, ottimo per vector search puro
        - PostgreSQL + pgvector: Pronto per produzione, ottimo per RDBMS esistenti
        - SQL Server: Il tipo VECTOR richiede SQL Server 2025 (non ancora disponibile)

        QUANDO USARE QDRANT:
        - Progetti che richiedono massive scale (miliardi di vettori)
        - Performance critica nella ricerca semantica
        - Filtering complesso durante la ricerca
        - Non hai bisogno di join SQL tradizionali

        QUANDO USARE POSTGRESQL + PGVECTOR (consigliato per RDBMS):
        - Vuoi combinare vector search con query SQL tradizionali
        - Hai gia PostgreSQL nel tuo stack
        - Preferisci soluzioni open source
        - Vuoi indici HNSW per ricerca approssimata veloce

        QUANDO USARE SQL SERVER (futuro):
        - Quando SQL Server 2025 sara disponibile
        - Stack esistente basato su SQL Server/.NET
        - Licenza SQL Server gia disponibile
        """);

    Console.WriteLine();
    Console.WriteLine("Premi un tasto per continuare...");
    Console.ReadKey();
}
