using MCPSharp;
using System.Text.Json;

await MCPServer.StartAsync("TestServer", "1.0");

namespace MCPSharp.Example
{
    ///<summary>testing interface for custom .net mcp server</summary>
    [McpTool]
    public class MCPDev
    {
        private static string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();

        // Static constructor to initialize the base directory
        static MCPDev()
        {
            BaseDirectory = Directory.GetCurrentDirectory();
        }

        // Method to set base directory (for testing)
        public static void SetBaseDirectory(string directory)
        {
            BaseDirectory = directory;
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
            var projects = Directory.GetFiles(BaseDirectory, "*.csproj", SearchOption.AllDirectories);
            return JsonSerializer.Serialize(projects);
        }

        ///<summary>Lists all .cs files in a project directory</summary>
        ///<param name="projectPath">Path to the project directory</param>
        [McpFunction("list_files", "Lists all .cs files in the specified project")]
        public static string ListFiles([McpParameter(true)] string projectPath)
        {
            if (!Directory.Exists(projectPath))
                return JsonSerializer.Serialize(new[] { "Error: Directory not found" });

            var files = Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*.cs", SearchOption.AllDirectories);
            return JsonSerializer.Serialize(files);
        }

        ///<summary>Search for code containing the specified text</summary>
        ///<param name="searchText">Text to search for</param>
        [McpFunction("search_code", "Search for code containing the specified text")]
        public static string SearchCode([McpParameter(true)] string searchText)
        {
            var results = new List<string>();
            var csFiles = Directory.GetFiles(BaseDirectory, "*.cs", SearchOption.AllDirectories);
            
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