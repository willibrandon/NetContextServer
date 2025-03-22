namespace NetContextServer.Models;

/// <summary>
/// Represents the analysis results of a NuGet package in the project, including usage, version, and security information.
/// </summary>
public class PackageAnalysis
{
    /// <summary>
    /// Gets or sets the NuGet package identifier.
    /// </summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current version of the package installed in the project.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the package is actively used in the project.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a newer version of the package is available.
    /// </summary>
    public bool HasUpdate { get; set; }

    /// <summary>
    /// Gets or sets the latest available version of the package, if an update is available.
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package has known security vulnerabilities.
    /// </summary>
    public bool HasSecurityIssues { get; set; }

    /// <summary>
    /// Gets or sets the list of file paths where the package is referenced or used.
    /// </summary>
    public List<string> UsageLocations { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of packages that depend on this package.
    /// </summary>
    public List<string> TransitiveDependencies { get; set; } = [];

    /// <summary>
    /// Gets or sets the recommended action to take regarding this package (e.g., update, remove, etc.).
    /// </summary>
    public string? RecommendedAction { get; set; }
}
