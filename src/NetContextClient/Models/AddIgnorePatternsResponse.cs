namespace NetContextClient.Models;

/// <summary>
/// Response model for adding ignore patterns
/// </summary>
public class AddIgnorePatternsResponse : IgnorePatternsResponse
{
    public string[] ValidPatternsAdded { get; set; } = [];
    public string[] InvalidPatterns { get; set; } = [];
} 