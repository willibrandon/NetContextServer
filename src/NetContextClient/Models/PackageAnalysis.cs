namespace NetContextClient.Models;

/// <summary>
/// Model for an individual package analysis result
/// </summary>
public class PackageAnalysis
{
    public string PackageId { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsUsed { get; set; }
    public List<string> UsageLocations { get; set; } = new List<string>();
    public string? LatestVersion { get; set; }
    public bool HasSecurityIssues { get; set; }
    public string? RecommendedAction { get; set; }
    public bool HasUpdate { get; set; }
    public List<string> TransitiveDependencies { get; set; } = new List<string>();
} 