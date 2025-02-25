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
        private static SemanticSearch _semanticSearch = new();
        private static bool _isIndexed = false;

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
        public static string[] GetIgnorePatterns()
        {
            LoadState();
            return UserIgnorePatterns.ToArray();
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

        private static void EnsureBaseDirectorySet()
        {
            if (string.IsNullOrEmpty(BaseDirectory))
            {
                throw new InvalidOperationException("Base directory not set. Use set_base_directory first.");
            }
        }

        private static async Task EnsureIndexedAsync()
        {
            if (!_isIndexed)
            {
                if (_semanticSearch == null)
                {
                    _semanticSearch = new SemanticSearch();
                }
                var files = Directory.GetFiles(BaseDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".cs") || f.EndsWith(".fs") || f.EndsWith(".vb"));
                await _semanticSearch.IndexFilesAsync(files);
                _isIndexed = true;
            }
        }

        private static string GetRelativePath(string fullPath)
        {
            return Path.GetRelativePath(BaseDirectory, fullPath);
        }

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
                EnsureBaseDirectorySet();
                await EnsureIndexedAsync();

                var results = await _semanticSearch.SearchAsync(query, topK ?? 5);
                
                return JsonSerializer.Serialize(new
                {
                    Results = results.Select(r => new
                    {
                        FilePath = GetRelativePath(r.Snippet.FilePath),
                        StartLine = r.Snippet.StartLine,
                        EndLine = r.Snippet.EndLine,
                        Content = FormatCodeContent(r.Snippet.Content),
                        Score = Math.Round(r.Score * 100, 1), // Convert to percentage
                        ParentScope = GetParentScope(r.Snippet.Content)
                    })
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Error = ex.Message });
            }
        }

        private static string FormatCodeContent(string content)
        {
            // First, remove all blank lines completely
            var lines = content.Split('\n');
            var nonBlankLines = new List<string>();
            
            // Track if we've seen important structural elements
            bool hasOpeningBrace = content.Contains("{");
            
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    nonBlankLines.Add(line);
                }
            }
            
            // If there are no non-blank lines, return the original content
            if (nonBlankLines.Count == 0)
                return content.Trim();
                
            // If we don't have an opening brace but have a class/method declaration,
            // try to find and include the opening brace from the original content
            if (!hasOpeningBrace)
            {
                var firstLine = nonBlankLines[0].Trim();
                if (firstLine.Contains("class ") || 
                    firstLine.Contains("interface ") ||
                    firstLine.Contains("struct ") ||
                    firstLine.Contains("enum ") ||
                    (firstLine.Contains("void ") && firstLine.Contains("(")) ||
                    (firstLine.Contains("public ") && firstLine.Contains("(")) ||
                    (firstLine.Contains("private ") && firstLine.Contains("(")) ||
                    (firstLine.Contains("protected ") && firstLine.Contains("(")) ||
                    (firstLine.Contains("internal ") && firstLine.Contains("(")))
                {
                    // Find the line with the opening brace in the original content
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("{"))
                        {
                            nonBlankLines.Add(lines[i]);
                            hasOpeningBrace = true;
                            break;
                        }
                    }
                    
                    // If we still don't have an opening brace, add one
                    if (!hasOpeningBrace)
                    {
                        nonBlankLines.Add("{");
                        nonBlankLines.Add("    // Content truncated");
                        nonBlankLines.Add("}");
                    }
                }
            }
            
            // Now add strategic blank lines only where needed for readability
            var result = new List<string>();
            bool addedBlankLine = false;
            
            for (int i = 0; i < nonBlankLines.Count; i++)
            {
                var line = nonBlankLines[i];
                var trimmedLine = line.TrimStart();
                
                // Add a blank line before class, method, or control structure definitions
                // but only if we haven't just added a blank line
                bool isSignificantLine = 
                    trimmedLine.Contains("class ") || 
                    trimmedLine.Contains("interface ") ||
                    trimmedLine.Contains("struct ") ||
                    trimmedLine.Contains("enum ") ||
                    (trimmedLine.Contains("void ") && trimmedLine.Contains("(")) ||
                    (trimmedLine.Contains("public ") && trimmedLine.Contains("(")) ||
                    (trimmedLine.Contains("private ") && trimmedLine.Contains("(")) ||
                    (trimmedLine.Contains("protected ") && trimmedLine.Contains("(")) ||
                    (trimmedLine.Contains("internal ") && trimmedLine.Contains("("));
                
                if (isSignificantLine && !addedBlankLine && i > 0)
                {
                    result.Add("");
                    addedBlankLine = true;
                }
                else
                {
                    addedBlankLine = false;
                }
                
                result.Add(line);
            }
            
            // Ensure we don't have more than 1/4 blank lines
            int totalLines = result.Count;
            int blankLines = result.Count(l => string.IsNullOrWhiteSpace(l));
            int maxAllowedBlankLines = totalLines / 4; // 25% rule
            
            if (blankLines > maxAllowedBlankLines)
            {
                // Remove some blank lines to meet the threshold
                var finalResult = new List<string>();
                int blankLinesToKeep = maxAllowedBlankLines;
                
                foreach (var line in result)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (blankLinesToKeep > 0)
                        {
                            finalResult.Add(line);
                            blankLinesToKeep--;
                        }
                        // Skip this blank line if we've reached our quota
                    }
                    else
                    {
                        finalResult.Add(line);
                    }
                }
                
                return string.Join("\n", finalResult).Trim();
            }
            
            return string.Join("\n", result).Trim();
        }

        private static string GetParentScope(string content)
        {
            List<string> scopeParts = new List<string>();
            string[] lines = content.Split('\n');

            foreach (string line in lines)
            {
                string trimmed = line.TrimStart();
                
                // Detect namespace
                if (trimmed.StartsWith("namespace "))
                {
                    string namespaceName = ExtractName(trimmed, "namespace");
                    if (!string.IsNullOrEmpty(namespaceName))
                    {
                        scopeParts.Add(namespaceName);
                    }
                }
                // Detect class, interface, struct, enum
                else if (trimmed.Contains("class "))
                {
                    string className = ExtractName(trimmed, "class");
                    if (!string.IsNullOrEmpty(className))
                    {
                        scopeParts.Add(className);
                    }
                }
                else if (trimmed.Contains("interface "))
                {
                    string interfaceName = ExtractName(trimmed, "interface");
                    if (!string.IsNullOrEmpty(interfaceName))
                    {
                        scopeParts.Add(interfaceName);
                    }
                }
                else if (trimmed.Contains("struct "))
                {
                    string structName = ExtractName(trimmed, "struct");
                    if (!string.IsNullOrEmpty(structName))
                    {
                        scopeParts.Add(structName);
                    }
                }
                else if (trimmed.Contains("enum "))
                {
                    string enumName = ExtractName(trimmed, "enum");
                    if (!string.IsNullOrEmpty(enumName))
                    {
                        scopeParts.Add(enumName);
                    }
                }
                // Detect methods
                else if ((trimmed.Contains("void ") || 
                         trimmed.Contains("async ") || 
                         trimmed.Contains("Task ") ||
                         trimmed.Contains("public ") || 
                         trimmed.Contains("private ") || 
                         trimmed.Contains("protected ") ||
                         trimmed.Contains("internal ")) &&
                         trimmed.Contains("(") && 
                         !trimmed.StartsWith("//") && 
                         !trimmed.StartsWith("/*"))
                {
                    string methodName = ExtractMethodName(trimmed);
                    if (!string.IsNullOrEmpty(methodName))
                    {
                        scopeParts.Add(methodName);
                    }
                }
            }

            return string.Join(".", scopeParts);
        }
        
        private static string ExtractName(string line, string keyword)
        {
            int keywordIndex = line.IndexOf(keyword + " ");
            if (keywordIndex < 0)
                return string.Empty;
                
            string afterKeyword = line.Substring(keywordIndex + keyword.Length + 1).Trim();
            
            // Find the end of the name (at first space, opening brace, colon, or parenthesis)
            int endIndex = afterKeyword.Length;
            int spaceIndex = afterKeyword.IndexOf(' ');
            int braceIndex = afterKeyword.IndexOf('{');
            int colonIndex = afterKeyword.IndexOf(':');
            int parenIndex = afterKeyword.IndexOf('(');
            
            if (spaceIndex >= 0 && spaceIndex < endIndex) endIndex = spaceIndex;
            if (braceIndex >= 0 && braceIndex < endIndex) endIndex = braceIndex;
            if (colonIndex >= 0 && colonIndex < endIndex) endIndex = colonIndex;
            if (parenIndex >= 0 && parenIndex < endIndex) endIndex = parenIndex;
            
            if (endIndex > 0)
            {
                return afterKeyword.Substring(0, endIndex).Trim();
            }
            
            return string.Empty;
        }
        
        private static string ExtractMethodName(string line)
        {
            // Extract the method name from the line (text before the opening parenthesis)
            int parenIndex = line.IndexOf('(');
            if (parenIndex <= 0)
                return string.Empty;
                
            string beforeParen = line.Substring(0, parenIndex).Trim();
            
            // Find the last space before the parenthesis
            int lastSpaceIndex = beforeParen.LastIndexOf(' ');
            if (lastSpaceIndex >= 0 && lastSpaceIndex < beforeParen.Length - 1)
            {
                // Return everything after the last space
                return beforeParen.Substring(lastSpaceIndex + 1).Trim();
            }
            
            return string.Empty;
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