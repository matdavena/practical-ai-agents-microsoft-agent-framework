/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                  03. DEV ASSISTANT - MEMORY                                  ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  OBIETTIVO: Comprendere i diversi tipi di memoria per un agente AI           ║
 * ║                                                                              ║
 * ║  TIPI DI MEMORIA:                                                            ║
 * ║                                                                              ║
 * ║  1. MEMORIA IMPLICITA (Thread)                                               ║
 * ║     - L'AgentThread mantiene automaticamente la cronologia                   ║
 * ║     - L'agente "ricorda" tutto ciò che è stato detto nella sessione          ║
 * ║     - Persa al riavvio dell'applicazione (a meno di persistenza)             ║
 * ║                                                                              ║
 * ║  2. MEMORIA A BREVE TERMINE (Thread Persistence)                             ║
 * ║     - Serializzazione del thread su disco                                    ║
 * ║     - Permette di riprendere conversazioni dopo il riavvio                   ║
 * ║     - Mantiene l'INTERA conversazione                                        ║
 * ║                                                                              ║
 * ║  3. MEMORIA A LUNGO TERMINE (AIContextProvider)                              ║
 * ║     - Informazioni estratte e persistite tra sessioni                        ║
 * ║     - Non l'intera chat, ma FATTI importanti (preferenze, stato, etc.)       ║
 * ║     - Iniettati nel contesto prima di ogni richiesta                         ║
 * ║                                                                              ║
 * ║  4. MEMORIA SEMANTICA (Vector Store) - Preview nel prossimo progetto         ║
 * ║     - Ricerca per similarità semantica                                       ║
 * ║     - Usa embeddings per trovare info rilevanti                              ║
 * ║     - Ideale per grandi quantità di informazioni                             ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.Text;
using Common;
using DevAssistant.Memory.Memory;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;  // Necessario per l'extension method CreateAIAgent con ChatClientAgentOptions

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURAZIONE INIZIALE
// ═══════════════════════════════════════════════════════════════════════════════

// Importante per visualizzare correttamente emoji e caratteri speciali
Console.OutputEncoding = Encoding.UTF8;

ConsoleHelper.WriteTitle("03. DevAssistant Memory");
ConsoleHelper.WriteSubtitle("Esplorazione della memoria dell'agente");

// Otteniamo la configurazione
var apiKey = ConfigurationHelper.GetOpenAiApiKey();
var model = ConfigurationHelper.GetOpenAiModel();

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * SETUP DELLA MEMORIA PREFERENZE
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * UserPreferencesMemory è un AIContextProvider custom che:
 * - Carica preferenze utente salvate da sessioni precedenti
 * - Estrae nuove preferenze dai messaggi dell'utente
 * - Inietta le preferenze nel contesto dell'agente
 */

ConsoleHelper.WriteSeparator("Step 1: Inizializzazione sistema di memoria");

// Creiamo la memoria per le preferenze utente
var preferencesMemory = new UserPreferencesMemory();

// Mostriamo le preferenze caricate (se esistono)
if (!string.IsNullOrEmpty(preferencesMemory.Preferences.Nome))
{
    ConsoleHelper.WriteInfo($"Preferenze caricate - Bentornato, {preferencesMemory.Preferences.Nome}!");

    Console.WriteLine();
    Console.WriteLine("Preferenze memorizzate:");
    Console.WriteLine($"  Nome: {preferencesMemory.Preferences.Nome}");
    Console.WriteLine($"  Stile: {preferencesMemory.Preferences.StileComunicazione ?? "non specificato"}");
    Console.WriteLine($"  Progetto: {preferencesMemory.Preferences.ProgettoCorrente ?? "nessuno"}");
    if (preferencesMemory.Preferences.ArgomentiInteresse.Count > 0)
    {
        Console.WriteLine($"  Interessi: {string.Join(", ", preferencesMemory.Preferences.ArgomentiInteresse)}");
    }
    Console.WriteLine($"  Ultimo accesso: {preferencesMemory.Preferences.UltimoAccesso:g}");
    Console.WriteLine();
}
else
{
    ConsoleHelper.WriteInfo("Nessuna preferenza trovata - Prima esecuzione");
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * CREAZIONE DELL'AGENTE CON MEMORIA
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * PUNTO CHIAVE: Come collegare AIContextProvider all'agente
 *
 * L'AIContextProvider viene passato tramite ChatClientAgentOptions.
 * Usiamo AIContextProviderFactory per creare il provider per ogni thread.
 */

ConsoleHelper.WriteSeparator("Step 2: Creazione agente con memoria");

/*
 * ISTRUZIONI DELL'AGENTE:
 *
 * Notiamo che le istruzioni NON includono info sull'utente.
 * Queste verranno iniettate AUTOMATICAMENTE dal UserPreferencesMemory!
 *
 * Il flow è:
 * 1. Utente scrive messaggio
 * 2. Framework chiama preferencesMemory.InvokingAsync()
 * 3. Il contesto viene AGGIUNTO alle istruzioni
 * 4. L'LLM riceve istruzioni + contesto + messaggi
 * 5. Dopo la risposta, framework chiama preferencesMemory.InvokedAsync()
 * 6. Il provider estrae e salva eventuali nuove preferenze
 */
string agentInstructions = """
    Sei DevAssistant, un assistente per sviluppatori software.

    Il tuo compito è aiutare gli sviluppatori con:
    - Rispondere a domande su programmazione
    - Suggerire best practices
    - Aiutare con debugging
    - Spiegare concetti tecnici

    IMPORTANTE SULLA MEMORIA:
    - Ricorda ciò che l'utente ti dice durante la conversazione
    - Se l'utente si presenta o condivide preferenze, ricordalo
    - Usa le informazioni dalle sessioni precedenti (se disponibili nel contesto)
    - Personalizza le risposte in base a ciò che sai dell'utente

    Sii conciso ma utile. Usa esempi di codice quando appropriato.
    """;

/*
 * CREAZIONE DELL'AGENTE CON ChatClientAgentOptions:
 *
 * Usiamo ChatClientAgentOptions per configurazione avanzata:
 * - ChatOptions.Instructions: le istruzioni dell'agente
 * - AIContextProviderFactory: factory per creare AIContextProvider per ogni thread
 *
 * NOTA SULLA FACTORY:
 * La factory viene chiamata quando si crea un nuovo thread.
 * Restituisce un'istanza di AIContextProvider per quel thread.
 * Qui usiamo sempre la stessa istanza (preferencesMemory) per condividere
 * le preferenze tra tutti i thread.
 */
ChatClientAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(model)
    .CreateAIAgent(new ChatClientAgentOptions
    {
        Name = "DevAssistant",
        ChatOptions = new ChatOptions
        {
            // Le istruzioni possono essere passate qui
            // Ma per semplicità le passiamo nel primo messaggio
        },
        // Factory che restituisce il nostro AIContextProvider
        // ctx contiene lo stato serializzato (se il thread è stato deserializzato)
        AIContextProviderFactory = ctx => preferencesMemory
    });

// Aggiungiamo le istruzioni come system message
// (il framework supporta anche ChatOptions.Instructions, ma per compatibilità usiamo questo approccio)

ConsoleHelper.WriteInfo("Agente creato con UserPreferencesMemory collegata");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * GESTIONE CONVERSAZIONI SALVATE
 * ═══════════════════════════════════════════════════════════════════════════════
 */

ConsoleHelper.WriteSeparator("Step 3: Verifica conversazioni salvate");

var savedConversations = ThreadPersistence.ListSavedConversations().ToList();
AgentThread? existingThread = null;
string conversationId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";

if (savedConversations.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Conversazioni salvate trovate:");
    for (int i = 0; i < savedConversations.Count; i++)
    {
        var info = ThreadPersistence.GetConversationInfo(savedConversations[i]);
        if (info != null)
        {
            Console.WriteLine($"  [{i + 1}] {info.ConversationId}");
            Console.WriteLine($"      Ultimo aggiornamento: {info.LastModified:g}");
            Console.WriteLine($"      Dimensione: {info.FileSizeBytes / 1024.0:F1} KB");
        }
    }
    Console.WriteLine($"  [N] Nuova conversazione");
    Console.WriteLine();

    Console.Write("Scegli (numero o N): ");
    var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

    if (int.TryParse(choice, out int index) && index > 0 && index <= savedConversations.Count)
    {
        conversationId = savedConversations[index - 1];

        /*
         * CARICAMENTO THREAD:
         *
         * Usiamo agent.DeserializeThread() attraverso ThreadPersistence.
         * Il thread viene ricreato con:
         * - Tutti i messaggi precedenti
         * - Lo stato dell'AIContextProvider (le preferenze)
         */
        existingThread = await ThreadPersistence.LoadThreadAsync(agent, conversationId);

        if (existingThread != null)
        {
            ConsoleHelper.WriteSuccess($"Conversazione '{conversationId}' caricata!");
        }
        else
        {
            ConsoleHelper.WriteError("Impossibile caricare la conversazione");
            existingThread = null;
        }
    }
    else
    {
        ConsoleHelper.WriteInfo("Avvio nuova conversazione");
    }
}
else
{
    ConsoleHelper.WriteInfo("Nessuna conversazione salvata - Avvio nuova sessione");
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * SETUP DEL THREAD
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * Se abbiamo caricato un thread esistente, lo usiamo.
 * Altrimenti ne creiamo uno nuovo.
 */

AgentThread thread = existingThread ?? agent.GetNewThread();

Console.WriteLine();
ConsoleHelper.WriteInfo(existingThread != null
    ? "Usando thread caricato - la conversazione continua da dove era rimasta"
    : "Nuovo thread creato - inizia una nuova conversazione");

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * LOOP DI CONVERSAZIONE
 * ═══════════════════════════════════════════════════════════════════════════════
 */

ConsoleHelper.WriteSeparator("Step 4: Avvio conversazione");

Console.WriteLine();
Console.WriteLine("COMANDI SPECIALI:");
Console.WriteLine("  'esci' o 'quit'    - Termina e salva la conversazione");
Console.WriteLine("  'reset'            - Cancella preferenze e conversazioni");
Console.WriteLine("  'memoria'          - Mostra stato della memoria");
Console.WriteLine("  'salva'            - Forza salvataggio conversazione");
Console.WriteLine();

// Suggerimenti per testare la memoria
if (string.IsNullOrEmpty(preferencesMemory.Preferences.Nome))
{
    Console.WriteLine("SUGGERIMENTO: Prova a dire 'Mi chiamo [tuo nome]' per testare la memoria!");
    Console.WriteLine("              Oppure: 'Sto lavorando sul progetto [nome progetto]'");
    Console.WriteLine();
}

// Inviamo le istruzioni come primo messaggio di sistema
// Questo è un workaround perché ChatCompletionOptions.Instructions non è sempre supportato
var systemMessage = agentInstructions;

int messageCount = 0;

while (true)
{
    // Input utente
    ConsoleHelper.WriteUserMessage("");
    Console.Write("> ");
    var userInput = Console.ReadLine();

    // Gestione input vuoto
    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    // Comandi speciali
    var command = userInput.Trim().ToLowerInvariant();

    if (command is "esci" or "quit" or "exit")
    {
        // Salviamo la conversazione prima di uscire
        await ThreadPersistence.SaveThreadAsync(thread, conversationId);
        preferencesMemory.SavePreferences();

        Console.WriteLine();
        ConsoleHelper.WriteSuccess($"Conversazione salvata come '{conversationId}'");
        ConsoleHelper.WriteSuccess("Preferenze salvate");
        ConsoleHelper.WriteInfo("Alla prossima!");
        break;
    }

    if (command == "reset")
    {
        Console.Write("Sei sicuro di voler cancellare TUTTE le preferenze e conversazioni? (s/n): ");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() == "s")
        {
            preferencesMemory.ClearPreferences();

            // Cancelliamo tutte le conversazioni
            foreach (var convId in ThreadPersistence.ListSavedConversations())
            {
                ThreadPersistence.DeleteThread(convId);
            }

            // Creiamo un nuovo thread
            thread = agent.GetNewThread();
            conversationId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
            messageCount = 0;

            ConsoleHelper.WriteSuccess("Memoria resettata completamente!");
        }
        continue;
    }

    if (command == "memoria")
    {
        Console.WriteLine();
        Console.WriteLine("=== STATO MEMORIA ===");
        Console.WriteLine();
        Console.WriteLine("PREFERENZE UTENTE:");
        Console.WriteLine($"  Nome: {preferencesMemory.Preferences.Nome ?? "(non impostato)"}");
        Console.WriteLine($"  Stile: {preferencesMemory.Preferences.StileComunicazione ?? "(non impostato)"}");
        Console.WriteLine($"  Progetto: {preferencesMemory.Preferences.ProgettoCorrente ?? "(non impostato)"}");
        Console.WriteLine($"  Interessi: {(preferencesMemory.Preferences.ArgomentiInteresse.Count > 0 ? string.Join(", ", preferencesMemory.Preferences.ArgomentiInteresse) : "(nessuno)")}");
        Console.WriteLine();
        Console.WriteLine("CONVERSAZIONE:");
        Console.WriteLine($"  ID: {conversationId}");
        Console.WriteLine($"  Messaggi in sessione: {messageCount}");
        Console.WriteLine();
        Console.WriteLine("CONVERSAZIONI SALVATE:");
        var saved = ThreadPersistence.ListSavedConversations().ToList();
        if (saved.Count > 0)
        {
            foreach (var id in saved)
            {
                Console.WriteLine($"  - {id}");
            }
        }
        else
        {
            Console.WriteLine("  (nessuna)");
        }
        Console.WriteLine();
        continue;
    }

    if (command == "salva")
    {
        await ThreadPersistence.SaveThreadAsync(thread, conversationId);
        preferencesMemory.SavePreferences();
        ConsoleHelper.WriteSuccess($"Conversazione salvata come '{conversationId}'");
        continue;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Invocazione dell'agente
    // ─────────────────────────────────────────────────────────────────────────

    try
    {
        /*
         * COSA SUCCEDE DURANTE RunStreamingAsync:
         *
         * 1. Il messaggio utente viene aggiunto al thread
         * 2. Il framework chiama TUTTI i contextProviders registrati
         *    → preferencesMemory.InvokingAsync() viene chiamato
         *    → Genera contesto dalle preferenze salvate
         * 3. Il contesto viene aggiunto alle istruzioni
         * 4. Tutto viene inviato all'LLM
         * 5. La risposta viene streamata
         * 6. Il framework chiama preferencesMemory.InvokedAsync()
         *    → Estrae nuove preferenze dai messaggi
         * 7. La risposta viene aggiunta al thread
         */

        ConsoleHelper.WriteAgentMessage("");

        // Per il primo messaggio, includiamo le istruzioni come contesto
        var promptWithContext = messageCount == 0
            ? $"[Contesto sistema: {systemMessage}]\n\n{userInput}"
            : userInput;

        await foreach (var update in agent.RunStreamingAsync(promptWithContext, thread))
        {
            // Streaming della risposta
            ConsoleHelper.WriteStreamChunk(update.ToString());
        }

        ConsoleHelper.EndStreamLine();

        messageCount++;

        // Salviamo periodicamente (ogni 5 scambi)
        if (messageCount % 5 == 0)
        {
            await ThreadPersistence.SaveThreadAsync(thread, conversationId);
            ConsoleHelper.WriteInfo("[Auto-save conversazione]");
        }
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Errore durante l'elaborazione: {ex.Message}");

        // In caso di errore, mostriamo più dettagli per debug
        if (ex.InnerException != null)
        {
            Console.WriteLine($"  Dettaglio: {ex.InnerException.Message}");
        }
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * RIEPILOGO CONCETTI APPRESI
 * ═══════════════════════════════════════════════════════════════════════════════
 *
 * 1. MEMORIA IMPLICITA (Thread):
 *    - AgentThread mantiene tutti i messaggi in memoria
 *    - L'agente "ricorda" la conversazione corrente automaticamente
 *
 * 2. PERSISTENZA THREAD:
 *    - thread.Serialize() → salva il thread come JsonElement
 *    - agent.DeserializeThread() → ricarica il thread
 *    - Permette di riprendere conversazioni tra sessioni
 *
 * 3. AIContextProvider:
 *    - Classe astratta per iniettare contesto dinamico
 *    - InvokingAsync: chiamato PRIMA dell'invocazione LLM
 *    - InvokedAsync: chiamato DOPO l'invocazione LLM
 *    - Restituisce AIContext con Instructions, Messages, Tools
 *
 * 4. MEMORIA A LUNGO TERMINE:
 *    - Estraiamo FATTI importanti dalle conversazioni
 *    - Li salviamo in formato strutturato (JSON, DB, etc.)
 *    - Li iniettiamo nel contesto quando rilevanti
 *
 * NEL PROSSIMO PROGETTO:
 * - Vector Store per memoria semantica
 * - Embeddings per ricerca per similarità
 * - RAG (Retrieval Augmented Generation)
 */
