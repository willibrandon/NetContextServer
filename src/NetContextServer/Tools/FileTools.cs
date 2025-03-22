using ModelContextProtocol.Server;
using NetContextServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NetContextServer.Tools;

[McpToolType]
public static class FileTools
{
    // Shared JsonSerializerOptions instance to improve performance
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] ErrorDirectoryNotFound = ["Error: Directory not found"];

    [McpTool("list_files")]
    [Description("Lists all .NET source files in the specified project directory.")]
    public static string ListFiles(
        [Description("Absolute path to the project directory containing the .cs files")]
        string projectPath) =>
        JsonSerializer.Serialize(FileService.ListFiles(projectPath), DefaultJsonOptions);

    [McpTool("list_projects")]
    [Description("Scans the current solution and returns all .csproj files found.")]
    public static string ListProjects() => 
        JsonSerializer.Serialize(FileService.ListProjects(), DefaultJsonOptions);

    [McpTool("list_projects_in_dir")]
    [Description("Searches a specific directory for .csproj files.")]
    public static string ListProjectsInDirectory(
        [Description("Absolute path to the directory to search for .csproj files")]
        string directory) => 
        JsonSerializer.Serialize(FileService.ListProjectsInDirectory(directory), DefaultJsonOptions);

    [McpTool("list_solutions")]
    [Description("Returns all .sln files found in the base directory.")]
    public static string ListSolutions() => 
        JsonSerializer.Serialize(FileService.ListSolutions(), DefaultJsonOptions);

    [McpTool("list_source_files")]
    [Description("Lists all source files in a project directory.")]
    public static string ListSourceFiles(
        [Description("Absolute path to the project directory to scan for source files")]
        string projectDir) => 
        JsonSerializer.Serialize(FileService.ListSourceFiles(projectDir), DefaultJsonOptions);

    [McpTool("open_file")]
    [Description("Reads and returns the contents of a specified file.")]
    public static string OpenFile(
        [Description("Absolute path to the file to read")]
        string filePath) => 
        FileService.OpenFile(filePath);

    [McpTool("set_base_directory")]
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

    [McpTool("get_base_directory")]
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