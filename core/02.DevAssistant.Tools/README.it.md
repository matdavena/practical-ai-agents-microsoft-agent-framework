# 02. DevAssistant - Tools

> Dare al tuo agente AI la capacità di agire nel mondo reale

## Panoramica

In questo progetto, estendiamo il nostro agente AI con i **Tools** - funzioni che l'agente può chiamare per eseguire azioni e recuperare informazioni dal mondo reale. Questa è la base del pattern "Function Calling" che rende gli agenti AI veramente utili.

## Cosa Imparerai

- ✅ Come definire Tools usando metodi C# con l'attributo `[Description]`
- ✅ Come usare `AIFunctionFactory.Create()` per registrare i tools
- ✅ Il pattern Function Calling: l'LLM decide quando chiamare i tools
- ✅ Differenza tra tools statici e tools con istanza
- ✅ Best practices per la sicurezza dei tools (sandboxing)

## Concetti Chiave

| Concetto | Descrizione |
|----------|-------------|
| `Tool/Function` | Una funzione che l'agente può invocare |
| `AIFunctionFactory` | Factory per creare `AITool` da metodi .NET |
| `[Description]` | Attributo che descrive il tool all'LLM |
| `Function Calling` | Pattern dove l'LLM decide di usare un tool |

## Flusso del Function Calling

```
┌──────────────────────────────────────────────────────────────────────┐
│  1. Utente: "Che ore sono?"                                          │
│                    ↓                                                 │
│  2. L'LLM analizza i tools disponibili                               │
│                    ↓                                                 │
│  3. L'LLM decide: "Devo chiamare get_current_datetime"              │
│                    ↓                                                 │
│  4. Il framework esegue il metodo .NET                               │
│                    ↓                                                 │
│  5. Risultato: "Domenica, 22 Dicembre 2024 - 14:30:00 (LOCAL)"      │
│                    ↓                                                 │
│  6. LLM: "Sono le 14:30 di domenica 22 dicembre"                    │
└──────────────────────────────────────────────────────────────────────┘
```

## Struttura del Progetto

```
02.DevAssistant.Tools/
├── Program.cs                    # Programma principale con setup agente
├── Tools/
│   ├── DateTimeTools.cs         # Tools data/ora (statici)
│   ├── CalculatorTools.cs       # Tools matematici (statici)
│   └── FileSystemTools.cs       # Operazioni su file (istanza)
└── README.md
```

## Tools Inclusi

### DateTime Tools (Statici)
| Tool | Descrizione |
|------|-------------|
| `get_current_datetime` | Ottiene data e ora corrente |
| `get_timezone` | Ottiene info sul fuso orario |
| `calculate_date_difference` | Calcola il tempo tra due date |
| `get_day_of_week` | Ottiene il nome del giorno per una data |

### Calculator Tools (Statici)
| Tool | Descrizione |
|------|-------------|
| `calculate` | Operazioni matematiche base (+, -, *, /, ^, %) |
| `calculate_percentage` | Calcoli percentuali |
| `convert_units` | Conversioni unità (km/mi, kg/lb, °C/°F) |
| `calculate_statistics` | Media, mediana, min, max, somma |

### FileSystem Tools (Istanza)
| Tool | Descrizione |
|------|-------------|
| `get_working_directory` | Ottiene la directory sandbox |
| `list_files` | Elenca contenuto directory |
| `read_file` | Legge contenuto file |
| `write_file` | Crea/modifica file |
| `create_directory` | Crea directory |
| `delete_file` | Elimina file |

## Esempi di Codice

### Definire un Tool Statico

```csharp
public static class DateTimeTools
{
    [Description("Ottiene data e ora corrente. Usa quando l'utente chiede che ore sono.")]
    public static string GetCurrentDateTime(
        [Description("'local' o 'utc'")]
        string timeType = "local")
    {
        var dt = timeType == "utc" ? DateTime.UtcNow : DateTime.Now;
        return $"{dt:dddd, dd MMMM yyyy - HH:mm:ss}";
    }
}
```

### Definire un Tool con Istanza

```csharp
public class FileSystemTools
{
    public string WorkingDirectory { get; }

    public FileSystemTools(string? workingDir = null)
    {
        WorkingDirectory = workingDir ?? "./workspace";
    }

    [Description("Legge il contenuto di un file")]
    public string ReadFile(
        [Description("Path relativo del file")]
        string path)
    {
        var fullPath = ValidatePath(path);
        return File.ReadAllText(fullPath);
    }
}
```

### Registrare i Tools

```csharp
var fileTools = new FileSystemTools();

var tools = new List<AITool>
{
    // Tools statici
    AIFunctionFactory.Create(DateTimeTools.GetCurrentDateTime, "get_current_datetime"),
    AIFunctionFactory.Create(CalculatorTools.Calculate, "calculate"),

    // Tools con istanza
    AIFunctionFactory.Create(fileTools.ReadFile, "read_file"),
    AIFunctionFactory.Create(fileTools.WriteFile, "write_file"),
};

ChatClientAgent agent = openAiClient
    .GetChatClient(model)
    .CreateAIAgent(
        instructions: "Sei un assistente con accesso a vari tools...",
        tools: tools
    );
```

## Best Practices di Sicurezza

1. **Sandbox per operazioni su file** - Limita a una directory specifica
2. **Valida tutti i path** - Previeni attacchi path traversal (`../../../etc/passwd`)
3. **Gestisci gli errori** - Restituisci messaggi di errore utili
4. **Documenta il comportamento** - Attributi `[Description]` chiari

## Esecuzione del Progetto

```bash
cd core/02.DevAssistant.Tools
dotnet run
```

## Esempi di Interazione

```
Tu: Che ore sono?
Agente: [chiama get_current_datetime] Sono le 14:30 di domenica 22 dicembre 2024.

Tu: Quanto fa il 15% di 250?
Agente: [chiama calculate_percentage] Il 15% di 250 è 37,5.

Tu: Crea un file chiamato notes.txt con "Hello World"
Agente: [chiama write_file] Il file 'notes.txt' è stato creato con successo.

Tu: Converti 100 chilometri in miglia
Agente: [chiama convert_units] 100 km equivalgono a circa 62,14 miglia.
```

## Punti Chiave

1. **[Description] è cruciale** - L'LLM lo usa per decidere quando chiamare il tool
2. **Restituisci stringhe** - Facili da interpretare per l'LLM nelle risposte
3. **Gestisci gli errori** - Restituisci messaggi che l'LLM può spiegare all'utente
4. **Sicurezza prima di tutto** - Valida sempre gli input, specialmente per operazioni su file

## Prossimi Passi

Continua con [03. DevAssistant - Memory](../03.DevAssistant.Memory/) per imparare:
- Memoria a breve termine (contesto conversazione)
- Memoria a lungo termine (persiste tra sessioni)
- Come gli agenti possono ricordare preferenze utente

## Risorse Correlate

- [Microsoft Agent Framework - Tools](https://github.com/microsoft/agents)
- [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling)
