# 01. Hello Agent

> Il tuo primo agente AI con Microsoft Agent Framework

## Panoramica

Questo è il punto di partenza del tuo viaggio con Microsoft Agent Framework. In questo progetto, creerai il tuo primo agente AI e comprenderai i concetti fondamentali che alimentano tutte le applicazioni basate su agenti.

## Cosa Imparerai

- ✅ Come creare un `OpenAIClient` per comunicare con le API OpenAI
- ✅ Come creare un `ChatClientAgent` (l'agente vero e proprio)
- ✅ Come eseguire una richiesta singola con `RunAsync()`
- ✅ Come gestire lo streaming delle risposte con `RunStreamingAsync()`
- ✅ Come mantenere il contesto della conversazione con `AgentThread`

## Concetti Chiave

| Concetto | Descrizione |
|----------|-------------|
| `OpenAIClient` | Il client HTTP che comunica con le API OpenAI |
| `ChatClient` | Il client specifico per le chat dall'SDK OpenAI |
| `ChatClientAgent` | L'agente del framework che estende il ChatClient con capacità agentiche |
| `AgentThread` | Mantiene lo stato della conversazione (memoria a breve termine) |
| `AgentRunResponse` | L'oggetto di risposta dell'agente |

## Architettura

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  OpenAIClient   │────▶│   ChatClient    │────▶│ChatClientAgent  │
│  (Client HTTP)  │     │   (API Chat)    │     │  (Agente AI)    │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Prerequisiti

1. **.NET 10 SDK** installato
2. **API Key OpenAI** configurata (vedi [Configurazione](#configurazione))

## Configurazione

Imposta la tua API key OpenAI usando uno di questi metodi:

### Opzione 1: Variabile d'Ambiente (Consigliato)
```bash
# Windows (PowerShell)
$env:OPENAI_API_KEY = "la-tua-api-key"

# Windows (CMD)
set OPENAI_API_KEY=la-tua-api-key

# Linux/macOS
export OPENAI_API_KEY="la-tua-api-key"
```

### Opzione 2: User Secrets
```bash
dotnet user-secrets set "OpenAI:ApiKey" "la-tua-api-key"
```

## Esecuzione del Progetto

```bash
cd core/01.HelloAgent
dotnet run
```

## Guida al Codice

### Step 1: Creare il Client OpenAI

```csharp
OpenAIClient openAiClient = new(apiKey);
```

L'`OpenAIClient` è il punto di ingresso per tutte le comunicazioni con le API OpenAI.

### Step 2: Creare l'Agente

```csharp
ChatClientAgent agent = openAiClient
    .GetChatClient(model)
    .CreateAIAgent(
        instructions: "Sei un assistente AI amichevole...",
        name: "HelloAgent"
    );
```

Il metodo di estensione `CreateAIAgent()` trasforma un semplice `ChatClient` in un `ChatClientAgent` con capacità avanzate.

### Step 3: Richiesta Singola (RunAsync)

```csharp
AgentRunResponse response = await agent.RunAsync("La tua domanda qui");
Console.WriteLine(response.ToString());
```

`RunAsync()` invia un messaggio e attende la risposta completa.

### Step 4: Risposta in Streaming (RunStreamingAsync)

```csharp
await foreach (var update in agent.RunStreamingAsync("La tua domanda"))
{
    Console.Write(update.ToString());
}
```

`RunStreamingAsync()` restituisce la risposta token per token per una migliore UX.

### Step 5: Conversazione con Thread

```csharp
AgentThread thread = agent.GetNewThread();

await foreach (var update in agent.RunStreamingAsync(userInput, thread))
{
    Console.Write(update.ToString());
}
```

`AgentThread` mantiene la storia della conversazione, abilitando conversazioni multi-turno.

## Sezioni Demo

Il progetto include tre demo interattive:

1. **Demo 1: Richiesta Singola** - Domanda/risposta base con `RunAsync()`
2. **Demo 2: Risposta in Streaming** - Visualizzazione della risposta in tempo reale con `RunStreamingAsync()`
3. **Demo 3: Conversazione con Thread** - Chat interattiva con memoria

## Punti Chiave

1. **System Prompt (instructions)** - Definisce il comportamento e la personalità dell'agente
2. **RunAsync vs RunStreamingAsync** - Scegli in base ai requisiti UX
3. **AgentThread** - Essenziale per mantenere il contesto della conversazione
4. **Uso dei Token** - Thread più lunghi consumano più token

## Prossimi Passi

Continua con [02. Chat With History](../02.ChatWithHistory/) per imparare:
- Aggiungere tool al tuo agente
- Abilitare l'agente a eseguire azioni nel mondo reale
- Comprendere il pattern Tool Calling

## Risorse Correlate

- [Documentazione Microsoft Agent Framework](https://github.com/microsoft/agents)
- [Documentazione API OpenAI](https://platform.openai.com/docs)
