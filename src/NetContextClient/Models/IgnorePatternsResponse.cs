namespace NetContextClient.Models;

/// <summary>
/// Response model for ignore patterns operations
/// </summary>
public class IgnorePatternsResponse
{
    /// <summary>
    /// Gets or sets the built-in default glob patterns used for file exclusion.
    /// </summary>
    public string[] DefaultPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the user-defined glob patterns used for file exclusion.
    /// </summary>
    public string[] UserPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the combined list of both default and user-defined glob patterns.
    /// </summary>
    public string[] AllPatterns { get; set; } = [];
} 