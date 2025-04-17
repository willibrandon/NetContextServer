using System.Text.Json.Serialization;

namespace NetContextClient.Models;

/// <summary>
/// Response model for version information.
/// </summary>
public class VersionResponse
{
    /// <summary>
    /// Gets the current version of NetContextServer.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current .NET runtime version.
    /// </summary>
    [JsonPropertyName("runtimeVersion")]
    public string RuntimeVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether semantic search is configured and available.
    /// </summary>
    [JsonPropertyName("semanticSearchEnabled")]
    public bool SemanticSearchEnabled { get; init; }

    /// <summary>
    /// Gets the current operating system.
    /// </summary>
    [JsonPropertyName("operatingSystem")]
    public string OperatingSystem { get; init; } = string.Empty;

    /// <summary>
    /// Gets active configuration settings.
    /// </summary>
    [JsonPropertyName("activeConfiguration")]
    public Dictionary<string, string> ActiveConfiguration { get; init; } = new();
} 