namespace NetContextServer.Models;

public class ProjectPackageAnalysis
{
    public string ProjectPath { get; set; } = string.Empty;
    
    public List<PackageAnalysis> Packages { get; set; } = [];
} 