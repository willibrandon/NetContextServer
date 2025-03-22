namespace NetContextClient.Models;

/// <summary>
/// Response model for semantic search results
/// </summary>
public class SemanticSearchResponse
{
    /// <summary>
    /// Gets or sets the array of semantic search results ordered by relevance.
    /// </summary>
    public SemanticSearchResult[] Results { get; set; } = [];
}
