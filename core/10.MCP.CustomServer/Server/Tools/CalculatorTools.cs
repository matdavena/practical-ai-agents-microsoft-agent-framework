// ============================================================================
// 10. MCP CUSTOM SERVER
// FILE: CalculatorTools.cs
// ============================================================================
//
// Tool MCP per operazioni matematiche.
//
// COME FUNZIONA:
// 1. [McpServerToolType] marca la classe come contenitore di tool
// 2. [McpServerTool] marca ogni metodo come tool esposto via MCP
// 3. [Description] fornisce la descrizione per l'LLM
// 4. I parametri diventano automaticamente i parametri del tool
//
// L'SDK MCP genera automaticamente lo schema JSON per ogni tool!
// ============================================================================

using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Server.Tools;

/// <summary>
/// Tool per operazioni matematiche.
/// Ogni metodo pubblico con [McpServerTool] diventa un tool MCP.
/// </summary>
[McpServerToolType]
public sealed class CalculatorTools
{
    // ========================================================================
    // OPERAZIONI BASE
    // ========================================================================

    [McpServerTool(Name = "add")]
    [Description("Somma due numeri e restituisce il risultato.")]
    public static double Add(
        [Description("Primo numero da sommare")] double a,
        [Description("Secondo numero da sommare")] double b)
    {
        return a + b;
    }

    [McpServerTool(Name = "subtract")]
    [Description("Sottrae il secondo numero dal primo.")]
    public static double Subtract(
        [Description("Numero da cui sottrarre")] double a,
        [Description("Numero da sottrarre")] double b)
    {
        return a - b;
    }

    [McpServerTool(Name = "multiply")]
    [Description("Moltiplica due numeri.")]
    public static double Multiply(
        [Description("Primo fattore")] double a,
        [Description("Secondo fattore")] double b)
    {
        return a * b;
    }

    [McpServerTool(Name = "divide")]
    [Description("Divide il primo numero per il secondo. Restituisce errore se si divide per zero.")]
    public static string Divide(
        [Description("Dividendo")] double a,
        [Description("Divisore (non può essere zero)")] double b)
    {
        if (b == 0)
            return "Errore: divisione per zero non consentita";

        return (a / b).ToString();
    }

    // ========================================================================
    // OPERAZIONI AVANZATE
    // ========================================================================

    [McpServerTool(Name = "power")]
    [Description("Eleva un numero a potenza.")]
    public static double Power(
        [Description("Base")] double baseNum,
        [Description("Esponente")] double exponent)
    {
        return Math.Pow(baseNum, exponent);
    }

    [McpServerTool(Name = "sqrt")]
    [Description("Calcola la radice quadrata di un numero.")]
    public static string SquareRoot(
        [Description("Numero di cui calcolare la radice (deve essere >= 0)")] double number)
    {
        if (number < 0)
            return "Errore: impossibile calcolare la radice quadrata di un numero negativo";

        return Math.Sqrt(number).ToString();
    }

    [McpServerTool(Name = "percentage")]
    [Description("Calcola la percentuale di un numero. Es: percentage(200, 15) = 30 (15% di 200)")]
    public static double Percentage(
        [Description("Numero totale")] double total,
        [Description("Percentuale da calcolare")] double percent)
    {
        return (total * percent) / 100;
    }

    [McpServerTool(Name = "average")]
    [Description("Calcola la media di una lista di numeri separati da virgola.")]
    public static string Average(
        [Description("Numeri separati da virgola (es: '10,20,30')")] string numbers)
    {
        try
        {
            var values = numbers.Split(',')
                .Select(n => double.Parse(n.Trim()))
                .ToList();

            if (values.Count == 0)
                return "Errore: nessun numero fornito";

            return values.Average().ToString("F2");
        }
        catch (FormatException)
        {
            return "Errore: formato numeri non valido. Usa numeri separati da virgola.";
        }
    }
}
