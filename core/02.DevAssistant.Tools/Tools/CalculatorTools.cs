/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                          CALCULATOR TOOLS                                    ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Tools for mathematical operations.                                          ║
 * ║                                                                              ║
 * ║  WHY A CALCULATOR TOOL?                                                      ║
 * ║  LLMs are notoriously unreliable with mathematics!                           ║
 * ║  Providing a calculator tool allows the agent to delegate                    ║
 * ║  calculations to deterministic code, getting precise results.                ║
 * ║                                                                              ║
 * ║  INTERESTING PATTERN:                                                        ║
 * ║  - The LLM is good at understanding WHAT to calculate from natural language  ║
 * ║  - The tool is good at EXECUTING the calculation precisely                   ║
 * ║  - Together: human understanding + computational precision                   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.ComponentModel;

namespace DevAssistant.Tools.Tools;

/// <summary>
/// Tools for mathematical operations.
/// Static class because it doesn't require state.
/// </summary>
public static class CalculatorTools
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: Calculate - Basic operations
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Performs a basic mathematical calculation (addition, subtraction, multiplication, division). Use this tool for any numerical calculation.")]
    public static string Calculate(
        [Description("The first number of the operation")]
        double a,
        [Description("The operator: '+' (addition), '-' (subtraction), '*' (multiplication), '/' (division), '%' (modulo), '^' (power)")]
        string operatore,
        [Description("The second number of the operation")]
        double b)
    {
        try
        {
            double result = operatore switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => b != 0 ? a / b : throw new DivideByZeroException("Division by zero"),
                "%" => b != 0 ? a % b : throw new DivideByZeroException("Modulo by zero"),
                "^" => Math.Pow(a, b),
                _ => throw new ArgumentException($"Operator '{operatore}' not recognized")
            };

            return $"{a} {operatore} {b} = {result}";
        }
        catch (Exception ex)
        {
            return $"Calculation error: {ex.Message}";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: CalculatePercentage
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Calculates percentages. Can calculate: the percentage of a number, percentage increase/decrease, or what percentage one number is of another.")]
    public static string CalculatePercentage(
        [Description("The type of calculation: 'of' (X% of Y), 'change' (% change from X to Y), 'what' (X is what % of Y)")]
        string calculationType,
        [Description("The first value (percentage for 'of', initial value for 'change', part for 'what')")]
        double value1,
        [Description("The second value (the base number)")]
        double value2)
    {
        try
        {
            return calculationType.ToLowerInvariant() switch
            {
                "of" => $"{value1}% of {value2} = {value2 * value1 / 100}",
                "change" => value1 != 0
                    ? $"Change from {value1} to {value2} = {((value2 - value1) / value1) * 100:F2}%"
                    : "Error: initial value cannot be zero",
                "what" => value2 != 0
                    ? $"{value1} is {(value1 / value2) * 100:F2}% of {value2}"
                    : "Error: base value cannot be zero",
                _ => "Error: invalid type. Use 'of', 'change' or 'what'"
            };
        }
        catch (Exception ex)
        {
            return $"Calculation error: {ex.Message}";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: ConvertUnits
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Converts common units of measurement (length, weight, temperature). Use this tool for conversions between metric and imperial systems.")]
    public static string ConvertUnits(
        [Description("The value to convert")]
        double value,
        [Description("The source unit (e.g., 'km', 'm', 'mi', 'ft', 'kg', 'lb', 'celsius', 'fahrenheit')")]
        string fromUnit,
        [Description("The destination unit")]
        string toUnit)
    {
        try
        {
            var from = fromUnit.ToLowerInvariant();
            var to = toUnit.ToLowerInvariant();

            // Length conversions
            double? result = (from, to) switch
            {
                // Kilometers
                ("km", "m") => value * 1000,
                ("km", "mi") => value * 0.621371,
                ("km", "ft") => value * 3280.84,

                // Meters
                ("m", "km") => value / 1000,
                ("m", "ft") => value * 3.28084,
                ("m", "cm") => value * 100,

                // Miles
                ("mi", "km") => value * 1.60934,
                ("mi", "m") => value * 1609.34,
                ("mi", "ft") => value * 5280,

                // Feet
                ("ft", "m") => value * 0.3048,
                ("ft", "km") => value * 0.0003048,
                ("ft", "mi") => value / 5280,

                // Weight
                ("kg", "lb") => value * 2.20462,
                ("kg", "g") => value * 1000,
                ("lb", "kg") => value * 0.453592,
                ("g", "kg") => value / 1000,

                // Temperature
                ("celsius", "fahrenheit") or ("c", "f") => (value * 9 / 5) + 32,
                ("fahrenheit", "celsius") or ("f", "c") => (value - 32) * 5 / 9,
                ("celsius", "kelvin") or ("c", "k") => value + 273.15,
                ("kelvin", "celsius") or ("k", "c") => value - 273.15,

                _ => null
            };

            return result.HasValue
                ? $"{value} {fromUnit} = {result.Value:F4} {toUnit}"
                : $"Conversion from '{fromUnit}' to '{toUnit}' not supported";
        }
        catch (Exception ex)
        {
            return $"Conversion error: {ex.Message}";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: CalculateStatistics
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * Example of a tool that accepts an array as parameter.
     * The LLM is very good at extracting lists of numbers from text!
     */

    [Description("Calculates basic statistics (mean, sum, minimum, maximum, median) for a list of numbers.")]
    public static string CalculateStatistics(
        [Description("The numbers to analyze, separated by comma (e.g., '10, 20, 30, 40')")]
        string numbers)
    {
        try
        {
            // Parse numbers from the string
            var values = numbers
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.Parse(s.Trim()))
                .ToList();

            if (values.Count == 0)
            {
                return "Error: no numbers provided";
            }

            // Calculate statistics
            var sum = values.Sum();
            var avg = values.Average();
            var min = values.Min();
            var max = values.Max();
            var count = values.Count;

            // Median
            var sorted = values.OrderBy(x => x).ToList();
            var median = count % 2 == 0
                ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2
                : sorted[count / 2];

            return $"""
                Statistics for {count} numbers:
                - Sum: {sum}
                - Mean: {avg:F2}
                - Median: {median:F2}
                - Minimum: {min}
                - Maximum: {max}
                """;
        }
        catch (FormatException)
        {
            return "Error: invalid number format. Use comma-separated numbers (e.g., '10, 20, 30')";
        }
        catch (Exception ex)
        {
            return $"Calculation error: {ex.Message}";
        }
    }
}
