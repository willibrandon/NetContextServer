using System.Collections.Generic;

namespace NetContextClient.Models;

/// <summary>
/// Model for an individual package analysis result
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
    /// Gets or sets the list of file paths where the package is referenced or used.
    /// </summary>
    public List<string> UsageLocations { get; set; } = [];

    /// <summary>
    /// Gets or sets the latest available version of the package, if an update is available.
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package has known security vulnerabilities.
    /// </summary>
    public bool HasSecurityIssues { get; set; }

    /// <summary>
    /// Gets or sets the recommended action to take regarding this package (e.g., update, remove, etc.).
    /// </summary>
    public string? RecommendedAction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a newer version of the package is available.
    /// </summary>
    public bool HasUpdate { get; set; }

    /// <summary>
    /// Gets or sets the list of packages that depend on this package.
    /// </summary>
    public List<string> TransitiveDependencies { get; set; } = [];
    
    /// <summary>
    /// Gets or sets a visual representation of the dependency graph as ASCII art.
    /// </summary>
    public string? DependencyGraph { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the package is used implicitly
    /// (e.g., test frameworks, build tools, analyzers) without direct code references.
    /// </summary>
    public bool ImplicitUsage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the latest available version is a preview/prerelease version.
    /// </summary>
    public bool IsPreviewVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a newer preview version is available (when not including preview versions in regular updates).
    /// </summary>
    public bool HasPreviewUpdate { get; set; }

    /// <summary>
    /// Gets or sets the latest available preview version of the package, if a preview update is available.
    /// </summary>
    public string? LatestPreviewVersion { get; set; }
} 