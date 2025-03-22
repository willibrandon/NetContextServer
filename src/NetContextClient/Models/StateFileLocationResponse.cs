namespace NetContextClient.Models;

/// <summary>
/// Response model for state file location
/// </summary>
public class StateFileLocationResponse
{
    /// <summary>
    /// Gets or sets the absolute path to the state file used for storing persistent configuration.
    /// </summary>
    public string StateFilePath { get; set; } = string.Empty;
}
