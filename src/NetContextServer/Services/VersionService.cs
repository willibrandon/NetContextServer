using NetContextServer.Models;

namespace NetContextServer.Services;

/// <summary>
/// Service for retrieving version and configuration information about the NetContextServer.
/// </summary>
public static class VersionService
{
    /// <summary>
    /// Gets the current version information for NetContextServer.
    /// </summary>
    /// <returns>A VersionInfo object containing current version and configuration details.</returns>
    public static VersionInfo GetVersionInfo()
    {
        return new VersionInfo
        {
            SemanticSearchEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")) &&
                                  !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")),
            ActiveConfiguration = new Dictionary<string, string>
            {
                ["BaseDirectory"] = FileValidationService.BaseDirectory ?? "Not set",
                ["HasIgnorePatterns"] = IgnorePatternService.HasUserDefinedPatterns().ToString(),
                ["StateFileLocation"] = IgnorePatternService.GetStateFileLocation()
            }
        };
    }
} 