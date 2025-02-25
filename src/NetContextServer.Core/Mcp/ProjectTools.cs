using MCPSharp;
using NetContextServer.Core.Models;

namespace NetContextServer.Core.MCP;

/// <summary>
/// Provides MCP tools for working with .NET projects and solutions.
/// </summary>
[McpTool("project_tools", "Tools for working with .NET projects and solutions")]
public static class ProjectTools
{
    private static ProjectIndex? _index;

    /// <summary>
    /// Initializes the ProjectTools with the specified project index.
    /// </summary>
    /// <param name="index">The project index containing information about projects and their files.</param>
    public static void Initialize(ProjectIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    /// <summary>
    /// Lists all projects in the solution.
    /// </summary>
    /// <returns>A list of project file paths.</returns>
    [McpFunction("list_projects", "Lists all projects in the solution")]
    public static List<string> ListProjects()
    {
        EnsureInitialized();
        return _index!.ProjectPaths;
    }

    /// <summary>
    /// Lists all source files in a specific project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>A list of source file paths for the specified project.</returns>
    [McpFunction("list_files", "Lists all source files in a specific project")]
    public static List<string> ListFiles([McpParameter(true, "The path to the project file")] string projectPath)
    {
        EnsureInitialized();
        return _index!.FilesByProject.TryGetValue(projectPath, out var files)
            ? files
            : new List<string>();
    }

    /// <summary>
    /// Gets the solution path.
    /// </summary>
    /// <returns>The path to the solution file, or null if not found.</returns>
    [McpFunction("get_solution_path", "Gets the path to the solution file")]
    public static string? GetSolutionPath()
    {
        EnsureInitialized();
        return _index!.SolutionPath;
    }

    /// <summary>
    /// Gets the solution root directory.
    /// </summary>
    /// <returns>The path to the solution root directory.</returns>
    [McpFunction("get_solution_root", "Gets the path to the solution root directory")]
    public static string GetSolutionRoot()
    {
        EnsureInitialized();
        return _index!.SolutionRoot;
    }

    private static void EnsureInitialized()
    {
        if (_index == null)
        {
            throw new InvalidOperationException("ProjectTools has not been initialized. Call Initialize() first.");
        }
    }
} 