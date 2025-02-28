using MCPSharp;
using NetContextServer.Models;
using NetContextServer.Services;
using System.Text.Json;

await MCPServer.StartAsync("NetContextServer", "1.0");

namespace NetContextServer
{
    /// <summary>
    ///  NetContextServer
    /// </summary>
    [McpTool]
    public class NetContextServer
    {
        private static string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();

        private static HashSet<string> UserIgnorePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ignore_patterns.json");

        // Static constructor to initialize the base directory and load state
        static NetContextServer()
        {
            BaseDirectory = Directory.GetCurrentDirectory();
            LoadState();
        }

        /// <summary>
        /// Get the location of the state file
        /// </summary>
        [McpFunction("get_state_file_location", "Get the location of the ignore patterns state file")]
        public static string GetStateFileLocation() => IgnorePatternService.GetStateFileLocation();

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

        /// <summary>
        /// Add patterns to ignore when scanning files
        /// </summary>
        [McpFunction("add_ignore_patterns", "Add patterns to ignore when scanning files")]
        public static string AddIgnorePatterns([McpParameter(true)] string[] patterns) => IgnorePatternService.AddIgnorePatterns(patterns);

        /// <summary>
        /// Clear all ignore patterns
        /// </summary>
        [McpFunction("clear_ignore_patterns", "Clear all ignore patterns")]
        public static string ClearIgnorePatterns() => IgnorePatternService.ClearIgnorePatterns();

        /// <summary>
        /// throws an exception - for ensuring we handle them gracefully
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [McpFunction("throw_exception", "throw_exception description")]
        public static string Exception() => throw new Exception("This is an exception");

        /// <summary>
        /// Get current ignore patterns
        /// </summary>
        [McpFunction("get_ignore_patterns", "Get current ignore patterns")]
        public static string GetIgnorePatterns() => IgnorePatternService.GetIgnorePatterns();

        ///<summary>just returns a message for testing.</summary>
        [McpFunction("hello", "hello description")]
        public static string Hello() => "hello, claude.";

        ///<summary>Lists all .cs files in a project directory</summary>
        ///<param name="projectPath">Path to the project directory</param>
        [McpFunction("list_files", "Lists all .NET source files in the specified project")]
        public static string ListFiles([McpParameter(true)] string projectPath) => JsonSerializer.Serialize(FileService.ListFiles(projectPath));

        ///<summary>Lists all projects in the solution</summary>
        [McpFunction("list_projects", "Lists all .csproj files in the solution")]
        public static string ListProjects() => JsonSerializer.Serialize(FileService.ListProjects());

        /// <summary>
        /// Get all project files in a solution directory
        /// </summary>
        [McpFunction("list_projects_in_dir", "Lists all .csproj files in a directory")]
        public static string ListProjectsInDirectory([McpParameter(true)] string directory) => JsonSerializer.Serialize(FileService.ListProjectsInDirectory(directory));

        /// <summary>
        /// Get all solution files in the base directory
        /// </summary>
        [McpFunction("list_solutions", "Lists all .sln files in the base directory")]
        public static string ListSolutions() => JsonSerializer.Serialize(FileService.ListSolutions());

        /// <summary>
        /// Get all source files in a project directory
        /// </summary>
        [McpFunction("list_source_files", "Lists all source files in a project directory")]
        public static string ListSourceFiles([McpParameter(true)] string projectDir) => JsonSerializer.Serialize(FileService.ListSourceFiles(projectDir));

        ///<summary>Read contents of a file</summary>
        ///<param name="filePath">Path to the file to read</param>
        [McpFunction("open_file", "Read contents of a specified file")]
        public static string OpenFile([McpParameter(true)] string filePath) => FileService.OpenFile(filePath);

        /// <summary>
        /// Remove specific ignore patterns
        /// </summary>
        [McpFunction("remove_ignore_patterns", "Remove specific ignore patterns")]
        public static string RemoveIgnorePatterns([McpParameter(true)] string[] patterns) => IgnorePatternService.RemoveIgnorePatterns(patterns);

        ///<summary>Search for code containing the specified text</summary>
        ///<param name="searchText">Text to search for</param>
        [McpFunction("search_code", "Search for code containing the specified text")]
        public static string SearchCode([McpParameter(true)] string searchText) => JsonSerializer.Serialize(CodeSearchService.SearchCode(searchText));

        /// <summary>
        /// Search code using semantic similarity
        /// </summary>
        [McpFunction("semantic_search", "Search code using semantic similarity")]
        public static async Task<string> SemanticSearchAsync(
            [McpParameter(true)] string query,
            [McpParameter(false)] int? topK = 5)
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

        // Method to set base directory (for testing)
        [McpFunction("set_base_directory", "set_base_directory description")]
        public static string SetBaseDirectory([McpParameter(true)] string directory)
        {
            if (!Directory.Exists(directory))
                return JsonSerializer.Serialize(new[] { "Error: Directory not found" });

            FileValidationService.SetBaseDirectory(directory);
            return JsonSerializer.Serialize(new[] { $"Base directory set to: {directory}" });
        }
    }
}
