using ModelContextProtocol.Server;
using NetContextServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NetContextServer.Tools;

/// <summary>
/// Provides MCP tools for file system operations, including file listing, project discovery, and file access.
/// </summary>
[McpServerToolType]
public static class FileTools
{
    /// <summary>
    /// Default JSON serializer options used across all file tools.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Standard error message for directory not found scenarios.
    /// </summary>
    private static readonly string[] ErrorDirectoryNotFound = ["Error: Directory not found"];

    /// <summary>
    /// Lists all .NET source files in the specified project directory.
    /// </summary>
    /// <param name="projectPath">Absolute path to the project directory containing the .cs files.</param>
    /// <returns>A JSON string containing an array of file paths.</returns>
    [McpServerTool("list_files")]
    [Description("Lists all .NET source files in the specified project directory.")]
    public static string ListFiles(
        [Description("Absolute path to the project directory containing the .cs files")]
        string projectPath) =>
        JsonSerializer.Serialize(FileService.ListFiles(projectPath), DefaultJsonOptions);

    /// <summary>
    /// Scans the current solution and returns all .csproj files found.
    /// </summary>
    /// <returns>A JSON string containing an array of project file paths.</returns>
    [McpServerTool("list_projects")]
    [Description("Scans the current solution and returns all .csproj files found.")]
    public static string ListProjects() => 
        JsonSerializer.Serialize(FileService.ListProjects(), DefaultJsonOptions);

    /// <summary>
    /// Searches a specific directory for .csproj files.
    /// </summary>
    /// <param name="directory">Absolute path to the directory to search for .csproj files.</param>
    /// <returns>A JSON string containing an array of project file paths.</returns>
    [McpServerTool("list_projects_in_dir")]
    [Description("Searches a specific directory for .csproj files.")]
    public static string ListProjectsInDirectory(
        [Description("Absolute path to the directory to search for .csproj files")]
        string directory) => 
        JsonSerializer.Serialize(FileService.ListProjectsInDirectory(directory), DefaultJsonOptions);

    /// <summary>
    /// Returns all .sln files found in the base directory.
    /// </summary>
    /// <returns>A JSON string containing an array of solution file paths.</returns>
    [McpServerTool("list_solutions")]
    [Description("Returns all .sln files found in the base directory.")]
    public static string ListSolutions() => 
        JsonSerializer.Serialize(FileService.ListSolutions(), DefaultJsonOptions);

    /// <summary>
    /// Lists all source files in a project directory.
    /// </summary>
    /// <param name="projectDir">Absolute path to the project directory to scan for source files.</param>
    /// <returns>A JSON string containing an array of source file paths.</returns>
    [McpServerTool("list_source_files")]
    [Description("Lists all source files in a project directory.")]
    public static string ListSourceFiles(
        [Description("Absolute path to the project directory to scan for source files")]
        string projectDir)
    {
        var result = FileService.ListSourceFiles(projectDir);
        // If it's a single item array starting with "Error:", return it as plain text
        if (result.Length == 1 && result[0].StartsWith("Error:"))
        {
            return result[0];
        }
        return JsonSerializer.Serialize(result, DefaultJsonOptions);
    }

    /// <summary>
    /// Reads and returns the contents of a specified file.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to read.</param>
    /// <returns>The contents of the file, or an error message if the file cannot be read.</returns>
    [McpServerTool("open_file")]
    [Description("Reads and returns the contents of a specified file.")]
    public static string OpenFile(
        [Description("Absolute path to the file to read")]
        string filePath) => 
        FileService.OpenFile(filePath);

    /// <summary>
    /// Sets the base directory for all file operations.
    /// </summary>
    /// <param name="directory">Absolute path to set as the new base directory. Must be a valid, existing directory.</param>
    /// <returns>A JSON string containing a confirmation message or an error message.</returns>
    [McpServerTool("set_base_directory")]
    [Description("Sets the base directory for all file operations.")]
    public static string SetBaseDirectory(
        [Description("Absolute path to set as the new base directory. Must be a valid, existing directory")]
        string directory)
    {
        if (!Directory.Exists(directory))
        {
            return JsonSerializer.Serialize(ErrorDirectoryNotFound, DefaultJsonOptions);
        }

        FileValidationService.SetBaseDirectory(directory);
        
        return JsonSerializer.Serialize(new[] { $"Base directory set to: {directory}" }, DefaultJsonOptions);
    }

    /// <summary>
    /// Returns the current base directory used for all file operations.
    /// </summary>
    /// <returns>A JSON string containing the current base directory and whether it exists.</returns>
    [McpServerTool("get_base_directory")]
    [Description("Returns the current base directory used for all file operations.")]
    public static string GetBaseDirectory()
    {
        try
        {
            var baseDir = FileValidationService.BaseDirectory;
            
            return JsonSerializer.Serialize(new
            {
                BaseDirectory = baseDir,
                Exists = Directory.Exists(baseDir)
            },
            DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Error = ex.Message }, DefaultJsonOptions);
        }
    }
}
