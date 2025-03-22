namespace NetContextClient.Models;

/// <summary>
/// Response model for adding ignore patterns
/// </summary>
public class AddIgnorePatternsResponse : IgnorePatternsResponse
{
    /// <summary>
    /// Gets or sets the array of glob patterns that were successfully added to the ignore list.
    /// </summary>
    public string[] ValidPatternsAdded { get; set; } = [];

    /// <summary>
    /// Gets or sets the array of patterns that were rejected due to invalid glob syntax or other validation errors.
    /// </summary>
    public string[] InvalidPatterns { get; set; } = [];
} 