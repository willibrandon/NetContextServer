namespace NetContextServer.Models;

/// <summary>
/// Represents the persistent state of user-defined ignore patterns.
/// </summary>
internal class UserPatternsState
{
    /// <summary>
    /// Gets or sets the array of user-defined glob patterns used for file exclusion.
    /// </summary>
    public string[] UserPatterns { get; set; } = [];
}
