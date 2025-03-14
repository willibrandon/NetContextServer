namespace NetContextClient.Models;

/// <summary>
/// Model for package analysis results grouped by project
/// </summary>
public class ProjectPackageAnalysis
{
    public string ProjectPath { get; set; } = string.Empty;

    public List<PackageAnalysis> Packages { get; set; } = [];
} 