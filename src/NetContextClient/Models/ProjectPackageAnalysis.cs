namespace NetContextClient.Models;

/// <summary>
/// Model for package analysis results grouped by project
/// </summary>
public class ProjectPackageAnalysis
{
    /// <summary>
    /// Gets or sets the path to the project file being analyzed.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of package analysis results for this project.
    /// </summary>
    public List<PackageAnalysis> Packages { get; set; } = [];
} 