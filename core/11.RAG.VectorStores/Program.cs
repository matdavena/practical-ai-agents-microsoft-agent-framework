// ============================================================================
// 11. RAG WITH REAL VECTOR STORES
// ============================================================================
//
// This project demonstrates the use of real vector stores for RAG,
// as an alternative to the InMemoryVectorStore seen in project 05.
//
// AVAILABLE VECTOR STORES:
//
// 1. QDRANT (port 6333/6334)
//    - Native vector database, open-source
//    - Optimized for high-performance semantic search
//    - Integrated web dashboard: http://localhost:6333/dashboard
//    - Supports billions of vectors with advanced filtering
//
// 2. POSTGRESQL + PGVECTOR (port 5433)
//    - PostgreSQL with pgvector extension (open source)
//    - Most popular solution for vector search in RDBMS
//    - Supports HNSW and IVFFlat indexes
//    - Port 5433 to avoid conflicts with local installations
//
// 3. SQL SERVER (port 1434) - FUTURE
//    - Relational database with vector support (SQL Server 2025)
//    - NOTE: Requires SQL Server 2025 (not yet publicly available)
//    - Port 1434 to avoid conflicts with local installations
//
// PREREQUISITES:
// 1. Docker Desktop installed and running
// 2. Start containers with: docker compose up -d
// 3. Wait a few seconds for the databases to be ready
//
// VERIFY CONTAINERS:
// - docker compose ps
// - docker compose logs -f
//
// ============================================================================

using System.Text;
using _11.RAG.VectorStores.VectorStores;
using Common;

Console.OutputEncoding = Encoding.UTF8;

ConsoleHelper.WriteTitle("11. RAG with Real Vector Stores");

// ============================================================================
// VERIFY PREREQUISITES
// ============================================================================

Console.WriteLine("PREREQUISITES:");
Console.WriteLine("   1. Docker Desktop must be running");
Console.WriteLine("   2. Containers must be started:");
Console.WriteLine("      cd core/11.RAG.VectorStores");
Console.WriteLine("      docker compose up -d");
Console.WriteLine();
Console.WriteLine("CONFIGURED CONTAINERS:");
Console.WriteLine("   - Qdrant:      http://localhost:6333 (dashboard: /dashboard)");
Console.WriteLine("   - PostgreSQL:  localhost:5433 (postgres/VectorStore123!)");
Console.WriteLine("   - SQL Server:  localhost,1434 (sa/VectorStore123!) - SQL Server 2025");
Console.WriteLine();

// ============================================================================
// MAIN MENU
// ============================================================================

while (true)
{
    ConsoleHelper.WriteSeparator("Select Vector Store");

    Console.WriteLine("1. Qdrant      - Native vector database (port 6333)");
    Console.WriteLine("2. PostgreSQL  - pgvector, open source (port 5433)");
    Console.WriteLine("3. SQL Server  - REQUIRES SQL Server 2025 (not yet public)");
    Console.WriteLine("4. Comparison  - Show differences between approaches");
    Console.WriteLine("0. Exit");
    Console.WriteLine();

    Console.Write("Choice: ");
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
            Console.WriteLine("Invalid choice!");
            break;
    }
}

exit:

// ============================================================================
// FINAL SUMMARY
// ============================================================================

ConsoleHelper.WriteSeparator("Summary");

Console.WriteLine("In this project you learned:");
Console.WriteLine();
Console.WriteLine("1. QDRANT - Native Vector Database:");
Console.WriteLine("   - Optimized for high-performance semantic search");
Console.WriteLine("   - Web dashboard to visualize collections and vectors");
Console.WriteLine("   - Simple API: CreateCollection, Upsert, VectorSearch");
Console.WriteLine("   - Ideal for: large volumes, pure search, complex filtering");
Console.WriteLine();
Console.WriteLine("2. POSTGRESQL + PGVECTOR - Open Source RDBMS:");
Console.WriteLine("   - pgvector extension for PostgreSQL (open source)");
Console.WriteLine("   - Supports HNSW and IVFFlat indexes");
Console.WriteLine("   - Combines traditional SQL queries with vector search");
Console.WriteLine("   - Ideal for: those already using PostgreSQL, open source solution");
Console.WriteLine();
Console.WriteLine("3. SQL SERVER 2025 - Integrated Vector Support (future):");
Console.WriteLine("   - Vector features integrated into relational database");
Console.WriteLine("   - Join between vectors and structured data");
Console.WriteLine("   - NOTE: Requires SQL Server 2025 (not yet available)");
Console.WriteLine();
Console.WriteLine("4. KEY CONCEPTS:");
Console.WriteLine("   - [VectorStoreKey] - Identifies the primary key");
Console.WriteLine("   - [VectorStoreData] - Data/metadata fields");
Console.WriteLine("   - [VectorStoreVector] - Embedding field with dimensions");
Console.WriteLine("   - IVectorStore - Common interface for all backends");
Console.WriteLine("   - SearchAsync() - Similarity search");
Console.WriteLine();
Console.WriteLine("5. DOCKER COMPOSE:");
Console.WriteLine("   - docker compose up -d    -> Start containers");
Console.WriteLine("   - docker compose ps       -> Verify status");
Console.WriteLine("   - docker compose down     -> Stop containers");
Console.WriteLine("   - docker compose down -v  -> Also remove volumes");
Console.WriteLine();

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

void ShowComparison()
{
    ConsoleHelper.WriteSeparator("Vector Stores Comparison");

    Console.WriteLine("""
        ┌────────────────────┬──────────────────────┬──────────────────────┬──────────────────────┐
        │ Feature            │ Qdrant               │ PostgreSQL+pgvector  │ SQL Server 2025      │
        ├────────────────────┼──────────────────────┼──────────────────────┼──────────────────────┤
        │ Type               │ Native Vector DB     │ RDBMS + extension    │ RDBMS + vector       │
        │ License            │ Open source          │ Open source          │ Commercial           │
        │ Availability       │ Available now        │ Available now        │ Not yet public       │
        │ Performance        │ Excellent            │ Very good            │ Good                 │
        │ Scale              │ Billions of vectors  │ Millions of vectors  │ Millions of vectors  │
        │ Supported indexes  │ HNSW, Flat           │ HNSW, IVFFlat        │ Flat only            │
        │ Transactions       │ Eventual consistency │ Full ACID            │ Full ACID            │
        │ Join with data     │ No (metadata only)   │ Yes (native SQL)     │ Yes (native SQL)     │
        │ Setup              │ Simple (Docker)      │ Simple (Docker)      │ Not available        │
        │ Cloud support      │ Qdrant Cloud         │ AWS/Azure/GCP/Neon   │ Azure SQL            │
        └────────────────────┴──────────────────────┴──────────────────────┴──────────────────────┘

        CURRENT STATUS (December 2024):
        - Qdrant: Production ready, excellent for pure vector search
        - PostgreSQL + pgvector: Production ready, excellent for existing RDBMS
        - SQL Server: VECTOR type requires SQL Server 2025 (not yet available)

        WHEN TO USE QDRANT:
        - Projects requiring massive scale (billions of vectors)
        - Critical performance in semantic search
        - Complex filtering during search
        - No need for traditional SQL joins

        WHEN TO USE POSTGRESQL + PGVECTOR (recommended for RDBMS):
        - Want to combine vector search with traditional SQL queries
        - Already have PostgreSQL in your stack
        - Prefer open source solutions
        - Want HNSW indexes for fast approximate search

        WHEN TO USE SQL SERVER (future):
        - When SQL Server 2025 becomes available
        - Existing stack based on SQL Server/.NET
        - SQL Server license already available
        """);

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}
