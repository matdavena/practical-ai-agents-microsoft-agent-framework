// ============================================================================
// 10. MCP CUSTOM SERVER
// FILE: CalculatorTools.cs
// ============================================================================
//
// MCP tools for mathematical operations.
//
// HOW IT WORKS:
// 1. [McpServerToolType] marks the class as a tool container
// 2. [McpServerTool] marks each method as a tool exposed via MCP
// 3. [Description] provides the description for the LLM
// 4. Parameters automatically become the tool's parameters
//
// The MCP SDK automatically generates the JSON schema for each tool!
// ============================================================================

using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Server.Tools;

/// <summary>
/// Tools for mathematical operations.
/// Each public method with [McpServerTool] becomes an MCP tool.
/// </summary>
[McpServerToolType]
public sealed class CalculatorTools
{
    // ========================================================================
    // BASIC OPERATIONS
    // ========================================================================

    [McpServerTool(Name = "add")]
    [Description("Adds two numbers and returns the result.")]
    public static double Add(
        [Description("First number to add")] double a,
        [Description("Second number to add")] double b)
    {
        return a + b;
    }

    [McpServerTool(Name = "subtract")]
    [Description("Subtracts the second number from the first.")]
    public static double Subtract(
        [Description("Number to subtract from")] double a,
        [Description("Number to subtract")] double b)
    {
        return a - b;
    }

    [McpServerTool(Name = "multiply")]
    [Description("Multiplies two numbers.")]
    public static double Multiply(
        [Description("First factor")] double a,
        [Description("Second factor")] double b)
    {
        return a * b;
    }

    [McpServerTool(Name = "divide")]
    [Description("Divides the first number by the second. Returns error if dividing by zero.")]
    public static string Divide(
        [Description("Dividend")] double a,
        [Description("Divisor (cannot be zero)")] double b)
    {
        if (b == 0)
            return "Error: division by zero not allowed";

        return (a / b).ToString();
    }

    // ========================================================================
    // ADVANCED OPERATIONS
    // ========================================================================

    [McpServerTool(Name = "power")]
    [Description("Raises a number to a power.")]
    public static double Power(
        [Description("Base")] double baseNum,
        [Description("Exponent")] double exponent)
    {
        return Math.Pow(baseNum, exponent);
    }

    [McpServerTool(Name = "sqrt")]
    [Description("Calculates the square root of a number.")]
    public static string SquareRoot(
        [Description("Number to calculate the square root of (must be >= 0)")] double number)
    {
        if (number < 0)
            return "Error: cannot calculate the square root of a negative number";

        return Math.Sqrt(number).ToString();
    }

    [McpServerTool(Name = "percentage")]
    [Description("Calculates the percentage of a number. E.g.: percentage(200, 15) = 30 (15% of 200)")]
    public static double Percentage(
        [Description("Total number")] double total,
        [Description("Percentage to calculate")] double percent)
    {
        return (total * percent) / 100;
    }

    [McpServerTool(Name = "average")]
    [Description("Calculates the average of a list of comma-separated numbers.")]
    public static string Average(
        [Description("Comma-separated numbers (e.g.: '10,20,30')")] string numbers)
    {
        try
        {
            var values = numbers.Split(',')
                .Select(n => double.Parse(n.Trim()))
                .ToList();

            if (values.Count == 0)
                return "Error: no numbers provided";

            return values.Average().ToString("F2");
        }
        catch (FormatException)
        {
            return "Error: invalid number format. Use comma-separated numbers.";
        }
    }
}
