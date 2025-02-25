namespace NetContextServer.Core.Models;

/// <summary>
/// Represents an index of .NET projects and their source files.
/// </summary>
public class ProjectIndex
{
    /// <summary>
    /// Gets or sets the paths to project files (.csproj, .vbproj, etc.).
    /// </summary>
    public List<string> ProjectPaths { get; set; } = [];

    /// <summary>
    /// Gets or sets a dictionary mapping project paths to their source files.
    /// </summary>
    public Dictionary<string, List<string>> FilesByProject { get; set; } = [];

    /// <summary>
    /// Gets the root directory of the solution.
    /// </summary>
    public string SolutionRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets the path to the solution file (.sln).
    /// </summary>
    public string? SolutionPath { get; set; }
} 