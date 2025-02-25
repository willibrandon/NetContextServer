using MCPSharp;
using NetContextServer.Core.Models;

namespace NetContextServer.Tool.Mcp;

/// <summary>
/// Provides MCP tools for working with source files in a .NET solution.
/// </summary>
[McpTool("file_tools", "Tools for working with source files in a .NET solution")]
public class FileTools
{
    private ProjectIndex? _index;
    private const int MaxFileSize = 100_000; // Maximum file size to return (characters)

    /// <summary>
    /// Initializes the FileTools with the specified project index.
    /// </summary>
    /// <param name="index">The project index containing information about projects and their files.</param>
    public void Initialize(ProjectIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    /// <summary>
    /// Represents a code search result.
    /// </summary>
    public class CodeSearchResult
    {
        /// <summary>
        /// Gets or sets the file path where the match was found.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the line number where the match was found (1-based).
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the text of the line containing the match.
        /// </summary>
        public string LineText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Opens a file and returns its contents.
    /// </summary>
    /// <param name="filePath">The path to the file to open.</param>
    /// <returns>The contents of the file, potentially truncated if too large.</returns>
    [McpFunction("open_file", "Opens a file and returns its contents")]
    public string OpenFile([McpParameter(true, "The path to the file to open")] string filePath)
    {
        EnsureInitialized();
        
        // Basic safety check
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // Verify the file is within the solution directory
        if (!IsFileInSolutionDirectory(filePath))
            throw new UnauthorizedAccessException($"Access to file outside solution directory is not allowed: {filePath}");

        // Read and potentially truncate the file
        var content = File.ReadAllText(filePath);
        if (content.Length > MaxFileSize)
        {
            content = content[..MaxFileSize] + "\n... [Truncated]";
        }
        return content;
    }

    /// <summary>
    /// Searches for a keyword in all source files.
    /// </summary>
    /// <param name="keyword">The keyword to search for.</param>
    /// <returns>A list of search results containing file paths, line numbers, and matching lines.</returns>
    [McpFunction("search_code", "Searches for a keyword in all source files")]
    public List<CodeSearchResult> SearchCode([McpParameter(true, "The keyword to search for")] string keyword)
    {
        EnsureInitialized();
        
        if (string.IsNullOrWhiteSpace(keyword))
            throw new ArgumentException("Search keyword cannot be empty", nameof(keyword));

        var results = new List<CodeSearchResult>();
        var allSourceFiles = _index!.FilesByProject.Values.SelectMany(files => files).ToList();

        foreach (var file in allSourceFiles)
        {
            try
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new CodeSearchResult
                        {
                            FilePath = file,
                            LineNumber = i + 1,
                            LineText = lines[i].Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with other files
                Console.Error.WriteLine($"Error searching file {file}: {ex.Message}");
            }
        }

        return results;
    }

    private void EnsureInitialized()
    {
        if (_index == null)
        {
            throw new InvalidOperationException("FileTools has not been initialized. Call Initialize() first.");
        }
    }

    private bool IsFileInSolutionDirectory(string filePath)
    {
        EnsureInitialized();
        var solutionRoot = _index!.SolutionRoot;
        return Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(solutionRoot));
    }
} 