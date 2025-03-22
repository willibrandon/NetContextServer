namespace NetContextClient.Models;

/// <summary>
/// Model for an individual semantic search result
/// </summary>
public class SemanticSearchResult
{
    /// <summary>
    /// Gets or sets the file path where the search result was found.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the starting line number of the result in the file (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Gets or sets the ending line number of the result in the file (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Gets or sets the matched content from the file.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the semantic similarity score of the result (higher is better).
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Gets or sets the parent scope (e.g., namespace, class, or method) containing this result.
    /// </summary>
    public string ParentScope { get; set; } = string.Empty;
} 