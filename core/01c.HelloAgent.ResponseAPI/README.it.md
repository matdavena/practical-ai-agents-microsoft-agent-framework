# 01c. Hello Agent - OpenAI Response API

> Comprendere l'API evoluta di OpenAI per applicazioni agentiche

## Panoramica

Questo progetto dimostra la **Response API di OpenAI**, un'evoluzione della Chat Completions API lanciata a marzo 2025. Mentre Chat Completions rimane supportata, Response API è l'approccio raccomandato da OpenAI per nuove applicazioni agentiche.

## Perché Response API?

OpenAI ha introdotto Response API per superare i limiti di Chat Completions nei casi d'uso agentici:

- **Conversazioni Stateful**: Non serve inviare l'intera cronologia
- **Tool Built-in**: Web search, code interpreter, file search senza implementazione custom
- **Migliori Prestazioni**: 40-80% di miglior utilizzo della cache
- **Accesso al Ragionamento**: Accedi ai summary del ragionamento dai reasoning models
- **Supporto MCP**: Integrazione nativa con server Model Context Protocol

## Response API vs Chat Completions

| Caratteristica | Chat Completions | Response API |
|----------------|------------------|--------------|
| **Pattern API** | `GetChatClient()` | `GetResponsesClient()` |
| **Stato** | Stateless | Stateful (lato server) |
| **Conversazione** | Invia tutta la cronologia | Usa `previous_response_id` |
| **Web Search** | Implementazione custom | Built-in `HostedWebSearchTool` |
| **Code Interpreter** | Non disponibile | Built-in `HostedCodeInterpreterTool` |
| **File Search** | RAG custom necessario | Built-in `HostedFileSearchTool` |
| **Server MCP** | Non supportato | Supporto nativo |
| **Cache** | Standard | 40-80% migliore |
| **Ragionamento** | Nascosto | Summary accessibili |
| **Futuro** | Mantenuto | Sviluppo principale |

## Concetti Chiave

| Concetto | Descrizione |
|----------|-------------|
| `ResponsesClient` | Client per OpenAI Response API |
| `GetResponsesClient()` | Metodo di estensione per ottenere il client Response API |
| `ResponseId` | ID univoco per ogni risposta (per riprendere conversazioni) |
| `ConversationId` | ID della risposta precedente per continuare la conversazione |
| `HostedWebSearchTool` | Tool built-in per ricerca web |
| `HostedCodeInterpreterTool` | Tool built-in per esecuzione codice Python |
| `HostedFileSearchTool` | Tool built-in per ricerca file/vettoriale |

## Architettura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      ARCHITETTURA RESPONSE API                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                        OpenAI Server-Side                             │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │   │
│  │  │ Web Search  │  │    Code     │  │    File     │  │     MCP     │  │   │
│  │  │   (Bing)    │  │ Interpreter │  │   Search    │  │   Servers   │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  │   │
│  │                                                                       │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐ │   │
│  │  │              Storage Stato Conversazione                         │ │   │
│  │  │         (Accessibile via previous_response_id)                   │ │   │
│  │  └─────────────────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                            Response API                                      │
│                                    │                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                      La Tua Applicazione                              │   │
│  │                                                                       │   │
│  │  ┌─────────────┐      ┌─────────────┐      ┌─────────────┐           │   │
│  │  │ OpenAIClient│─────▶│Responses    │─────▶│ChatClient   │           │   │
│  │  │             │      │Client       │      │Agent        │           │   │
│  │  └─────────────┘      └─────────────┘      └─────────────┘           │   │
│  │                                                                       │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Prerequisiti

- **API Key OpenAI** da [platform.openai.com](https://platform.openai.com)
- Nota: Alcune funzionalità (Web Search, Code Interpreter) sono **solo OpenAI** e non disponibili su Azure OpenAI

## Configurazione

```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:OPENAI_MODEL = "gpt-4o-mini"  # opzionale, default: gpt-4o-mini
```

## Esecuzione del Progetto

```bash
cd core/01c.HelloAgent.ResponseAPI
dotnet run
```

## Guida al Codice

### Utilizzo Base Response API

```csharp
using OpenAI;
using OpenAI.Responses;
using Microsoft.Agents.AI;

OpenAIClient client = new(apiKey);

// DIFFERENZA CHIAVE: GetResponsesClient() invece di GetChatClient()
ResponsesClient responsesClient = client.GetResponsesClient("gpt-4o-mini");

// Stesso metodo di estensione CreateAIAgent()
AIAgent agent = responsesClient.CreateAIAgent(
    instructions: "Sei un assistente utile."
);

// Stesso metodo RunAsync()
AgentRunResponse response = await agent.RunAsync("Ciao!");
```

### Conversazione Stateful (Riprendi con Response ID)

```csharp
// Primo messaggio
AgentRunResponse response1 = await agent.RunAsync("Mi chiamo Marco.", thread);

// Salva response ID (in produzione salvalo nel database)
string responseId = response1.ResponseId;

// Dopo: Riprendi la conversazione con solo l'ID (niente cronologia!)
var options = new ChatClientAgentRunOptions
{
    ChatOptions = new ChatOptions
    {
        ConversationId = responseId  // Passa solo l'ID precedente!
    }
};

AgentRunResponse response2 = await agent.RunAsync("Come mi chiamo?", options: options);
// L'agente ricorda: "Ti chiami Marco"
```

### Web Search Tool

```csharp
AIAgent agent = responsesClient.CreateAIAgent(
    instructions: "Cerca nel web informazioni correnti.",
    tools: [new HostedWebSearchTool()]  // Built-in!
);

await agent.RunAsync("Quali sono le notizie tech di oggi?");
// L'agente cerca automaticamente nel web e sintetizza i risultati
```

### Code Interpreter Tool

```csharp
AIAgent agent = responsesClient.CreateAIAgent(
    instructions: "Sei un analista dati.",
    tools: [new HostedCodeInterpreterTool()]  // Built-in!
);

await agent.RunAsync("Calcola i primi 20 numeri di Fibonacci e mostrali in tabella.");
// L'agente scrive ed esegue codice Python, ritorna risultati formattati
```

### File Search Tool

```csharp
// Carica file su OpenAI
var fileClient = client.GetOpenAIFileClient();
var uploadResult = await fileClient.UploadFileAsync(data, "documento.pdf", FileUploadPurpose.UserData);

// Crea vector store
var vectorClient = client.GetVectorStoreClient();
var vectorStore = await vectorClient.CreateVectorStoreAsync(new VectorStoreCreationOptions { Name = "MieiDoc" });
await vectorClient.AddFileToVectorStoreAsync(vectorStore.Value.Id, uploadResult.Value.Id);

// Crea agente con file search
AIAgent agent = responsesClient.CreateAIAgent(
    instructions: "Rispondi alle domande dai documenti caricati.",
    tools: [new HostedFileSearchTool
    {
        Inputs = [new HostedVectorStoreContent(vectorStore.Value.Id)]
    }]
);
```

## Sezioni Demo

1. **Response API Base** - Agente semplice usando Response API
2. **Conversazione Stateful** - Riprendi conversazioni con response ID
3. **Web Search Tool** - Capacità di ricerca web built-in
4. **Code Interpreter** - Esegui Python in ambiente sandboxed
5. **Confronta API** - Confronto fianco a fianco con Chat Completions

## Quando Usare Ogni API

### Usa Response API Quando:
- Costruisci applicazioni agentiche
- Hai bisogno di tool built-in (web search, code interpreter, file search)
- Vuoi conversazioni stateful senza gestire la cronologia
- Lavori con reasoning models e hai bisogno dei reasoning summaries
- Pianifichi di usare server MCP

### Usa Chat Completions Quando:
- Generazione di testo semplice e stateless
- Hai bisogno di massima compatibilità (Azure OpenAI, altri provider)
- Non ti servono tool built-in
- Hai già un'integrazione Chat Completions esistente

## Supporto Azure OpenAI

> **Importante**: Dal 2025, alcune funzionalità Response API sono solo OpenAI:
> - `HostedWebSearchTool` - Solo OpenAI
> - `HostedCodeInterpreterTool` - Solo OpenAI (problemi download file su Azure)
> - `HostedFileSearchTool` - Supporto limitato su Azure
>
> Response API base (conversazioni stateful) funziona su Azure OpenAI.

## Punti Chiave

1. **Stesso Codice Agente** - `ChatClientAgent` funziona con entrambe le API
2. **Stateful di Default** - Response API memorizza lo stato conversazione lato server
3. **Tool Built-in** - Non serve implementare web search o esecuzione codice
4. **A Prova di Futuro** - API principale OpenAI per nuovi sviluppi
5. **Prestazioni Migliori** - Caching migliorato riduce costi e latenza

## Prossimi Passi

Continua con [02. DevAssistant - Tools](../02.DevAssistant.Tools/) per imparare il function calling custom.

## Risorse Correlate

- [Documentazione OpenAI Response API](https://platform.openai.com/docs/api-reference/responses)
- [Why We Built the Responses API](https://developers.openai.com/blog/responses-api/)
- [Responses vs Chat Completions](https://platform.openai.com/docs/guides/responses-vs-chat-completions)
- [Migrate to Responses API](https://platform.openai.com/docs/guides/migrate-to-responses)
- [Microsoft Agent Framework](https://github.com/microsoft/agents)
