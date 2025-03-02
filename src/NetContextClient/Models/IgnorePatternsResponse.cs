namespace NetContextClient.Models;

/// <summary>
/// Response model for ignore patterns operations
/// </summary>
public class IgnorePatternsResponse
{
    public string[] DefaultPatterns { get; set; } = [];
    public string[] UserPatterns { get; set; } = [];
    public string[] AllPatterns { get; set; } = [];
} 