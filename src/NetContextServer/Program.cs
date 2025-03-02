using MCPSharp;
using NetContextServer.Models;
using NetContextServer.Services;
using System.Text.Json;

await MCPServer.StartAsync("NetContextServer", "1.0");

namespace NetContextServer
{
    /// <summary>
    /// Core server implementation for the .NET Context Provider (MCP) service.
    /// </summary>
    /// <remarks>
    /// This class provides a collection of tools for analyzing and managing .NET codebases:
    /// - Package analysis and dependency management
    /// - File and directory operations
    /// - Code search capabilities (both text and semantic)
    /// - Ignore pattern management for file scanning
    /// 
    /// The server maintains state for ignore patterns and base directory configuration.
    /// All file operations are performed relative to a configured base directory.
    /// </remarks>
    [McpTool]
    public class NetContextServer
    {
        private static string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();

        private static HashSet<string> UserIgnorePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ignore_patterns.json");

        /// <summary>
        /// Initializes the NetContextServer static instance.
        /// </summary>
        /// <remarks>
        /// Performs two critical initialization steps:
        /// 1. Sets the base directory to the current working directory
        /// 2. Loads any previously saved ignore patterns from the state file
        /// 
        /// If the state file cannot be loaded, initializes with an empty set of ignore patterns.
        /// </remarks>
        static NetContextServer()
        {
            BaseDirectory = Directory.GetCurrentDirectory();
            LoadState();
        }

        /// <summary>
        /// Adds new patterns to the file scanning ignore list.
        /// </summary>
        /// <param name="patterns">Array of glob patterns to ignore (e.g. ['*.generated.cs', 'bin/*'])</param>
        /// <returns>JSON confirmation message indicating which patterns were added</returns>
        /// <remarks>
        /// Patterns are case-insensitive and are persisted to the state file.
        /// Changes affect both in-memory state and persistent storage.
        /// </remarks>
        [McpFunction("add_ignore_patterns",
            "Adds new patterns to the ignore list for file scanning operations. Patterns are case-insensitive and persisted to the " +
            "state file. Returns a JSON confirmation message of patterns added.")]
        public static string AddIgnorePatterns([McpParameter(
            required: true,
            description: "Array of glob patterns to ignore (e.g. ['*.generated.cs', 'bin/*'])")] string[] patterns) =>
            IgnorePatternService.AddIgnorePatterns(patterns);

        /// <summary>
        /// Analyzes NuGet packages in all projects found in the base directory.
        /// </summary>
        /// <returns>JSON array of package analyses including usage locations, versions, and update recommendations</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the base directory is not set</exception>
        [McpFunction("analyze_packages",
            "Analyzes NuGet packages in all projects found in the base directory. Returns detailed information about package usage, versions, and update " +
            "recommendations.")]
        public static async Task<string> AnalyzePackagesAsync()
        {
            try
            {
                FileValidationService.EnsureBaseDirectorySet();
                var baseDir = FileValidationService.BaseDirectory;
                
                // Find all .csproj files in the base directory
                var projectFiles = Directory.GetFiles(baseDir, "*.csproj", SearchOption.AllDirectories);
                
                if (projectFiles.Length == 0)
                {
                    return JsonSerializer.Serialize(new { Message = "No project files found in the base directory." });
                }
                
                var analyzer = new PackageAnalyzerService(baseDir);
                var allAnalyses = new List<ProjectPackageAnalysis>();
                
                foreach (var projectFile in projectFiles)
                {
                    var packages = await analyzer.GetPackageReferencesAsync(projectFile);
                    var analyses = new List<PackageAnalysis>();
                    
                    foreach (var package in packages)
                    {
                        var analysis = await analyzer.AnalyzePackageAsync(package);
                        analyses.Add(analysis);
                    }
                    
                    allAnalyses.Add(new ProjectPackageAnalysis
                    {
                        ProjectPath = FileValidationService.GetRelativePath(projectFile),
                        Packages = analyses
                    });
                }
                
                return JsonSerializer.Serialize(allAnalyses, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Removes all user-defined ignore patterns from the system.
        /// </summary>
        /// <returns>JSON confirmation message indicating patterns were cleared</returns>
        /// <remarks>
        /// This operation affects both the in-memory patterns and the persistent storage.
        /// Cannot be undone without re-adding patterns.
        /// </remarks>
        [McpFunction("clear_ignore_patterns",
            "Removes all user-defined ignore patterns from both memory and the persistent state file. Returns a JSON confirmation " +
            "message of patterns cleared.")]
        public static string ClearIgnorePatterns() => IgnorePatternService.ClearIgnorePatterns();

        /// <summary>
        /// Retrieves the current list of active ignore patterns.
        /// </summary>
        /// <returns>JSON array of currently active ignore patterns</returns>
        /// <remarks>
        /// Returns patterns from in-memory state, which should match the persisted state.
        /// Patterns are used to exclude files from various scanning operations.
        /// </remarks>
        [McpFunction("get_ignore_patterns",
            "Retrieves the current list of active ignore patterns from memory. These patterns are used to exclude files from scanning " +
            "operations. Returns a JSON array of patterns.")]
        public static string GetIgnorePatterns() => IgnorePatternService.GetIgnorePatterns();

        /// <summary>
        /// Retrieves the location of the ignore patterns state file.
        /// </summary>
        /// <returns>Absolute path to the ignore_patterns.json file in the application's base directory</returns>
        [McpFunction("get_state_file_location",
            "Returns the absolute path to the ignore_patterns.json state file which stores user-defined ignore patterns. This file is " +
            "located in the application's base directory.")]
        public static string GetStateFileLocation() => IgnorePatternService.GetStateFileLocation();

        /// <summary>
        /// Simple health check endpoint that verifies basic functionality.
        /// </summary>
        /// <returns>A greeting message indicating the service is operational</returns>
        /// <remarks>
        /// Used for testing connectivity and basic functionality.
        /// Always returns the same static message.
        /// </remarks>
        [McpFunction("hello",
            "Simple health check endpoint that returns a greeting message. Used for testing connectivity and basic functionality.")]
        public static string Hello() => "hello, claude.";

        /// <summary>
        /// Lists all .NET source files in the specified project directory.
        /// </summary>
        /// <param name="projectPath">Absolute path to the project directory containing the .cs files</param>
        /// <returns>JSON array of file paths relative to the project root</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the project directory cannot be found</exception>
        [McpFunction("list_files",
            "Lists all .NET source files (.cs) in the specified project directory. Returns a JSON array of file paths relative to " +
            "the project root.")]
        public static string ListFiles([McpParameter(
            required: true,
            description: "Absolute path to the project directory containing the .cs files")] string projectPath) =>
            JsonSerializer.Serialize(FileService.ListFiles(projectPath));

        /// <summary>
        /// Scans the current solution for all project files.
        /// </summary>
        /// <returns>JSON array of .csproj file paths found in the solution</returns>
        /// <remarks>
        /// Requires the base directory to be set to a valid solution directory.
        /// Only searches within the current solution scope.
        /// </remarks>
        [McpFunction("list_projects",
            "Scans the current solution and returns a JSON array of all .csproj files found. Useful for identifying all projects in " +
            "the current solution.")]
        public static string ListProjects() => JsonSerializer.Serialize(FileService.ListProjects());

        /// <summary>
        /// Searches a specific directory for project files.
        /// </summary>
        /// <param name="directory">Absolute path to the directory to search for .csproj files</param>
        /// <returns>JSON array of .csproj file paths found in the specified directory</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory cannot be found</exception>
        /// <remarks>
        /// Unlike list_projects, this targets a specific directory rather than the whole solution.
        /// </remarks>
        [McpFunction("list_projects_in_dir",
            "Searches a specific directory for .csproj files and returns them as a JSON array. Unlike list_projects, this targets a " +
            "specific directory rather than the whole solution.")]
        public static string ListProjectsInDirectory([McpParameter(
            required: true,
            description: "Absolute path to the directory to search for .csproj files")] string directory) => 
            JsonSerializer.Serialize(FileService.ListProjectsInDirectory(directory));

        /// <summary>
        /// Lists all solution files in the base directory.
        /// </summary>
        /// <returns>JSON array of .sln file paths found in the base directory</returns>
        /// <remarks>
        /// Requires the base directory to be set.
        /// Useful for identifying solution files in the workspace.
        /// </remarks>
        [McpFunction("list_solutions",
            "Returns a JSON array of all .sln files found in the base directory. Useful for identifying solution files in the " +
            "workspace.")]
        public static string ListSolutions() => JsonSerializer.Serialize(FileService.ListSolutions());

        /// <summary>
        /// Lists all source files in a project directory.
        /// </summary>
        /// <param name="projectDir">Absolute path to the project directory to scan for source files</param>
        /// <returns>JSON array of file paths relative to the project directory</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the project directory cannot be found</exception>
        /// <remarks>
        /// Includes all source files, not just .cs files.
        /// Paths in the response are relative to the project directory.
        /// </remarks>
        [McpFunction("list_source_files",
            "Lists all source files (not just .cs files) in a project directory. Returns a JSON array of file paths relative to the " +
            "project directory.")]
        public static string ListSourceFiles([McpParameter(
            required: true,
            description: "Absolute path to the project directory to scan for source files")] string projectDir) => 
            JsonSerializer.Serialize(FileService.ListSourceFiles(projectDir));

        /// <summary>
        /// Reads and returns the contents of a specified file.
        /// </summary>
        /// <param name="filePath">Absolute path to the file to read</param>
        /// <returns>String containing the entire contents of the file</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
        /// <remarks>
        /// Performs validation to ensure the file exists and is accessible.
        /// </remarks>
        [McpFunction("open_file",
            "Reads and returns the contents of a specified file as a string. Performs validation to ensure the file exists and is " +
            "accessible.")]
        public static string OpenFile([McpParameter(
            required: true,
            description: "Absolute path to the file to read")] string filePath) => 
            FileService.OpenFile(filePath);

        /// <summary>
        /// Removes specific patterns from the ignore list.
        /// </summary>
        /// <param name="patterns">Array of glob patterns to remove from the ignore list</param>
        /// <returns>JSON confirmation message indicating which patterns were removed</returns>
        /// <remarks>
        /// Patterns are case-insensitive.
        /// Updates both in-memory state and persistent storage.
        /// </remarks>
        [McpFunction("remove_ignore_patterns",
            "Removes specific patterns from the ignore list. Patterns are case-insensitive. Updates both memory and persistent " +
            "storage. Returns a JSON confirmation message.")]
        public static string RemoveIgnorePatterns([McpParameter(
            required: true,
            description: "Array of glob patterns to remove from the ignore list")] string[] patterns) => 
            IgnorePatternService.RemoveIgnorePatterns(patterns);

        /// <summary>
        /// Performs a text-based search across all code files.
        /// </summary>
        /// <param name="searchText">The exact text string to search for in the codebase</param>
        /// <returns>JSON array of matches with file locations and context</returns>
        /// <remarks>
        /// Searches are case-sensitive and exact match only.
        /// Results include surrounding context for each match.
        /// </remarks>
        [McpFunction("search_code",
            "Performs a text-based search across all code files for the specified text. Returns JSON array of matches with file " +
            "locations and context.")]
        public static string SearchCode([McpParameter(
            required: true,
            description: "The exact text string to search for in the codebase")] string searchText) => 
            JsonSerializer.Serialize(CodeSearchService.SearchCode(searchText));

        /// <summary>
        /// Performs a semantic similarity search across the codebase.
        /// </summary>
        /// <param name="query">Natural language description of the code you're looking for</param>
        /// <param name="topK">Optional: Number of results to return (default: 5)</param>
        /// <returns>JSON array of semantically similar code matches</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the base directory is not set</exception>
        /// <remarks>
        /// Uses AI to find code that matches the meaning of the query, not just exact text matches.
        /// Results are ranked by semantic similarity to the query.
        /// </remarks>
        [McpFunction("semantic_search",
            "Performs a semantic similarity search across the codebase. Uses AI to find code that matches the meaning of the query, " +
            "not just exact text matches. Returns JSON array of matches.")]
        public static async Task<string> SemanticSearchAsync(
            [McpParameter(
                required: true,
                description: "Natural language description of the code you're looking for")] string query,
            [McpParameter(
                required: false,
                description: "Optional: Number of results to return (default: 5)")] int? topK = 5)
        {
            try
            {
                FileValidationService.EnsureBaseDirectorySet();
                return await CodeSearchService.SemanticSearchAsync(query, topK ?? 5);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Sets the base directory for all file operations.
        /// </summary>
        /// <param name="directory">Absolute path to set as the new base directory. Must be a valid, existing directory</param>
        /// <returns>JSON confirmation message indicating the directory was set</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist</exception>
        /// <remarks>
        /// This directory serves as the root for relative path calculations and file searches.
        /// Must be set before performing most file operations.
        /// </remarks>
        [McpFunction("set_base_directory",
            "Sets the base directory for all file operations. This directory serves as the root for relative path calculations and " +
            "file searches. Returns JSON confirmation message.")]
        public static string SetBaseDirectory([McpParameter(
            required: true,
            description: "Absolute path to set as the new base directory. Must be a valid, existing directory")] string directory)
        {
            if (!Directory.Exists(directory))
                return JsonSerializer.Serialize(new[] { "Error: Directory not found" });

            FileValidationService.SetBaseDirectory(directory);
            return JsonSerializer.Serialize(new[] { $"Base directory set to: {directory}" });
        }

        /// <summary>
        /// Loads ignore patterns from the persistent state file.
        /// </summary>
        /// <remarks>
        /// Attempts to deserialize ignore patterns from the state file located at <see cref="StateFile"/>.
        /// If the file exists, patterns are loaded into memory with case-insensitive comparison.
        /// If the file doesn't exist or is invalid, initializes an empty set of patterns.
        /// 
        /// The state file is expected to contain a JSON serialized <see cref="UserPatternsState"/> object.
        /// </remarks>
        private static void LoadState()
        {
            try
            {
                if (File.Exists(StateFile))
                {
                    var state = JsonSerializer.Deserialize<UserPatternsState>(File.ReadAllText(StateFile));
                    UserIgnorePatterns = new HashSet<string>(state!.UserPatterns, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch
            {
                UserIgnorePatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
