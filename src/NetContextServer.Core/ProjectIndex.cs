namespace NetContextServer.Core;

/// <summary>
/// Represents an index of .NET projects and their associated files.
/// </summary>
public class ProjectIndex
{
    /// <summary>
    /// Gets or sets the list of project file paths (.csproj, .vbproj, etc.).
    /// </summary>
    public List<string> ProjectPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets a dictionary mapping project paths to their associated source files.
    /// </summary>
    public Dictionary<string, List<string>> FilesByProject { get; set; } = new();

    /// <summary>
    /// Gets the root path of the solution being indexed.
    /// </summary>
    public string SolutionRoot { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectIndex"/> class.
    /// </summary>
    /// <param name="solutionRoot">The root path of the solution being indexed.</param>
    public ProjectIndex(string solutionRoot)
    {
        SolutionRoot = solutionRoot;
    }
} 