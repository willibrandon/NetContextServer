using MCPSharp;
using System.Text.Json;
using System.Text.RegularExpressions;

await MCPServer.StartAsync("NetConextServer", "1.0");

namespace NetContextServer
{
    /// <summary>
    ///  NetConextServer
    /// </summary>
    [McpTool]
    public class NetConextServer
    {
        private static string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();
        private static HashSet<string> IgnorePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase) 
        { 
            "*.env",
            "appsettings.*.json",
            "*.pfx",
            "*.key",
            "*.pem",
            "*password*",
            "*secret*"
        };

        // Static constructor to initialize the base directory
        static NetConextServer()
        {
            BaseDirectory = Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Add patterns to ignore when scanning files
        /// </summary>
        [McpFunction("add_ignore_patterns", "Add patterns to ignore when scanning files")]
        public static string AddIgnorePatterns([McpParameter(true)] string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                IgnorePatterns.Add(pattern);
            }
            return JsonSerializer.Serialize(IgnorePatterns.ToArray());
        }

        /// <summary>
        /// Get current ignore patterns
        /// </summary>
        [McpFunction("get_ignore_patterns", "Get current ignore patterns")]
        public static string GetIgnorePatterns()
        {
            return JsonSerializer.Serialize(IgnorePatterns.ToArray());
        }

        /// <summary>
        /// Clear all ignore patterns
        /// </summary>
        [McpFunction("clear_ignore_patterns", "Clear all ignore patterns")]
        public static string ClearIgnorePatterns()
        {
            IgnorePatterns.Clear();
            return JsonSerializer.Serialize(IgnorePatterns.ToArray());
        }

        private static bool IsPathSafe(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var basePath = Path.GetFullPath(BaseDirectory);
                return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldIgnoreFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            foreach (var pattern in IgnorePatterns)
            {
                if (pattern.Contains("*"))
                {
                    var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                    if (Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase))
                        return true;
                }
                else if (fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // Method to set base directory (for testing)
        [McpFunction("set_base_directory", "set_base_directory description")]
        public static string SetBaseDirectory([McpParameter(true)] string directory)
        {
            if (!Directory.Exists(directory))
                return JsonSerializer.Serialize(new[] { "Error: Directory not found" });

            BaseDirectory = directory;
            return JsonSerializer.Serialize(new[] { $"Base directory set to: {directory}" });
        }

        ///<summary>just returns a message for testing.</summary>
        [McpFunction("hello", "hello description")]
        public static string Hello() => "hello, claude.";

        ///<summary>returns ths input string back</summary>
        ///<param name="input">the string to echo</param>
        [McpFunction("echo", "echo description")]
        public static string Echo([McpParameter(true)] string input) => input;

        ///<summary>Add Two Numbers</summary>
        ///<param name="a">first number</param>
        ///<param name="b">second number</param>
        [McpFunction("add", "add description")]
        public static string Add(int a, int b) => (a + b).ToString();

        /// <summary>
        /// Adds a complex object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [McpFunction("add_complex", "add_complex description")]
        public static string AddComplex(ComplicatedObject obj) => $"Name: {obj.Name}, Age: {obj.Age}, Hobbies: {string.Join(", ", obj.Hobbies)}";

        /// <summary>
        /// throws an exception - for ensuring we handle them gracefully
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [McpFunction("throw_exception", "throw_exception description")]
        public static string Exception() => throw new Exception("This is an exception");

        ///<summary>Lists all projects in the solution</summary>
        [McpFunction("list_projects", "Lists all .csproj files in the solution")]
        public static string ListProjects()
        {
            var projects = Directory.GetFiles(BaseDirectory, "*.csproj", SearchOption.AllDirectories)
                .Where(p => !ShouldIgnoreFile(p))
                .ToArray();
            return JsonSerializer.Serialize(projects);
        }

        ///<summary>Lists all .cs files in a project directory</summary>
        ///<param name="projectPath">Path to the project directory</param>
        [McpFunction("list_files", "Lists all .cs files in the specified project")]
        public static string ListFiles([McpParameter(true)] string projectPath)
        {
            if (!Directory.Exists(projectPath))
                return JsonSerializer.Serialize(new[] { "Error: Directory not found" });

            if (!IsPathSafe(projectPath))
                return JsonSerializer.Serialize(new[] { "Error: Access to this directory is not allowed" });

            var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Where(f => !ShouldIgnoreFile(f))
                .ToArray();
            return JsonSerializer.Serialize(files);
        }

        ///<summary>Search for code containing the specified text</summary>
        ///<param name="searchText">Text to search for</param>
        [McpFunction("search_code", "Search for code containing the specified text")]
        public static string SearchCode([McpParameter(true)] string searchText)
        {
            var results = new List<string>();
            var csFiles = Directory.GetFiles(BaseDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(f => !ShouldIgnoreFile(f));
            
            foreach (var file in csFiles)
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add($"{file}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
            
            return JsonSerializer.Serialize(results);
        }

        ///<summary>Read contents of a file</summary>
        ///<param name="filePath">Path to the file to read</param>
        [McpFunction("open_file", "Read contents of a specified file")]
        public static string OpenFile([McpParameter(true)] string filePath)
        {
            if (!File.Exists(filePath))
                return "Error: File not found";

            if (!IsPathSafe(filePath))
                return "Error: Access to this file is not allowed";

            if (ShouldIgnoreFile(filePath))
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

        /// <summary>
        /// Get all solution files in the base directory
        /// </summary>
        [McpFunction("list_solutions", "Lists all .sln files in the base directory")]
        public static string ListSolutions()
        {
            var solutions = Directory.GetFiles(BaseDirectory, "*.sln", SearchOption.AllDirectories);
            return JsonSerializer.Serialize(solutions);
        }

        /// <summary>
        /// Get all project files in a solution directory
        /// </summary>
        [McpFunction("list_projects_in_dir", "Lists all .csproj files in a directory")]
        public static string ListProjectsInDirectory([McpParameter(true)] string directory)
        {
            if (!Directory.Exists(directory))
                return JsonSerializer.Serialize(new[] { "Error: Directory not found" });

            if (!IsPathSafe(directory))
                return JsonSerializer.Serialize(new[] { "Error: Access to this directory is not allowed" });

            var projects = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories)
                .Where(p => !ShouldIgnoreFile(p))
                .ToArray();
            return JsonSerializer.Serialize(projects);
        }

        /// <summary>
        /// Get all source files in a project directory
        /// </summary>
        [McpFunction("list_source_files", "Lists all source files in a project directory")]
        public static string ListSourceFiles([McpParameter(true)] string projectDir)
        {
            if (!Directory.Exists(projectDir))
                return JsonSerializer.Serialize(new[] { "Error: Directory not found" });

            if (!IsPathSafe(projectDir))
                return JsonSerializer.Serialize(new[] { "Error: Access to this directory is not allowed" });

            var sourceFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(projectDir, "*.vb", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(projectDir, "*.fs", SearchOption.AllDirectories))
                .Where(f => !ShouldIgnoreFile(f))
                .ToArray();

            return JsonSerializer.Serialize(sourceFiles);
        }
    }

    /// <summary>
    /// A complicated object
    /// </summary>
    public class ComplicatedObject
    {
        /// <summary>The name of the object</summary>
        public string Name { get; set; } = "";
        /// <summary>The age of the object</summary>
        public int Age { get; set; } = 0;
        /// <summary>The hobbies of the object</summary>
        public string[] Hobbies { get; set; } = [];
    }
}