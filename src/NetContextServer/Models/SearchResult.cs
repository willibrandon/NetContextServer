namespace NetContextServer.Models;

public class SearchResult
{
    public string FilePath { get; set; } = string.Empty;

    public int StartLine { get; set; }

    public int EndLine { get; set; }

    public string Content { get; set; } = string.Empty;

    public double Score { get; set; }

    public string ParentScope { get; set; } = string.Empty;
}
