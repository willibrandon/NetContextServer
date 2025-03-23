namespace NetContextClient.Models;

/// <summary>
/// Response model for the think tool.
/// </summary>
public class ThinkResponse
{
    /// <summary>
    /// The thought that was processed.
    /// </summary>
    public string Thought { get; set; } = string.Empty;

    /// <summary>
    /// A confirmation message about the thought processing.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error message if something went wrong.
    /// </summary>
    public string? Error { get; set; }
} 