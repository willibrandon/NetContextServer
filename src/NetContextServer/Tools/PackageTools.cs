using ModelContextProtocol.Server;
using NetContextServer.Models;
using NetContextServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NetContextServer.Tools;

/// <summary>
/// Provides MCP tools for analyzing NuGet packages in .NET projects.
/// </summary>
[McpToolType]
public static class PackageTools
{
    /// <summary>
    /// Default JSON serializer options used for package analysis output.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Analyzes NuGet packages in all projects found in the base directory.
    /// </summary>
    /// <returns>
    /// A JSON string containing analysis results for each project, including:
    /// - Package versions and available updates
    /// - Usage analysis
    /// - Recommendations for updates or removal
    /// - Dependency graph visualization for each package
    /// </returns>
    /// <remarks>
    /// This operation requires the base directory to be set and contain valid .NET projects.
    /// The analysis includes checking for updates, detecting package usage, providing recommendations,
    /// and visualizing transitive dependency graphs.
    /// </remarks>
    [McpTool("analyze_packages")]
    [Description("Analyzes NuGet packages in all projects found in the base directory, including deep transitive dependencies.")]
    public static async Task<string> AnalyzePackagesAsync()
    {
        try
        {
            FileValidationService.EnsureBaseDirectorySet();
            var baseDir = FileValidationService.BaseDirectory;
            
            // Find all .csproj files in the base directory
            var projectFiles = Directory.GetFiles(baseDir, "*.csproj", SearchOption.AllDirectories);
            
            if (projectFiles.Length == 0)
            {
                return JsonSerializer.Serialize(new { message = "No project files found in the base directory." }, DefaultJsonOptions);
            }
            
            var analyzer = new PackageAnalyzerService(baseDir);
            var allAnalyses = new List<ProjectPackageAnalysis>();
            
            foreach (var projectFile in projectFiles)
            {
                var packages = await PackageAnalyzerService.GetPackageReferencesAsync(projectFile);
                var analyses = new List<PackageAnalysis>();
                
                foreach (var package in packages)
                {
                    var analysis = await analyzer.AnalyzePackageAsync(package);
                    
                    // Add dependency graph visualization
                    if (analysis.TransitiveDependencies != null && analysis.TransitiveDependencies.Count > 0)
                    {
                        analysis.DependencyGraph = GenerateDependencyGraph(analysis.PackageId, analysis.TransitiveDependencies);
                    }
                    
                    analyses.Add(analysis);
                }
                
                allAnalyses.Add(new ProjectPackageAnalysis
                {
                    ProjectPath = FileValidationService.GetRelativePath(projectFile),
                    Packages = analyses
                });
            }
            
            return JsonSerializer.Serialize(allAnalyses, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }
    
    /// <summary>
    /// Generates a visual representation of the dependency graph as ASCII art.
    /// </summary>
    /// <param name="rootPackageId">The root package ID.</param>
    /// <param name="dependencies">The list of dependency package IDs.</param>
    /// <returns>A string containing the ASCII visualization of the dependency graph.</returns>
    private static string GenerateDependencyGraph(string rootPackageId, List<string> dependencies)
    {
        if (dependencies.Count == 0)
        {
            return "No dependencies";
        }
        
        var lines = new List<string>();
        lines.Add($"└─ {rootPackageId}");
        
        // Group dependencies for visualization
        var grouped = dependencies
            .GroupBy(d => d.Split('.').FirstOrDefault() ?? "")
            .OrderBy(g => g.Key)
            .ToList();
        
        for (int i = 0; i < grouped.Count; i++)
        {
            var group = grouped[i];
            bool isLastGroup = i == grouped.Count - 1;
            string groupPrefix = isLastGroup ? "   └─ " : "   ├─ ";
            
            // Add the group header (if it's a meaningful group)
            if (!string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            {
                lines.Add($"{groupPrefix}{group.Key}.*");
                
                // Add group members
                string memberPrefix = isLastGroup ? "      " : "   │  ";
                foreach (var dependency in group.OrderBy(d => d))
                {
                    if (dependency != group.Key + ".*")
                    {
                        lines.Add($"{memberPrefix}└─ {dependency}");
                    }
                }
            }
            else
            {
                // Just add the single dependency
                foreach (var dependency in group)
                {
                    lines.Add($"{groupPrefix}{dependency}");
                }
            }
        }
        
        return string.Join(Environment.NewLine, lines);
    }
}
