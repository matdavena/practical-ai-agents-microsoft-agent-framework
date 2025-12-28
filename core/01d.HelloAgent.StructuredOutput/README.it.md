# 01d. Hello Agent - Structured Output

> Ottenere risposte tipizzate e strutturate dagli LLM

## Panoramica

Questo progetto dimostra lo **Structured Output**, una funzionalità che garantisce che l'LLM restituisca dati in un formato specifico definito da una classe C#. Invece di testo libero che richiede parsing, ottieni oggetti tipizzati utilizzabili direttamente nel codice.

## Perché Structured Output?

Senza structured output:
```csharp
// Risposta in testo libero
string response = "John Smith ha 35 anni e lavora come ingegnere software a Seattle...";

// Servono regex, parsing di stringhe, o un'altra chiamata LLM per estrarre i dati
// Soggetto a errori e inaffidabile!
```

Con structured output:
```csharp
// Risposta tipizzata
PersonInfo person = response.Result;

// Accesso diretto alle proprietà - type-safe!
Console.WriteLine(person.Name);        // "John Smith"
Console.WriteLine(person.Age);         // 35
Console.WriteLine(person.Occupation);  // "Software Engineer"
```

## Benefici Chiave

| Beneficio | Descrizione |
|-----------|-------------|
| **Type Safety** | Controllo a compile-time della struttura della risposta |
| **Niente Parsing** | Accesso diretto alle proprietà, niente regex |
| **Schema Garantito** | L'LLM ritorna sempre JSON valido che corrisponde alla tua classe |
| **Oggetti Annidati** | Supporto per gerarchie complesse e array |
| **Supporto Enum** | Gli enum vengono convertiti in valori stringa |

## Come Funziona

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         FLUSSO STRUCTURED OUTPUT                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐                    │
│  │  Classe C#  │────▶│ JSON Schema │────▶│  Richiesta  │                    │
│  │ (PersonInfo)│     │ (generato)  │     │   LLM API   │                    │
│  └─────────────┘     └─────────────┘     └──────┬──────┘                    │
│                                                  │                           │
│                                                  ▼                           │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐                    │
│  │   Oggetto   │◀────│    JSON     │◀────│  Risposta   │                    │
│  │   Tipizzato │     │   Response  │     │   LLM API   │                    │
│  └─────────────┘     └─────────────┘     └─────────────┘                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Metodi di Utilizzo

### Metodo 1: RunAsync<T>() Generico (Consigliato)

```csharp
// Approccio più semplice - specifica il tipo alla chiamata
AgentRunResponse<PersonInfo> response = await agent.RunAsync<PersonInfo>(
    "Estrai info su John Smith, 35 anni, ingegnere software."
);

// Accedi al risultato tipizzato
PersonInfo person = response.Result;
Console.WriteLine(person.Name);  // Accesso diretto alla proprietà
```

### Metodo 2: ResponseFormat alla Creazione dell'Agente

```csharp
// Specifica il formato quando crei l'agente
ChatClientAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "PersonExtractor",
    ChatOptions = new ChatOptions
    {
        Instructions = "Estrai informazioni sulla persona.",
        ResponseFormat = ChatResponseFormat.ForJsonSchema<PersonInfo>()
    }
});

// Tutte le risposte da questo agente saranno PersonInfo
var response = await agent.RunAsync("...");
PersonInfo person = response.Deserialize<PersonInfo>();
```

### Metodo 3: Manuale con ChatClientAgentRunOptions

```csharp
// Massimo controllo - specifica opzioni per ogni richiesta
AgentRunResponse response = await agent.RunAsync(query, options: new ChatClientAgentRunOptions
{
    ChatOptions = new ChatOptions
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema<PersonInfo>(jsonOptions)
    }
});

PersonInfo person = response.Deserialize<PersonInfo>(jsonOptions);
```

## Linee Guida per le Classi Modello

### Proprietà Base

```csharp
using System.ComponentModel;
using System.Text.Json.Serialization;

[Description("Informazioni su una persona")]  // Aiuta l'LLM a capire la classe
public class PersonInfo
{
    [JsonPropertyName("name")]                 // Nome proprietà JSON
    [Description("Il nome completo")]          // Descrizione per l'LLM
    public string? Name { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }              // Nullable per campi opzionali
}
```

### Oggetti Annidati

```csharp
public class Recipe
{
    public required string Name { get; set; }
    public required Ingredient[] Ingredients { get; set; }  // Array di oggetti
    public required CookingStep[] Steps { get; set; }       // Oggetti annidati
}

public class Ingredient
{
    public required string Name { get; set; }
    public required string Quantity { get; set; }
}
```

### Enum

```csharp
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Expert
}

public class Recipe
{
    public DifficultyLevel Difficulty { get; set; }  // Serializzato come stringa
}
```

## Limitazioni

| Limitazione | Dettagli |
|-------------|----------|
| **No DateTime** | Usa string per le date (es. "2024-01-15") |
| **No Uri** | Usa string per gli URL |
| **Max Profondità** | 5 livelli di annidamento |
| **Max Proprietà** | 100 totali tra tutti gli oggetti |
| **No minLength/maxLength** | Vincoli di tipo non applicati |

## Sezioni Demo

1. **Person Extraction** - Estrai info persona da testo non strutturato
2. **Recipe Generator** - Genera ricette strutturate con oggetti annidati
3. **Sentiment Analysis** - Analizza sentiment del testo con entità
4. **Compare Outputs** - Confronto fianco a fianco strutturato vs non strutturato
5. **Interactive** - Prova query custom con tipi diversi

## Prerequisiti

- **API Key OpenAI** da [platform.openai.com](https://platform.openai.com)
- Structured Output richiede GPT-4o o modelli più recenti

## Configurazione

```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:OPENAI_MODEL = "gpt-4o-mini"  # Deve supportare structured output
```

## Esecuzione del Progetto

```bash
cd core/01d.HelloAgent.StructuredOutput
dotnet run
```

## Struttura del Progetto

```
01d.HelloAgent.StructuredOutput/
├── Models/
│   ├── PersonInfo.cs         # Modello estrazione semplice
│   ├── Recipe.cs             # Modello complesso annidato
│   └── SentimentAnalysis.cs  # Modello analisi NLP
├── Program.cs                # Implementazioni demo
└── README.md                 # Questo file
```

## Casi d'Uso

| Caso d'Uso | Modello | Descrizione |
|------------|---------|-------------|
| **Estrazione Dati** | PersonInfo | Estrai entità da testo non strutturato |
| **Generazione Contenuti** | Recipe | Genera contenuti strutturati |
| **Analisi Testo** | SentimentAnalysis | Task NLP con risultati strutturati |
| **Compilazione Form** | Custom | Auto-compila form da linguaggio naturale |
| **Risposte API** | Custom | Genera risposte API strutturate |

## Punti Chiave

1. **Type Safety** - Controllo a compile-time previene errori runtime
2. **RunAsync<T>()** - Modo più semplice per ottenere risposte tipizzate
3. **[Description]** - Aiuta l'LLM a capire il tuo schema
4. **JsonPropertyName** - Controlla la serializzazione JSON
5. **Oggetti Annidati** - Supporto per strutture dati complesse
6. **Enum come Stringhe** - Usa JsonStringEnumConverter per leggibilità

## Prossimi Passi

Continua con [02. DevAssistant - Tools](../02.DevAssistant.Tools/) per imparare il function calling.

## Risorse Correlate

- [Guida OpenAI Structured Outputs](https://platform.openai.com/docs/guides/structured-outputs)
- [Using JSON Schema for Structured Output in .NET](https://devblogs.microsoft.com/semantic-kernel/using-json-schema-for-structured-output-in-net-for-openai-models/)
- [Microsoft Agent Framework](https://github.com/microsoft/agents)
