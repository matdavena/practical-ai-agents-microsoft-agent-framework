# 12. Web API - Chat Agents

> Integrazione di Microsoft Agent Framework con ASP.NET Core Web API, Dependency Injection e Keyed Services

## Panoramica

Questo progetto dimostra come costruire una **chat API pronta per la produzione** usando Microsoft Agent Framework con ASP.NET Core. Mostra:

- Pattern di **Dependency Injection** per la gestione degli agenti
- **Keyed Services** per registrare multipli agenti specializzati
- **Persistenza delle conversazioni** per chat riprensibili
- **REST API stile ChatGPT** per l'integrazione con frontend

## Concetti Chiave

### 1. Perché la Dependency Injection?

La Dependency Injection (DI) è fondamentale nelle applicazioni .NET moderne:

```
Senza DI:
┌─────────────────────────────────────────────────────┐
│ Controller                                          │
│   └── new OpenAIClient(apiKey)          // Accoppiato │
│       └── new ChatClientAgent(client)   // Accoppiato │
└─────────────────────────────────────────────────────┘

Con DI:
┌─────────────────────────────────────────────────────┐
│ Container DI                                        │
│   ├── OpenAIClient (Singleton)                      │
│   ├── ChatClientAgent "assistant" (Keyed Singleton) │
│   ├── ChatClientAgent "coder" (Keyed Singleton)     │
│   └── IConversationStore (Singleton)                │
└─────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────┐
│ Controller                                          │
│   ├── IServiceProvider ─────► Ottieni agente per key│
│   └── IConversationStore ───► Iniettato automaticamente│
└─────────────────────────────────────────────────────┘
```

**Benefici:**
- **Testabilità**: Facile mockare le dipendenze nei test unitari
- **Flessibilità**: Scambia implementazioni senza modificare il codice
- **Gestione del ciclo di vita**: Il container gestisce i cicli di vita degli oggetti
- **Accoppiamento lasco**: I componenti non conoscono le implementazioni concrete

### 2. Keyed Services (.NET 8+)

I Keyed Services permettono di registrare multiple implementazioni con chiavi uniche:

```csharp
// Registrazione
builder.Services.AddKeyedSingleton<ChatClientAgent>("assistant", (sp, key) =>
{
    var client = sp.GetRequiredService<OpenAIClient>();
    return new ChatClientAgentBuilder()
        .WithName("Assistant")
        .WithInstructions("Sei un assistente utile...")
        .WithChatClient(client.GetChatClient(model).AsIChatClient())
        .Build();
});

builder.Services.AddKeyedSingleton<ChatClientAgent>("coder", (sp, key) =>
{
    // Configurazione diversa per l'assistente del codice
});

// Risoluzione
var agent = serviceProvider.GetRequiredKeyedService<ChatClientAgent>("assistant");
```

### 3. Persistenza delle Conversazioni

I thread degli agenti possono essere serializzati e ripristinati per conversazioni riprensibili:

```csharp
// Salva lo stato della conversazione
JsonElement threadState = thread.Serialize();
await store.SaveThreadStateAsync(conversationId, threadState);

// Ripristina più tardi
var savedState = await store.GetThreadStateAsync(conversationId);
if (savedState.HasValue)
{
    AgentThread thread = agent.DeserializeThread(savedState.Value);
}
```

### 4. Architettura

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend React                           │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core Web API                       │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                     ChatController                        │  │
│  │  GET  /api/agents              - Lista agenti             │  │
│  │  POST /api/chat/{agentKey}     - Chatta con l'agente      │  │
│  │  GET  /api/conversations       - Lista conversazioni      │  │
│  │  GET  /api/conversations/{id}  - Ottieni conversazione    │  │
│  │  DELETE /api/conversations/{id} - Elimina conversazione   │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                │                                │
│                                ▼                                │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                   Container DI                            │  │
│  │                                                           │  │
│  │  ┌─────────────────┐  ┌─────────────────┐                 │  │
│  │  │ Keyed Services  │  │    Servizi      │                 │  │
│  │  │                 │  │                 │                 │  │
│  │  │ "assistant"  ───┼──┤ OpenAIClient    │                 │  │
│  │  │ "coder"      ───┼──┤ IConversationStore                │  │
│  │  │ "translator" ───┼──┤                 │                 │  │
│  │  └─────────────────┘  └─────────────────┘                 │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                         OpenAI API                              │
└─────────────────────────────────────────────────────────────────┘
```

## Esecuzione del Progetto

### Prerequisiti

- .NET 10 SDK
- Chiave API OpenAI

### Setup

```bash
# Imposta la tua chiave API
$env:OPENAI_API_KEY = "la-tua-chiave-api"

# Esegui l'API
cd core/12.WebApi.ChatAgents
dotnet run
```

### Accesso

- **Swagger UI**: http://localhost:5200
- **URL Base API**: http://localhost:5200/api

## Riferimento API

### Agenti

#### Lista Agenti Disponibili
```http
GET /api/agents
```

Risposta:
```json
[
  {
    "key": "assistant",
    "name": "General Assistant",
    "description": "Un assistente AI utile per compiti quotidiani."
  },
  {
    "key": "coder",
    "name": "Code Assistant",
    "description": "Un assistente specializzato per programmazione e codice."
  },
  {
    "key": "translator",
    "name": "Translator",
    "description": "Un traduttore multilingue per tradurre testi tra lingue."
  }
]
```

### Chat

#### Invia un Messaggio
```http
POST /api/chat/{agentKey}
Content-Type: application/json
X-User-Id: user123

{
  "message": "Ciao, come puoi aiutarmi?",
  "conversationId": null
}
```

Risposta:
```json
{
  "conversationId": "abc-123-def",
  "message": "Ciao! Sono il tuo assistente AI. Posso aiutarti con...",
  "role": "assistant",
  "timestamp": "2024-01-15T10:30:00Z",
  "messageId": "msg-456"
}
```

#### Continua una Conversazione
```http
POST /api/chat/assistant
Content-Type: application/json
X-User-Id: user123

{
  "message": "Dimmi di più su questo",
  "conversationId": "abc-123-def"
}
```

### Conversazioni

#### Lista Conversazioni dell'Utente
```http
GET /api/conversations
X-User-Id: user123
```

#### Ottieni Conversazione con Storico
```http
GET /api/conversations/{conversationId}
X-User-Id: user123
```

#### Elimina una Conversazione
```http
DELETE /api/conversations/{conversationId}
X-User-Id: user123
```

## Test con cURL

```bash
# Lista agenti
curl http://localhost:5200/api/agents

# Inizia una nuova conversazione
curl -X POST http://localhost:5200/api/chat/assistant \
  -H "Content-Type: application/json" \
  -H "X-User-Id: demo-user" \
  -d '{"message": "Cos'\''è la dependency injection?"}'

# Continua la conversazione (usa il conversationId di sopra)
curl -X POST http://localhost:5200/api/chat/assistant \
  -H "Content-Type: application/json" \
  -H "X-User-Id: demo-user" \
  -d '{"message": "Puoi farmi un esempio?", "conversationId": "IL-TUO-CONVERSATION-ID"}'

# Lista conversazioni
curl http://localhost:5200/api/conversations \
  -H "X-User-Id: demo-user"
```

## Punti Salienti del Codice

### Registrazione Keyed Services (Program.cs)

```csharp
// Registra multipli agenti con configurazioni diverse
builder.Services.AddKeyedSingleton<ChatClientAgent>("assistant", (sp, key) =>
{
    var client = sp.GetRequiredService<OpenAIClient>();
    return new ChatClientAgentBuilder()
        .WithName("Assistant")
        .WithInstructions("Sei un assistente utile...")
        .WithChatClient(client.GetChatClient(model).AsIChatClient())
        .Build();
});

builder.Services.AddKeyedSingleton<ChatClientAgent>("coder", (sp, key) =>
    // System prompt diverso per l'agente focalizzato sul codice
});
```

### Risoluzione Keyed Services (ChatController.cs)

```csharp
// Ottieni un agente specifico per chiave a runtime
var agent = _serviceProvider.GetRequiredKeyedService<ChatClientAgent>(agentKey);
```

### Persistenza del Thread (ChatController.cs)

```csharp
// Salva lo stato dopo ogni interazione
var threadState = thread.Serialize();
await _conversationStore.SaveThreadStateAsync(conversationId, threadState);

// Ripristina lo stato quando si riprende
var savedState = await _conversationStore.GetThreadStateAsync(conversationId);
if (savedState.HasValue)
{
    thread = agent.DeserializeThread(savedState.Value);
}
```

## Considerazioni per la Produzione

### 1. Autenticazione

Sostituisci l'header `X-User-Id` con autenticazione appropriata:

```csharp
// In ChatController
private string GetUserId()
{
    // Produzione: Ottieni dalle claims JWT
    return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedException();
}
```

### 2. Storage Persistente

Sostituisci `InMemoryConversationStore` con un'implementazione database:

```csharp
// SQL Server / PostgreSQL
builder.Services.AddSingleton<IConversationStore, SqlConversationStore>();

// Redis per scenari distribuiti
builder.Services.AddSingleton<IConversationStore, RedisConversationStore>();
```

### 3. Rate Limiting

Aggiungi rate limiting per proteggere la tua API:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("chat", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

### 4. Risposte in Streaming

Per un'esperienza di chat in tempo reale, implementa Server-Sent Events:

```csharp
[HttpPost("chat/{agentKey}/stream")]
public async Task StreamChat(string agentKey, ChatRequest request)
{
    Response.ContentType = "text/event-stream";

    await foreach (var chunk in agent.InvokeStreamingAsync(message, thread))
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n");
        await Response.Body.FlushAsync();
    }
}
```

## Punti Chiave

1. **DI è Essenziale**: Le app .NET moderne dovrebbero usare dependency injection per testabilità e flessibilità

2. **Keyed Services Abilitano Multi-Agente**: Registra multipli agenti con configurazioni diverse usando la stessa interfaccia

3. **La Serializzazione del Thread Abilita la Persistenza**: Salva e ripristina lo stato della conversazione per chat riprensibili

4. **Design API-First**: Endpoint RESTful rendono facile l'integrazione con qualsiasi frontend

5. **L'Astrazione Abilita la Flessibilità**: L'interfaccia `IConversationStore` permette di scambiare implementazioni di storage

## Prossimi Passi

- Aggiungere supporto streaming con Server-Sent Events
- Implementare un conversation store basato su database
- Aggiungere autenticazione JWT
- Costruire un frontend React
- Aggiungere tool agli agenti per funzionalità dinamiche

---

**Precedente**: [11. RAG con Vector Stores](../11.RAG.VectorStores/)
