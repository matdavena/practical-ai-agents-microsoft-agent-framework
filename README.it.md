# Learning Agent Framework

> Un percorso di apprendimento completo per Microsoft Agent Framework con progetti pratici

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Microsoft Agent Framework](https://img.shields.io/badge/Microsoft-Agent%20Framework-green)](https://github.com/microsoft/agents)

## Panoramica

Questo repository fornisce un percorso di apprendimento strutturato e progressivo per costruire agenti AI usando **Microsoft Agent Framework**. Ogni progetto si basa sul precedente, introducendo nuovi concetti e tecniche per creare applicazioni AI sofisticate.

## Prerequisiti

- **.NET 10 SDK** (o successivo)
- **OpenAI API Key** (per i modelli GPT)
- **Docker Desktop** (per i progetti con vector store)
- **Visual Studio 2022** o **VS Code** con estensione C#

## Avvio Rapido

```bash
# Clona il repository
git clone https://github.com/yourusername/LearningAgentFramework.git
cd LearningAgentFramework

# Imposta la tua API key OpenAI
# Windows PowerShell:
$env:OPENAI_API_KEY = "la-tua-api-key"

# Esegui il primo progetto
cd core/01.HelloAgent
dotnet run
```

## Percorso di Apprendimento

| # | Progetto | Concetti | Difficoltà |
|---|----------|----------|------------|
| 01 | [Hello Agent](core/01.HelloAgent/) | Agente base, client OpenAI, streaming | ⭐ Principiante |
| 02 | [DevAssistant - Tools](core/02.DevAssistant.Tools/) | Function calling, AIFunctionFactory | ⭐ Principiante |
| 03 | [DevAssistant - Memory](core/03.DevAssistant.Memory/) | AgentThread, memoria a breve termine | ⭐⭐ Intermedio |
| 04 | [DevAssistant - Long-Term Memory](core/04.DevAssistant.LongTermMemory/) | Memoria persistente, gestione sessioni | ⭐⭐ Intermedio |
| 05 | [Code Reviewer - RAG](core/05.CodeReviewer.RAG/) | Embeddings, ricerca vettoriale, pattern RAG | ⭐⭐ Intermedio |
| 06 | [Task Planner](core/06.TaskPlanner/) | Dependency injection, agenti strutturati | ⭐⭐ Intermedio |
| 07 | [DevTeam - Multi-Agent](core/07.DevTeam.MultiAgent/) | Collaborazione agenti, comunicazione A2A | ⭐⭐⭐ Avanzato |
| 08 | [Workflows](core/08.Workflows.Native/) | Orchestrazione workflow, task multi-step | ⭐⭐⭐ Avanzato |
| 09 | [MCP Integration](core/09.MCP.Integration/) | Model Context Protocol, tool esterni | ⭐⭐⭐ Avanzato |
| 10 | [MCP Custom Server](core/10.MCP.CustomServer/) | Creare server MCP, esporre tools | ⭐⭐⭐ Avanzato |
| 11 | [RAG con Vector Stores](core/11.RAG.VectorStores/) | Qdrant, PostgreSQL+pgvector, RAG produzione | ⭐⭐⭐ Avanzato |

## Struttura del Progetto

```
LearningAgentFramework/
├── core/                              # Progetti di apprendimento principali
│   ├── 01.HelloAgent/                 # Primo agente
│   ├── 02.DevAssistant.Tools/         # Tools/Function calling
│   ├── 03.DevAssistant.Memory/        # Memoria a breve termine
│   ├── 04.DevAssistant.LongTermMemory/# Memoria a lungo termine
│   ├── 05.CodeReviewer.RAG/           # Basi RAG
│   ├── 06.TaskPlanner/                # DI e agenti strutturati
│   ├── 07.DevTeam.MultiAgent/         # Sistemi multi-agente
│   ├── 08.Workflows.Native/           # Orchestrazione workflow
│   ├── 09.MCP.Integration/            # Integrazione client MCP
│   ├── 10.MCP.CustomServer/           # Server MCP personalizzato
│   └── 11.RAG.VectorStores/           # Vector stores produzione
├── shared/
│   └── Common/                        # Utility condivise
├── Directory.Build.props              # Impostazioni build comuni
├── Directory.Packages.props           # Versioni pacchetti centralizzate
└── README.md                          # Questo file
```

## Concetti Chiave Trattati

### Concetti Core degli Agenti
- **ChatClientAgent** - La classe agente principale
- **AgentThread** - Gestione del contesto conversazione
- **System Prompts** - Definizione del comportamento agente
- **Streaming Responses** - Output in tempo reale

### Tools & Function Calling
- **AIFunctionFactory** - Creare tools da metodi .NET
- **Attributo [Description]** - Documentare tools per l'LLM
- **Tools Statici vs Istanza** - Pattern di registrazione diversi
- **Sicurezza Tools** - Sandboxing e validazione

### Pattern di Memoria
- **Memoria a Breve Termine** - Contesto basato su AgentThread
- **Memoria a Lungo Termine** - Pattern di storage persistente
- **Memoria Vettoriale** - Ricerca semantica con embeddings

### RAG (Retrieval-Augmented Generation)
- **Embeddings** - Conversione testo in vettori
- **Vector Stores** - Qdrant, PostgreSQL+pgvector
- **Ricerca Semantica** - Trovare contesto rilevante
- **Strategie di Chunking** - Elaborazione documenti

### Sistemi Multi-Agente
- **Collaborazione Agenti** - Più agenti che lavorano insieme
- **Comunicazione A2A** - Messaggistica agent-to-agent
- **Orchestrazione Workflow** - Coordinamento task multi-step

### MCP (Model Context Protocol)
- **Client MCP** - Connessione a tool server
- **Server MCP** - Esporre tools via protocollo
- **Tool Discovery** - Registrazione dinamica tools

## Configurazione

### Variabili d'Ambiente

| Variabile | Descrizione | Obbligatoria |
|-----------|-------------|--------------|
| `OPENAI_API_KEY` | La tua API key OpenAI | Sì |
| `OPENAI_MODEL` | Modello da usare (default: gpt-4o-mini) | No |

### User Secrets (Alternativa)

```bash
cd core/01.HelloAgent
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "la-tua-api-key"
dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"
```

## Pacchetti NuGet Utilizzati

| Pacchetto | Scopo |
|-----------|-------|
| `Microsoft.Agents.AI` | Framework core |
| `Microsoft.Agents.AI.OpenAI` | Integrazione OpenAI |
| `Microsoft.Agents.AI.Workflows` | Orchestrazione workflow |
| `Microsoft.Agents.AI.A2A` | Comunicazione agent-to-agent |
| `Microsoft.SemanticKernel.Connectors.Qdrant` | Vector store Qdrant |
| `Microsoft.SemanticKernel.Connectors.PgVector` | Vector store PostgreSQL |
| `ModelContextProtocol` | Client/server MCP |

## Servizi Docker (per progetti Vector Store)

```bash
cd core/11.RAG.VectorStores
docker compose up -d
```

| Servizio | Porta | Scopo |
|----------|-------|-------|
| Qdrant | 6333, 6334 | Database vettoriale |
| PostgreSQL | 5433 | PostgreSQL + pgvector |
| SQL Server | 1434 | SQL Server (richiede 2025) |

## Contribuire

I contributi sono benvenuti! Sentiti libero di aprire issue e pull request.

## Licenza

Questo progetto è rilasciato sotto licenza MIT - vedi il file [LICENSE](LICENSE) per i dettagli.

## Ringraziamenti

- [Microsoft Agent Framework](https://github.com/microsoft/agents)
- [OpenAI](https://openai.com/)
- [Semantic Kernel](https://github.com/microsoft/semantic-kernel)

---

**Buon Apprendimento!** 🚀

Inizia con [01. Hello Agent](core/01.HelloAgent/) e procedi verso l'alto!
