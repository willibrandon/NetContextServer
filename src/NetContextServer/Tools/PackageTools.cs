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
        WriteIndented = true
    };

    /// <summary>
    /// Analyzes NuGet packages in all projects found in the base directory.
    /// </summary>
    /// <returns>
    /// A JSON string containing analysis results for each project, including:
    /// - Package versions and available updates
    /// - Usage analysis
    /// - Recommendations for updates or removal
    /// </returns>
    /// <remarks>
    /// This operation requires the base directory to be set and contain valid .NET projects.
    /// The analysis includes checking for updates, detecting package usage, and providing recommendations.
    /// </remarks>
    [McpTool("analyze_packages")]
    [Description("Analyzes NuGet packages in all projects found in the base directory.")]
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
                return JsonSerializer.Serialize(new { Message = "No project files found in the base directory." }, DefaultJsonOptions);
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
            return JsonSerializer.Serialize(new { Error = ex.Message }, DefaultJsonOptions);
        }
    }
}
