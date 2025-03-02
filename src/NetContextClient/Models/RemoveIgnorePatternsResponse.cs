namespace NetContextClient.Models;

/// <summary>
/// Response model for removing ignore patterns
/// </summary>
public class RemoveIgnorePatternsResponse : IgnorePatternsResponse
{
    public string[] RemovedPatterns { get; set; } = [];
    public string[] NotFoundPatterns { get; set; } = [];
    public string[] DefaultPatternsSkipped { get; set; } = [];
} 