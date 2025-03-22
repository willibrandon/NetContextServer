namespace NetContextServer.Models;

/// <summary>
/// Response model for ignore pattern operations containing default, user-defined, and combined patterns.
/// </summary>
public class IgnorePatternsResponse
{
    /// <summary>
    /// Gets or sets the default ignore patterns that are always active.
    /// </summary>
    public string[] DefaultPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the user-defined ignore patterns.
    /// </summary>
    public string[] UserPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets all active ignore patterns (both default and user-defined).
    /// </summary>
    public string[] AllPatterns { get; set; } = [];
} 