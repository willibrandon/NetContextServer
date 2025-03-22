namespace NetContextServer.Services;

internal static class FileService
{
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

    public static string[] ListProjects()
    {
        FileValidationService.EnsureBaseDirectorySet();
        return [.. Directory.GetFiles(FileValidationService.BaseDirectory, "*.csproj", SearchOption.AllDirectories).Where(p => !IgnorePatternService.ShouldIgnoreFile(p))];
    }

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

    public static string[] ListSolutions()
    {
        FileValidationService.EnsureBaseDirectorySet();
        return Directory.GetFiles(FileValidationService.BaseDirectory, "*.sln", SearchOption.AllDirectories);
    }

    public static string[] ListProjectsInDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return ["Error: Directory not found"];

        if (!FileValidationService.IsPathSafe(directory))
            return ["Error: Access to this directory is not allowed"];

        return [.. Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories).Where(p => !IgnorePatternService.ShouldIgnoreFile(p))];
    }

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
