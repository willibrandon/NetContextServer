namespace NetContextServer.Models;

public class PackageAnalysis
{
    public string PackageId { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public bool IsUsed { get; set; }

    public bool HasUpdate { get; set; }

    public string? LatestVersion { get; set; }

    public bool HasSecurityIssues { get; set; }

    public List<string> UsageLocations { get; set; } = [];

    public List<string> TransitiveDependencies { get; set; } = [];

    public string? RecommendedAction { get; set; }
}
