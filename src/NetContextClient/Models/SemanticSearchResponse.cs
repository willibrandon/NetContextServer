namespace NetContextClient.Models;

/// <summary>
/// Response model for semantic search results
/// </summary>
public class SemanticSearchResponse
{
    public SemanticSearchResult[] Results { get; set; } = [];
} 