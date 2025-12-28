# 01b. Hello Agent - Provider Multipli

> Utilizzo di Microsoft Agent Framework con diversi backend LLM

## Panoramica

Questo progetto dimostra che Microsoft Agent Framework è **agnostico rispetto al provider**. Lo stesso codice dell'agente funziona senza modifiche con diversi provider LLM: OpenAI, Azure OpenAI, Anthropic Claude, Google Gemini e Ollama (modelli locali).

Capire la flessibilità dei provider è essenziale perché:
- **Sviluppo**: Usa modelli locali gratuiti (Ollama) o opzioni cloud economiche (Gemini Flash)
- **Produzione**: Passa a provider cloud (OpenAI, Azure) per affidabilità
- **Enterprise**: Usa Azure OpenAI per compliance e residenza dati
- **Ragionamento**: Usa Anthropic Claude per task analitici complessi
- **Multimodale**: Usa Google Gemini per testo, immagini, video e audio
- **Privacy**: Usa Ollama quando i dati non possono uscire dall'infrastruttura

## Cosa Imparerai

- Come usare **OpenAI** (API diretta) - cloud, pagamento per token
- Come usare **Azure OpenAI** - enterprise, gestito da Azure
- Come usare **Anthropic Claude** - eccellente ragionamento e analisi
- Come usare **Google Gemini** - capacità multimodali
- Come usare **Ollama** - modelli locali, gratuito, privato
- Come lo stesso `ChatClientAgent` funziona con qualsiasi backend

## Concetti Chiave

| Concetto | Descrizione |
|----------|-------------|
| `OpenAIClient` | Client per le API dirette di OpenAI |
| `AzureOpenAIClient` | Client per OpenAI ospitato su Azure |
| `AnthropicClient` | Client per modelli Anthropic Claude |
| `GeminiChatClient` | Client per modelli Google Gemini |
| `OllamaApiClient` | Client per modelli Ollama locali |
| `IChatClient` | Interfaccia comune implementata da tutti i provider |

## Architettura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ASTRAZIONE DEL PROVIDER                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐           │
│  │ OpenAI  │  │ Azure   │  │Anthropic│  │ Google  │  │ Ollama  │           │
│  │ (Cloud) │  │ OpenAI  │  │ Claude  │  │ Gemini  │  │ (Local) │           │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘           │
│       │            │            │            │            │                 │
│       └────────────┴─────┬──────┴────────────┴────────────┘                 │
│                          │                                                   │
│                  ┌───────▼───────┐                                          │
│                  │  IChatClient  │  (Interfaccia Comune)                    │
│                  └───────┬───────┘                                          │
│                          │                                                   │
│                  ┌───────▼───────┐                                          │
│                  │ChatClientAgent│  (Stesso Codice Agente)                  │
│                  └───────────────┘                                          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Confronto Provider

| Caratteristica | OpenAI | Azure OpenAI | Anthropic | Google Gemini | Ollama |
|----------------|--------|--------------|-----------|---------------|--------|
| **Posizione** | Cloud | Cloud | Cloud | Cloud | Locale |
| **Costo** | Per token | Per token | Per token | Per token | Gratuito |
| **Ideale Per** | Qualità | Enterprise | Ragionamento | Multimodale | Privacy |
| **Top Modelli** | GPT-4o, o1 | GPT-4o | Claude 3.5 | Gemini 2.0 | Llama, Gemma |
| **Contesto** | 128K | 128K | 200K | 1M+ | Variabile |
| **Offline** | No | No | No | No | Si |

## Prerequisiti

### Per OpenAI
- API key da [platform.openai.com](https://platform.openai.com)

### Per Azure OpenAI
- Sottoscrizione Azure con risorsa OpenAI deployata

### Per Anthropic Claude
- API key da [console.anthropic.com](https://console.anthropic.com)

### Per Google Gemini
- API key da [aistudio.google.com](https://aistudio.google.com/apikey)

### Per Ollama
1. Installa da [ollama.ai](https://ollama.ai)
2. Scarica un modello: `ollama pull gemma3:1b`

## Configurazione

### OpenAI
```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:OPENAI_MODEL = "gpt-4o-mini"  # opzionale
```

### Azure OpenAI
```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://tua-risorsa.openai.azure.com/"
$env:AZURE_OPENAI_DEPLOYMENT = "nome-deployment"
$env:AZURE_OPENAI_API_KEY = "tua-api-key"  # oppure usa: az login
```

### Anthropic Claude
```powershell
$env:ANTHROPIC_API_KEY = "sk-ant-..."
$env:ANTHROPIC_MODEL = "claude-3-5-haiku-latest"  # opzionale
```

### Google Gemini
```powershell
$env:GOOGLE_API_KEY = "AI..."
$env:GOOGLE_MODEL = "gemini-2.0-flash"  # opzionale
```

### Ollama
```powershell
$env:OLLAMA_ENDPOINT = "http://localhost:11434"  # opzionale
$env:OLLAMA_MODEL = "gemma3:1b"  # opzionale
```

## Esecuzione del Progetto

```bash
cd core/01b.HelloAgent.Providers
dotnet run
```

## Guida al Codice

### OpenAI

```csharp
OpenAIClient client = new(apiKey);
ChatClientAgent agent = client
    .GetChatClient(model)
    .CreateAIAgent(instructions: "...", name: "OpenAI-Agent");
```

### Azure OpenAI

```csharp
AzureOpenAIClient client = new(new Uri(endpoint), new ApiKeyCredential(apiKey));
ChatClientAgent agent = client
    .GetChatClient(deploymentName)
    .CreateAIAgent(instructions: "...", name: "Azure-Agent");
```

### Anthropic Claude

```csharp
var anthropicClient = new AnthropicClient(new APIAuthentication(apiKey));
IChatClient chatClient = anthropicClient.Messages.AsBuilder().Build();
ChatClientAgent agent = new(chatClient, instructions: "...", name: "Claude-Agent");

// Claude richiede opzioni con ModelId
var options = new ChatClientAgentRunOptions(new ChatOptions { ModelId = model });
await agent.RunAsync("Ciao!", options: options);
```

### Google Gemini

```csharp
IChatClient geminiClient = new GeminiChatClient(apiKey: apiKey, model: model);
ChatClientAgent agent = new(geminiClient, instructions: "...", name: "Gemini-Agent");
```

### Ollama (Locale)

```csharp
IChatClient ollamaClient = new OllamaApiClient(new Uri(endpoint), modelName);
ChatClientAgent agent = new(ollamaClient, instructions: "...", name: "Ollama-Agent");
```

## Sezioni Demo

1. **Demo OpenAI** - Chat usando le API dirette di OpenAI
2. **Demo Azure OpenAI** - Chat usando modelli ospitati su Azure
3. **Demo Anthropic Claude** - Chat usando modelli Claude
4. **Demo Google Gemini** - Chat usando modelli Gemini
5. **Demo Ollama** - Chat usando modelli locali
6. **Confronta Tutti** - Esegui lo stesso prompt su tutti i provider disponibili

## Scelta del Provider

| Caso d'Uso | Provider Consigliato |
|------------|---------------------|
| Sviluppo/Testing | Ollama (gratuito) o Gemini Flash (economico) |
| Migliore Qualità | OpenAI GPT-4o o Anthropic Claude |
| Ragionamento Complesso | Anthropic Claude |
| Multimodale (immagini/video) | Google Gemini |
| Enterprise/Compliance | Azure OpenAI |
| Dati Sensibili | Ollama |
| Richiesto Offline | Ollama |

## Punti Chiave

1. **Astrazione del Provider** - `ChatClientAgent` funziona con qualsiasi `IChatClient`
2. **Stesso Codice Agente** - Cambia provider senza modificare la logica
3. **Solo Configurazione** - La selezione del provider è configurazione, non codice
4. **I Trade-off Contano** - Scegli in base a costo, privacy e capacità

## Prossimi Passi

Continua con [02. DevAssistant - Tools](../02.DevAssistant.Tools/) per imparare il function calling.

## Risorse Correlate

- [Microsoft Agent Framework](https://github.com/microsoft/agents)
- [OpenAI API](https://platform.openai.com/docs)
- [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/)
- [Anthropic Claude](https://docs.anthropic.com/)
- [Google Gemini](https://ai.google.dev/)
- [Ollama](https://ollama.ai)
