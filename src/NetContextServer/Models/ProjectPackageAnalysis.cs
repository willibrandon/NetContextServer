namespace NetContextServer.Models;

/// <summary>
/// Represents package analysis results grouped by project, containing project path and its package analysis details.
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