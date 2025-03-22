using ModelContextProtocol;
using ModelContextProtocol.Server;
using NetContextServer.Models;
using NetContextServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NetContextServer.Tools;

[McpToolType]
public static class PackageTools
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true
    };

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