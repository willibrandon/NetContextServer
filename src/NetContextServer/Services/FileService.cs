namespace NetContextServer.Services;

/// <summary>
/// Provides functionality for working with files in a .NET project, including file listing, project discovery, and file access.
/// </summary>
internal static class FileService
{
    /// <summary>
    /// Gets the supported .NET file patterns for file operations.
    /// </summary>
    public static readonly string[] DotNetFilePatterns = 
    [
        "*.cs",    // C# source files
        "*.vb",    // Visual Basic source files
        "*.fs",    // F# source files
        "*.fsx",   // F# script files
        "*.fsi",   // F# signature files
        "*.cshtml", // Razor views
        "*.vbhtml", // VB Razor views
        "*.razor"  // Blazor components
    ];

    /// <summary>
    /// Lists all .NET project files (*.csproj) in the base directory and its subdirectories.
    /// </summary>
    /// <returns>An array of project file paths that are not ignored by the ignore patterns.</returns>
    public static string[] ListProjects()
    {
        FileValidationService.EnsureBaseDirectorySet();
        return [.. Directory.GetFiles(FileValidationService.BaseDirectory, "*.csproj", SearchOption.AllDirectories).Where(p => !IgnorePatternService.ShouldIgnoreFile(p))];
    }

    /// <summary>
    /// Lists all supported .NET files in a specific project directory.
    /// </summary>
    /// <param name="projectPath">The directory path to search for files.</param>
    /// <returns>An array of file paths, or an error message if the directory is invalid or inaccessible.</returns>
    public static string[] ListFiles(string projectPath)
    {
        if (!Directory.Exists(projectPath))
            return ["Error: Directory not found"];

        if (!FileValidationService.IsPathSafe(projectPath))
            return ["Error: Access to this directory is not allowed"];

        var allFiles = new List<string>();
        foreach (var pattern in DotNetFilePatterns)
        {
            allFiles.AddRange(Directory.GetFiles(projectPath, pattern, SearchOption.TopDirectoryOnly)
                .Where(f => !IgnorePatternService.ShouldIgnoreFile(f))
                .Select(f => f.ToLowerInvariant())); // Convert to lowercase for consistent comparison
        }

        return [.. allFiles.Distinct()]; // Remove any duplicates
    }

    /// <summary>
    /// Lists all solution files (*.sln) in the base directory and its subdirectories.
    /// </summary>
    /// <returns>An array of solution file paths.</returns>
    public static string[] ListSolutions()
    {
        FileValidationService.EnsureBaseDirectorySet();
        return Directory.GetFiles(FileValidationService.BaseDirectory, "*.sln", SearchOption.AllDirectories);
    }

    /// <summary>
    /// Lists all project files (*.csproj) in a specific directory and its subdirectories.
    /// </summary>
    /// <param name="directory">The directory path to search for project files.</param>
    /// <returns>An array of project file paths, or an error message if the directory is invalid or inaccessible.</returns>
    public static string[] ListProjectsInDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return ["Error: Directory not found"];

        if (!FileValidationService.IsPathSafe(directory))
            return ["Error: Access to this directory is not allowed"];

        return [.. Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories).Where(p => !IgnorePatternService.ShouldIgnoreFile(p))];
    }

    /// <summary>
    /// Lists all supported source files in a project directory and its subdirectories.
    /// </summary>
    /// <param name="projectDir">The project directory path to search for source files.</param>
    /// <returns>An array of source file paths, or an error message if the directory is invalid or inaccessible.</returns>
    public static string[] ListSourceFiles(string projectDir)
    {
        if (!Directory.Exists(projectDir))
            return ["Error: Directory not found"];

        if (!FileValidationService.IsPathSafe(projectDir))
            return ["Error: Access to this directory is not allowed"];

        var allFiles = new List<string>();
        foreach (var pattern in DotNetFilePatterns)
        {
            allFiles.AddRange(Directory.GetFiles(projectDir, pattern, SearchOption.AllDirectories)
                .Where(f => !IgnorePatternService.ShouldIgnoreFile(f)));
        }

        return [.. allFiles];
    }

    /// <summary>
    /// Opens and reads the contents of a file, with safety checks and size limitations.
    /// </summary>
    /// <param name="filePath">The path of the file to open.</param>
    /// <returns>The contents of the file, or an error message if the file cannot be accessed or is restricted.</returns>
    /// <remarks>
    /// Files larger than 100,000 characters will be truncated.
    /// The file must pass safety checks and not be in the ignore list.
    /// </remarks>
    public static string OpenFile(string filePath)
    {
        if (!File.Exists(filePath))
            return "Error: File not found";

        if (!FileValidationService.IsPathSafe(filePath))
            return "Error: Access to this file is not allowed";

        if (IgnorePatternService.ShouldIgnoreFile(filePath))
            return "Error: This file type is restricted";

        try
        {
            var content = File.ReadAllText(filePath);
            if (content.Length > 100_000)
            {
                content = content[..100_000] + "\n... [Truncated]";
            }
            return content;
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }
}
