/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                              CONSOLE HELPER                                   ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Helper per formattare l'output nella console in modo leggibile.             ║
 * ║                                                                               ║
 * ║  Utilizza Spectre.Console per:                                               ║
 * ║  - Colorare i messaggi in base al ruolo (User, Agent, System)                ║
 * ║  - Creare separatori e titoli visivamente distinguibili                      ║
 * ║  - Formattare il testo in modo chiaro                                        ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using Spectre.Console;

namespace Common;

/// <summary>
/// Helper statico per la formattazione dell'output console.
/// Fornisce metodi per stampare messaggi colorati e formattati.
/// </summary>
public static class ConsoleHelper
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TITOLI E SEPARATORI
     * ═══════════════════════════════════════════════════════════════════════════
     * Metodi per creare elementi visuali che separano le sezioni dell'output.
     */

    /// <summary>
    /// Stampa un titolo grande con cornice.
    /// Usato all'avvio dell'applicazione.
    /// </summary>
    public static void WriteTitle(string title)
    {
        AnsiConsole.Write(
            new FigletText(title)
                .LeftJustified()
                .Color(Color.Cyan1));
    }

    /// <summary>
    /// Stampa un sottotitolo con descrizione.
    /// </summary>
    public static void WriteSubtitle(string subtitle)
    {
        AnsiConsole.MarkupLine($"[grey]{subtitle}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Stampa un separatore orizzontale.
    /// Utile per dividere le conversazioni.
    /// </summary>
    public static void WriteSeparator()
    {
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
    }

    /// <summary>
    /// Stampa un separatore con titolo.
    /// </summary>
    public static void WriteSeparator(string title)
    {
        AnsiConsole.Write(new Rule($"[yellow]{title}[/]").RuleStyle("grey"));
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * MESSAGGI DI CONVERSAZIONE
     * ═══════════════════════════════════════════════════════════════════════════
     * Metodi per stampare i messaggi della conversazione tra utente e agente.
     * Ogni ruolo ha un colore diverso per facilitare la lettura.
     */

    /// <summary>
    /// Stampa un messaggio dell'utente.
    /// Colore: Verde (indica input)
    /// </summary>
    public static void WriteUserMessage(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]👤 User:[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Stampa un messaggio dell'agente.
    /// Colore: Blu (indica risposta AI)
    /// </summary>
    public static void WriteAgentMessage(string message)
    {
        AnsiConsole.MarkupLine($"[bold blue]🤖 Agent:[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Stampa l'intestazione dell'agente (senza messaggio).
    /// Usato prima dello streaming.
    /// </summary>
    public static void WriteAgentHeader()
    {
        AnsiConsole.Markup("[bold blue]🤖 Agent:[/] ");
    }

    /// <summary>
    /// Stampa un chunk di testo durante lo streaming.
    /// Non aggiunge newline per permettere la concatenazione.
    /// </summary>
    public static void WriteStreamChunk(string chunk)
    {
        // Escape del markup per evitare problemi con caratteri speciali
        Console.Write(chunk);
    }

    /// <summary>
    /// Termina una riga di streaming.
    /// </summary>
    public static void EndStreamLine()
    {
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// Stampa un messaggio di sistema.
    /// Colore: Giallo (indica informazioni di sistema)
    /// </summary>
    public static void WriteSystemMessage(string message)
    {
        AnsiConsole.MarkupLine($"[bold yellow]⚙️  System:[/] [grey]{Markup.Escape(message)}[/]");
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * MESSAGGI DI STATO
     * ═══════════════════════════════════════════════════════════════════════════
     * Metodi per indicare successo, errore, warning, ecc.
     */

    /// <summary>
    /// Stampa un messaggio di successo.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]✅ {Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Stampa un messaggio di errore.
    /// </summary>
    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]❌ {Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Stampa un messaggio di warning.
    /// </summary>
    public static void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[bold orange1]⚠️  {Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Stampa un messaggio informativo.
    /// </summary>
    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[bold cyan]ℹ️  {Markup.Escape(message)}[/]");
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * INPUT UTENTE
     * ═══════════════════════════════════════════════════════════════════════════
     * Metodi per richiedere input all'utente.
     */

    /// <summary>
    /// Richiede input testuale all'utente.
    /// </summary>
    public static string AskInput(string prompt = "Tu")
    {
        return AnsiConsole.Ask<string>($"[green]{prompt}>[/]");
    }

    /// <summary>
    /// Richiede conferma (sì/no) all'utente.
    /// </summary>
    public static bool AskConfirmation(string prompt)
    {
        return AnsiConsole.Confirm(prompt);
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * PANNELLI E TABELLE
     * ═══════════════════════════════════════════════════════════════════════════
     * Metodi per visualizzare informazioni strutturate.
     */

    /// <summary>
    /// Stampa un pannello con titolo.
    /// Utile per mostrare informazioni in un box.
    /// </summary>
    public static void WritePanel(string title, string content)
    {
        var panel = new Panel(Markup.Escape(content))
            .Header($"[bold yellow]{title}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Stampa informazioni di configurazione in formato chiave-valore.
    /// </summary>
    public static void WriteConfiguration(Dictionary<string, string> config)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Parametro[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Valore[/]").LeftAligned());

        foreach (var (key, value) in config)
        {
            table.AddRow(
                $"[cyan]{Markup.Escape(key)}[/]",
                $"[white]{Markup.Escape(value)}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
