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
        private static readonly HashSet<string> DefaultIgnorePatterns = new(StringComparer.OrdinalIgnoreCase) 
        { 
            "*.env",
            "appsettings.*.json",
            "*.pfx",
            "*.key",
            "*.pem",
            "*password*",
            "*secret*"
        };
        private static HashSet<string> UserIgnorePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ignore_patterns.json");

        // Static constructor to initialize the base directory and load state
        static NetConextServer()
        {
            BaseDirectory = Directory.GetCurrentDirectory();
            LoadState();
        }

        private static bool IsValidGlobPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return false;

            // Basic glob pattern validation
            // Disallow patterns that start with / or \
            if (pattern.StartsWith("/") || pattern.StartsWith("\\"))
                return false;

            // Check for invalid characters
            var invalidChars = Path.GetInvalidFileNameChars()
                .Where(c => c != '*' && c != '?' && c != '[' && c != ']')
                .ToArray();
            
            if (pattern.Any(c => invalidChars.Contains(c)))
                return false;

            // Ensure balanced square brackets if used
            var openBrackets = pattern.Count(c => c == '[');
            var closeBrackets = pattern.Count(c => c == ']');
            if (openBrackets != closeBrackets)
                return false;

            return true;
        }

        /// <summary>
        /// Get the location of the state file
        /// </summary>
        [McpFunction("get_state_file_location", "Get the location of the ignore patterns state file")]
        public static string GetStateFileLocation()
        {
            return JsonSerializer.Serialize(new { StateFilePath = StateFile });
        }

        private static void SaveState()
        {
            var state = new { UserPatterns = UserIgnorePatterns.ToArray() };
            File.WriteAllText(StateFile, JsonSerializer.Serialize(state));
        }

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
        public static string AddIgnorePatterns([McpParameter(true)] string[] patterns)
        {
            var invalidPatterns = new List<string>();
            var validPatterns = new List<string>();

            foreach (var pattern in patterns)
            {
                if (IsValidGlobPattern(pattern))
                {
                    UserIgnorePatterns.Add(pattern);
                    validPatterns.Add(pattern);
                }
                else
                {
                    invalidPatterns.Add(pattern);
                }
            }

            SaveState();
            var allPatterns = GetAllPatterns().ToArray();
            return JsonSerializer.Serialize(new
            {
                DefaultPatterns = DefaultIgnorePatterns.ToArray(),
                UserPatterns = UserIgnorePatterns.ToArray(),
                AllPatterns = allPatterns,
                ValidPatternsAdded = validPatterns.ToArray(),
                InvalidPatterns = invalidPatterns.ToArray()
            });
        }

        /// <summary>
        /// Get current ignore patterns
        /// </summary>
        [McpFunction("get_ignore_patterns", "Get current ignore patterns")]
        public static string GetIgnorePatterns()
        {
            LoadState(); // Reload state to ensure we have latest patterns
            var allPatterns = GetAllPatterns().ToArray();
            return JsonSerializer.Serialize(new
            {
                DefaultPatterns = DefaultIgnorePatterns.ToArray(),
                UserPatterns = UserIgnorePatterns.ToArray(),
                AllPatterns = allPatterns
            });
        }

        /// <summary>
        /// Clear all ignore patterns
        /// </summary>
        [McpFunction("clear_ignore_patterns", "Clear all ignore patterns")]
        public static string ClearIgnorePatterns()
        {
            UserIgnorePatterns.Clear();
            SaveState();
            var allPatterns = GetAllPatterns().ToArray();
            return JsonSerializer.Serialize(new
            {
                DefaultPatterns = DefaultIgnorePatterns.ToArray(),
                UserPatterns = UserIgnorePatterns.ToArray(),
                AllPatterns = allPatterns
            });
        }

        /// <summary>
        /// Remove specific ignore patterns
        /// </summary>
        [McpFunction("remove_ignore_patterns", "Remove specific ignore patterns")]
        public static string RemoveIgnorePatterns([McpParameter(true)] string[] patterns)
        {
            var removedPatterns = new List<string>();
            var notFoundPatterns = new List<string>();
            var defaultPatterns = new List<string>();

            foreach (var pattern in patterns)
            {
                if (DefaultIgnorePatterns.Contains(pattern))
                {
                    defaultPatterns.Add(pattern);
                }
                else if (UserIgnorePatterns.Remove(pattern))
                {
                    removedPatterns.Add(pattern);
                }
                else
                {
                    notFoundPatterns.Add(pattern);
                }
            }

            SaveState();
            var allPatterns = GetAllPatterns().ToArray();
            return JsonSerializer.Serialize(new
            {
                DefaultPatterns = DefaultIgnorePatterns.ToArray(),
                UserPatterns = UserIgnorePatterns.ToArray(),
                AllPatterns = allPatterns,
                RemovedPatterns = removedPatterns.ToArray(),
                NotFoundPatterns = notFoundPatterns.ToArray(),
                DefaultPatternsSkipped = defaultPatterns.ToArray()
            });
        }

        private static IEnumerable<string> GetAllPatterns()
        {
            return DefaultIgnorePatterns.Concat(UserIgnorePatterns).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsPathSafe(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var fullPath = Path.GetFullPath(path);
            var basePath = Path.GetFullPath(BaseDirectory);
            return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldIgnoreFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return GetAllPatterns().Any(pattern =>
            {
                if (pattern.Contains("*"))
                {
                    var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                    return Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase);
                }
                return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
            });
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

    internal class UserPatternsState
    {
        public string[] UserPatterns { get; set; } = [];
    }
}