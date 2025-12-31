/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                              CONSOLE HELPER                                   ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Helper for formatting console output in a readable way.                     ║
 * ║                                                                               ║
 * ║  Uses Spectre.Console for:                                                   ║
 * ║  - Coloring messages based on role (User, Agent, System)                     ║
 * ║  - Creating visually distinguishable separators and titles                   ║
 * ║  - Formatting text clearly                                                   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using Spectre.Console;

namespace Common;

/// <summary>
/// Static helper for console output formatting.
/// Provides methods for printing colored and formatted messages.
/// </summary>
public static class ConsoleHelper
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TITLES AND SEPARATORS
     * ═══════════════════════════════════════════════════════════════════════════
     * Methods for creating visual elements that separate output sections.
     */

    /// <summary>
    /// Prints a large title with a frame.
    /// Used at application startup.
    /// </summary>
    public static void WriteTitle(string title)
    {
        AnsiConsole.Write(
            new FigletText(title)
                .LeftJustified()
                .Color(Color.Cyan1));
    }

    /// <summary>
    /// Prints a subtitle with description.
    /// </summary>
    public static void WriteSubtitle(string subtitle)
    {
        AnsiConsole.MarkupLine($"[grey]{subtitle}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Prints a horizontal separator.
    /// Useful for dividing conversations.
    /// </summary>
    public static void WriteSeparator()
    {
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
    }

    /// <summary>
    /// Prints a separator with a title.
    /// </summary>
    public static void WriteSeparator(string title)
    {
        AnsiConsole.Write(new Rule($"[yellow]{title}[/]").RuleStyle("grey"));
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CONVERSATION MESSAGES
     * ═══════════════════════════════════════════════════════════════════════════
     * Methods for printing conversation messages between user and agent.
     * Each role has a different color for easier reading.
     */

    /// <summary>
    /// Prints a user message.
    /// Color: Green (indicates input)
    /// </summary>
    public static void WriteUserMessage(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]👤 User:[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Prints an agent message.
    /// Color: Blue (indicates AI response)
    /// </summary>
    public static void WriteAgentMessage(string message)
    {
        AnsiConsole.MarkupLine($"[bold blue]🤖 Agent:[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Prints the agent header (without message).
    /// Used before streaming.
    /// </summary>
    public static void WriteAgentHeader()
    {
        AnsiConsole.Markup("[bold blue]🤖 Agent:[/] ");
    }

    /// <summary>
    /// Prints a text chunk during streaming.
    /// Does not add newline to allow concatenation.
    /// </summary>
    public static void WriteStreamChunk(string chunk)
    {
        // Escape markup to avoid issues with special characters
        Console.Write(chunk);
    }

    /// <summary>
    /// Ends a streaming line.
    /// </summary>
    public static void EndStreamLine()
    {
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// Prints a system message.
    /// Color: Yellow (indicates system information)
    /// </summary>
    public static void WriteSystemMessage(string message)
    {
        AnsiConsole.MarkupLine($"[bold yellow]⚙️  System:[/] [grey]{Markup.Escape(message)}[/]");
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * STATUS MESSAGES
     * ═══════════════════════════════════════════════════════════════════════════
     * Methods for indicating success, error, warning, etc.
     */

    /// <summary>
    /// Prints a success message.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]✅ {Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Prints an error message.
    /// </summary>
    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]❌ {Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Prints a warning message.
    /// </summary>
    public static void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[bold orange1]⚠️  {Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Prints an informational message.
    /// </summary>
    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[bold cyan]ℹ️  {Markup.Escape(message)}[/]");
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * USER INPUT
     * ═══════════════════════════════════════════════════════════════════════════
     * Methods for requesting user input.
     */

    /// <summary>
    /// Requests text input from the user.
    /// </summary>
    public static string AskInput(string prompt = "You")
    {
        return AnsiConsole.Ask<string>($"[green]{prompt}>[/]");
    }

    /// <summary>
    /// Requests confirmation (yes/no) from the user.
    /// </summary>
    public static bool AskConfirmation(string prompt)
    {
        return AnsiConsole.Confirm(prompt);
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * PANELS AND TABLES
     * ═══════════════════════════════════════════════════════════════════════════
     * Methods for displaying structured information.
     */

    /// <summary>
    /// Prints a panel with a title.
    /// Useful for showing information in a box.
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
    /// Prints configuration information in key-value format.
    /// </summary>
    public static void WriteConfiguration(Dictionary<string, string> config)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Parameter[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

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
