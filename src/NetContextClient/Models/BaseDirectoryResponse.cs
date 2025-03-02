namespace NetContextClient.Models;

/// <summary>
/// Response model for the get_base_directory function
/// </summary>
public class BaseDirectoryResponse
{
    /// <summary>
    /// The current base directory path
    /// </summary>
    public string BaseDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the base directory exists
    /// </summary>
    public bool Exists { get; set; }
} 