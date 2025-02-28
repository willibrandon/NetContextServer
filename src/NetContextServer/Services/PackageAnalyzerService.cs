using NetContextServer.Models;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Xml.Linq;

namespace NetContextServer.Services;

public class PackageAnalyzerService(string? baseDirectory = null)
{
    private static readonly SourceRepository NuGetRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
    private static readonly SourceCacheContext Cache = new();
    private readonly string _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();

    public async Task<List<PackageReference>> GetPackageReferencesAsync(string projectPath)
    {
        var packages = new List<PackageReference>();
        
        if (!File.Exists(projectPath)) return packages;
        
        var projectContent = await File.ReadAllTextAsync(projectPath);
        
        if (projectPath.EndsWith(".csproj"))
        {
            var doc = XDocument.Parse(projectContent);
            var packageRefs = doc.Descendants("PackageReference");
            
            foreach (var package in packageRefs)
            {
                packages.Add(new PackageReference
                {
                    Id = package.Attribute("Include")?.Value ?? string.Empty,
                    Version = package.Attribute("Version")?.Value ?? string.Empty,
                    ProjectPath = projectPath
                });
            }
        }
        
        return packages;
    }

    public async Task<PackageAnalysis> AnalyzePackageAsync(PackageReference package)
    {
        var analysis = new PackageAnalysis
        {
            PackageId = package.Id,
            Version = package.Version
        };

        try
        {
            // Check for updates
            if (!NuGetVersion.TryParse(package.Version, out var currentVersion))
            {
                analysis.RecommendedAction = $"Error: Invalid package version format '{package.Version}'";
                return analysis;
            }

            var resource = await NuGetRepository.GetResourceAsync<FindPackageByIdResource>();
            var versions = await resource.GetAllVersionsAsync(package.Id, Cache, NullLogger.Instance, CancellationToken.None);
            
            if (versions.Any())
            {
                var latestVersion = versions.Max();
                if (latestVersion != null)
                {
                    analysis.HasUpdate = latestVersion > currentVersion;
                    analysis.LatestVersion = latestVersion.ToString();
                }
            }

            // Check usage
            try
            {
                var projectDir = Path.GetDirectoryName(package.ProjectPath) ?? _baseDirectory;
                var sourceFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                    .ToArray();
                
                foreach (var file in sourceFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        
                        // Enhanced usage detection with proper line splitting
                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();
                            if (trimmedLine.StartsWith("using") || trimmedLine.StartsWith("[assembly:"))
                            {
                                if (trimmedLine.Contains($"{package.Id};", StringComparison.OrdinalIgnoreCase) ||
                                    trimmedLine.Contains($"{package.Id}.", StringComparison.OrdinalIgnoreCase))
                                {
                                    analysis.IsUsed = true;
                                    analysis.UsageLocations.Add(file);
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directory: {ex.Message}");
            }

            // Generate recommendations
            if (string.IsNullOrEmpty(analysis.RecommendedAction))
            {
                if (!analysis.IsUsed)
                {
                    analysis.RecommendedAction = "Consider removing this unused package";
                }
                else if (analysis.HasUpdate)
                {
                    analysis.RecommendedAction = $"Update available: {analysis.LatestVersion}";
                }
            }
        }
        catch (Exception ex)
        {
            analysis.RecommendedAction = $"Error analyzing package: {ex.Message}";
        }

        return analysis;
    }
}
