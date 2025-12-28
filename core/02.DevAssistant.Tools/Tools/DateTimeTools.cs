/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                           DATETIME TOOLS                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Tools for obtaining date and time information.                              ║
 * ║                                                                              ║
 * ║  KEY CONCEPTS:                                                               ║
 * ║  - Every public method can become a "Tool" for the agent                     ║
 * ║  - The [Description] attribute describes the tool to the LLM                 ║
 * ║  - Parameters can have [Description] to explain what to pass                 ║
 * ║  - The LLM reads these descriptions to understand WHEN and HOW to use tools  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.ComponentModel;

namespace DevAssistant.Tools.Tools;

/// <summary>
/// Collection of tools for date and time operations.
///
/// IMPORTANT NOTE:
/// This is a STATIC class because its methods don't need state.
/// For tools that require configuration or dependencies, use a class
/// with an instance (see FileSystemTools).
/// </summary>
public static class DateTimeTools
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: GetCurrentDateTime
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * This tool returns the current date and time.
     *
     * The [Description] attribute is FUNDAMENTAL:
     * - The LLM reads it to understand what the tool does
     * - Must be clear and concise
     * - Influences when the LLM decides to use the tool
     */

    /// <summary>
    /// Gets the current date and time in the specified format.
    /// </summary>
    [Description("Gets the current date and time. Use this tool when the user asks what time it is, what day it is, or the current date.")]
    public static string GetCurrentDateTime(
        [Description("The type of time to return: 'local' for local time, 'utc' for UTC time")]
        string timeType = "local")
    {
        /*
         * NOTE: The return type can be any serializable type.
         * - string: the most common, easy for the LLM to use
         * - complex objects: get serialized to JSON
         * - primitives (int, bool, etc.): converted to string
         */

        DateTime dateTime = timeType.ToLowerInvariant() switch
        {
            "utc" => DateTime.UtcNow,
            _ => DateTime.Now  // default: local
        };

        // Return a readable format that the LLM can easily interpret
        return $"{dateTime:dddd, MMMM dd, yyyy - HH:mm:ss} ({timeType.ToUpperInvariant()})";
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: GetCurrentTimezone
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Gets information about the current timezone.
    /// </summary>
    [Description("Gets the current system timezone. Use this tool when the user asks what timezone they are in.")]
    public static string GetCurrentTimezone()
    {
        var tz = TimeZoneInfo.Local;
        return $"Timezone: {tz.DisplayName} (ID: {tz.Id}, Offset: {tz.BaseUtcOffset})";
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: CalculateDateDifference
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * This tool shows how to handle more complex parameters.
     * The LLM is very good at extracting dates from natural language!
     */

    /// <summary>
    /// Calculates the difference between two dates.
    /// </summary>
    [Description("Calculates how many days, hours and minutes there are between two dates. Use this tool when the user asks how much time is left until a date or how much time has passed since a date.")]
    public static string CalculateDateDifference(
        [Description("The first date in yyyy-MM-dd format (e.g., 2024-12-25)")]
        string fromDate,
        [Description("The second date in yyyy-MM-dd format (e.g., 2025-01-01). If not specified, uses current date.")]
        string? toDate = null)
    {
        try
        {
            var from = DateTime.Parse(fromDate);
            var to = toDate != null ? DateTime.Parse(toDate) : DateTime.Now;

            var diff = to - from;
            var isPast = diff.TotalDays < 0;
            diff = isPast ? -diff : diff;  // Absolute value

            var direction = isPast ? "have passed" : "remaining";

            return $"From {from:MM/dd/yyyy} to {to:MM/dd/yyyy} {direction}: " +
                   $"{(int)diff.TotalDays} days, {diff.Hours} hours and {diff.Minutes} minutes";
        }
        catch (FormatException)
        {
            // It's good practice to handle errors in tools
            // The LLM can understand the error and retry or explain it to the user
            return "Error: invalid date format. Use yyyy-MM-dd format (e.g., 2024-12-25)";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: GetDayOfWeek
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Gets the day of the week for a specified date.
    /// </summary>
    [Description("Gets the day of the week (Monday, Tuesday, etc.) for a specific date. Use this tool when the user asks what day of the week a certain date was/will be.")]
    public static string GetDayOfWeek(
        [Description("The date in yyyy-MM-dd format (e.g., 2024-12-25)")]
        string date)
    {
        try
        {
            var dt = DateTime.Parse(date);
            var dayName = dt.ToString("dddd", System.Globalization.CultureInfo.InvariantCulture);
            return $"{dt:MM/dd/yyyy} is/was a {dayName}";
        }
        catch (FormatException)
        {
            return "Error: invalid date format. Use yyyy-MM-dd format (e.g., 2024-12-25)";
        }
    }
}
