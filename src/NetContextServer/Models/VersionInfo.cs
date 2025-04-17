using System.Text.Json.Serialization;

namespace NetContextServer.Models;

/// <summary>
/// Represents version and configuration information for the NetContextServer.
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// Gets the current version of NetContextServer.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = typeof(VersionInfo).Assembly.GetName().Version?.ToString() ?? "unknown";

    /// <summary>
    /// Gets the current .NET runtime version.
    /// </summary>
    [JsonPropertyName("runtimeVersion")]
    public string RuntimeVersion { get; init; } = Environment.Version.ToString();

    /// <summary>
    /// Gets whether semantic search is configured and available.
    /// </summary>
    [JsonPropertyName("semanticSearchEnabled")]
    public bool SemanticSearchEnabled { get; init; }

    /// <summary>
    /// Gets the current operating system.
    /// </summary>
    [JsonPropertyName("operatingSystem")]
    public string OperatingSystem { get; init; } = Environment.OSVersion.ToString();

    /// <summary>
    /// Gets active configuration settings.
    /// </summary>
    [JsonPropertyName("activeConfiguration")]
    public Dictionary<string, string> ActiveConfiguration { get; init; } = new();
} 