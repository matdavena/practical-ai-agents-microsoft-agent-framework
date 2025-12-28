/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║                         FILE SYSTEM TOOLS                                    ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║  Tools for interacting with the file system.                                 ║
 * ║                                                                              ║
 * ║  DIFFERENCE FROM DateTimeTools:                                              ║
 * ║  - This is a class with an INSTANCE (not static)                             ║
 * ║  - It has STATE (WorkingDirectory)                                           ║
 * ║  - Methods are instance methods, not static                                  ║
 * ║                                                                              ║
 * ║  SECURITY:                                                                   ║
 * ║  - We limit operations to a "sandbox" directory                              ║
 * ║  - Never allow arbitrary file system access!                                 ║
 * ║  - Always validate paths to prevent path traversal attacks                   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

using System.ComponentModel;

namespace DevAssistant.Tools.Tools;

/// <summary>
/// Tools for file system operations.
///
/// NOTE: This class uses an instance to maintain the WorkingDirectory.
/// When using AIFunctionFactory.Create with instance methods,
/// pass the instance as the target.
/// </summary>
public class FileSystemTools
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * CONFIGURATION AND STATE
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Working directory (sandbox) - all operations are limited to here.
    /// </summary>
    public string WorkingDirectory { get; }

    /// <summary>
    /// Creates a new FileSystemTools instance with the specified working directory.
    /// </summary>
    /// <param name="workingDirectory">The sandbox directory. If it doesn't exist, it will be created.</param>
    public FileSystemTools(string? workingDirectory = null)
    {
        // Default: a "workspace" subfolder in the current directory
        WorkingDirectory = workingDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "workspace");

        // Create the directory if it doesn't exist
        if (!Directory.Exists(WorkingDirectory))
        {
            Directory.CreateDirectory(WorkingDirectory);
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * SECURITY METHODS
     * ═══════════════════════════════════════════════════════════════════════════
     *
     * IMPORTANT: These methods are NOT tools!
     * They are private support methods for validation.
     */

    /// <summary>
    /// Validates and normalizes a path, ensuring it's inside the WorkingDirectory.
    /// Prevents path traversal attacks (e.g., "../../../etc/passwd").
    /// </summary>
    private string ValidateAndNormalizePath(string relativePath)
    {
        // Combine with working directory
        var fullPath = Path.GetFullPath(Path.Combine(WorkingDirectory, relativePath));

        // Verify the resulting path is inside the WorkingDirectory
        if (!fullPath.StartsWith(WorkingDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Access denied: path must be inside '{WorkingDirectory}'");
        }

        return fullPath;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: GetWorkingDirectory
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Gets the current working directory (sandbox). All file operations are limited to this directory.")]
    public string GetWorkingDirectory()
    {
        return WorkingDirectory;
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: ListFiles
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Lists all files in a directory. Use this tool to see what a folder contains.")]
    public string ListFiles(
        [Description("The relative path of the directory to explore. Use '.' for current directory.")]
        string relativePath = ".")
    {
        try
        {
            var fullPath = ValidateAndNormalizePath(relativePath);

            if (!Directory.Exists(fullPath))
            {
                return $"Error: directory '{relativePath}' does not exist.";
            }

            var files = Directory.GetFiles(fullPath);
            var dirs = Directory.GetDirectories(fullPath);

            if (files.Length == 0 && dirs.Length == 0)
            {
                return $"Directory '{relativePath}' is empty.";
            }

            var result = $"Contents of '{relativePath}':\n\n";

            // Directories first
            if (dirs.Length > 0)
            {
                result += "📁 Directories:\n";
                foreach (var dir in dirs)
                {
                    result += $"   {Path.GetFileName(dir)}/\n";
                }
                result += "\n";
            }

            // Then files
            if (files.Length > 0)
            {
                result += "📄 Files:\n";
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    result += $"   {info.Name} ({FormatFileSize(info.Length)})\n";
                }
            }

            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Security error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: ReadFile
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Reads the content of a text file. Use this tool when the user wants to see the contents of a file.")]
    public string ReadFile(
        [Description("The relative path of the file to read.")]
        string relativePath)
    {
        try
        {
            var fullPath = ValidateAndNormalizePath(relativePath);

            if (!File.Exists(fullPath))
            {
                return $"Error: file '{relativePath}' does not exist.";
            }

            var content = File.ReadAllText(fullPath);

            // Limit length to avoid overly long responses
            if (content.Length > 5000)
            {
                content = content[..5000] + "\n\n... [content truncated, file is too large]";
            }

            return $"Contents of '{relativePath}':\n\n{content}";
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Security error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: WriteFile
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Writes content to a text file. If the file exists, it will be overwritten. Use this tool when the user wants to create or modify a file.")]
    public string WriteFile(
        [Description("The relative path of the file to create/modify.")]
        string relativePath,
        [Description("The text content to write to the file.")]
        string content)
    {
        try
        {
            var fullPath = ValidateAndNormalizePath(relativePath);

            // Create parent directory if it doesn't exist
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);

            return $"File '{relativePath}' written successfully ({content.Length} characters).";
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Security error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: CreateDirectory
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Creates a new directory. Use this tool when the user wants to create a new folder.")]
    public string CreateDirectory(
        [Description("The relative path of the directory to create.")]
        string relativePath)
    {
        try
        {
            var fullPath = ValidateAndNormalizePath(relativePath);

            if (Directory.Exists(fullPath))
            {
                return $"Directory '{relativePath}' already exists.";
            }

            Directory.CreateDirectory(fullPath);
            return $"Directory '{relativePath}' created successfully.";
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Security error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error creating directory: {ex.Message}";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * TOOL: DeleteFile
     * ═══════════════════════════════════════════════════════════════════════════
     */

    [Description("Deletes a file. WARNING: this operation is irreversible. Use this tool only when the user explicitly asks to delete a file.")]
    public string DeleteFile(
        [Description("The relative path of the file to delete.")]
        string relativePath)
    {
        try
        {
            var fullPath = ValidateAndNormalizePath(relativePath);

            if (!File.Exists(fullPath))
            {
                return $"Error: file '{relativePath}' does not exist.";
            }

            File.Delete(fullPath);
            return $"File '{relativePath}' deleted successfully.";
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Security error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error deleting file: {ex.Message}";
        }
    }

    /*
     * ═══════════════════════════════════════════════════════════════════════════
     * UTILITY METHODS (not tools)
     * ═══════════════════════════════════════════════════════════════════════════
     */

    /// <summary>
    /// Formats file size in a human-readable way.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
