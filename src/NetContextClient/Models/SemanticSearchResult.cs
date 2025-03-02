namespace NetContextClient.Models;

/// <summary>
/// Model for an individual semantic search result
/// </summary>
public class SemanticSearchResult
{
    public string FilePath { get; set; } = string.Empty;

    public int StartLine { get; set; }

    public int EndLine { get; set; }

    public string Content { get; set; } = string.Empty;

    public float Score { get; set; }

    public string ParentScope { get; set; } = string.Empty;
} 