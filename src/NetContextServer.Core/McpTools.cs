using MCPSharp;
using System.IO;

namespace NetContextServer.Core;

/// <summary>
/// Provides MCP tools for interacting with .NET codebases.
/// </summary>
[McpTool]
public class McpTools
{
    private readonly ProjectIndex _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpTools"/> class.
    /// </summary>
    /// <param name="index">The project index to use for the tools.</param>
    public McpTools(ProjectIndex index)
    {
        _index = index;
    }

    /// <summary>
    /// Lists all projects in the solution.
    /// </summary>
    /// <returns>A list of project file paths.</returns>
    [McpFunction("ListProjects", "Lists all projects in the solution.")]
    public List<string> ListProjects()
    {
        return _index.ProjectPaths;
    }

    /// <summary>
    /// Lists all source files in a specific project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>A list of source file paths.</returns>
    [McpFunction("ListFiles", "Lists all source files in a specific project.")]
    public List<string> ListFiles([McpParameter(true)] string projectPath)
    {
        return _index.FilesByProject.TryGetValue(projectPath, out var files)
            ? files
            : new List<string>();
    }

    /// <summary>
    /// Opens a file and returns its contents.
    /// </summary>
    /// <param name="filePath">The path to the file to open.</param>
    /// <returns>The contents of the file.</returns>
    [McpFunction("OpenFile", "Opens a file and returns its contents.")]
    public string OpenFile([McpParameter(true)] string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        // Read the file and optionally limit its size
        var content = File.ReadAllText(filePath);
        if (content.Length > 100_000)
        {
            content = content.Substring(0, 100_000) + "\n... [Truncated]";
        }
        return content;
    }

    /// <summary>
    /// Searches for a keyword in all source files.
    /// </summary>
    /// <param name="keyword">The keyword to search for.</param>
    /// <returns>A list of search results.</returns>
    [McpFunction("SearchCode", "Searches for a keyword in all source files.")]
    public List<CodeSearchResult> SearchCode([McpParameter(true)] string keyword)
    {
        var results = new List<CodeSearchResult>();
        foreach (var kvp in _index.FilesByProject)
        {
            foreach (var file in kvp.Value)
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
                                LineText = lines[i]
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other files
                    Console.Error.WriteLine($"Error processing file {file}: {ex.Message}");
                }
            }
        }
        return results;
    }
}

/// <summary>
/// Represents a search result from the SearchCode tool.
/// </summary>
public class CodeSearchResult
{
    /// <summary>
    /// Gets or sets the path to the file containing the match.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number of the match (1-based).
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the text of the line containing the match.
    /// </summary>
    public string LineText { get; set; } = string.Empty;
} 