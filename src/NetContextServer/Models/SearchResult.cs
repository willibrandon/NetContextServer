namespace NetContextServer.Models;

/// <summary>
/// Represents a search result from a code search operation, containing the matched content and its metadata.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Gets or sets the file path where the search result was found.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the starting line number of the search result in the file (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Gets or sets the ending line number of the search result in the file (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Gets or sets the actual content/text of the search result.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relevance score of the search result, where higher values indicate better matches.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the parent scope (e.g., namespace, class, or method) containing this search result.
    /// </summary>
    public string ParentScope { get; set; } = string.Empty;
}
