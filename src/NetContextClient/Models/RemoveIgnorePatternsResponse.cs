namespace NetContextClient.Models;

/// <summary>
/// Response model for removing ignore patterns
/// </summary>
public class RemoveIgnorePatternsResponse : IgnorePatternsResponse
{
    /// <summary>
    /// Gets or sets the array of patterns that were successfully removed from the ignore list.
    /// </summary>
    public string[] RemovedPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the array of patterns that were not found in the ignore list.
    /// </summary>
    public string[] NotFoundPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the array of default patterns that could not be removed as they are system-defined.
    /// </summary>
    public string[] DefaultPatternsSkipped { get; set; } = [];
} 