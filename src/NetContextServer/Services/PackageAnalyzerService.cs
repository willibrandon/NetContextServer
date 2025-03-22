using NetContextServer.Models;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Xml.Linq;

namespace NetContextServer.Services;

/// <summary>
/// Provides functionality for analyzing NuGet package references in .NET projects,
/// including version checking, usage analysis, and update recommendations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the PackageAnalyzerService class.
/// </remarks>
/// <param name="baseDirectory">The base directory for package analysis. If null, uses the current directory.</param>
public class PackageAnalyzerService(string? baseDirectory = null)
{
    private static readonly SourceRepository NuGetRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
    private static readonly SourceCacheContext Cache = new();
    private readonly string _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();

    /// <summary>
    /// Gets all package references from a specified project file.
    /// </summary>
    /// <param name="projectPath">The path to the project file (.csproj).</param>
    /// <returns>A list of PackageReference objects containing package information.</returns>
    public static async Task<List<PackageReference>> GetPackageReferencesAsync(string projectPath)
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

    /// <summary>
    /// Analyzes a package reference to determine its usage, available updates, and provides recommendations.
    /// </summary>
    /// <param name="package">The package reference to analyze.</param>
    /// <returns>A PackageAnalysis object containing the analysis results.</returns>
    /// <remarks>
    /// The analysis includes:
    /// - Checking for available updates
    /// - Detecting package usage in source files
    /// - Generating recommendations based on usage and updates
    /// </remarks>
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
            
            // Resolve transitive dependencies
            try
            {
                var dependencyResource = await NuGetRepository.GetResourceAsync<DependencyInfoResource>();
                var packageDependencyInfo = await dependencyResource.ResolvePackage(
                    new NuGet.Packaging.Core.PackageIdentity(package.Id, currentVersion),
                    NuGet.Frameworks.NuGetFramework.ParseFolder("net6.0"),
                    Cache,
                    NullLogger.Instance,
                    CancellationToken.None);
                
                if (packageDependencyInfo != null)
                {
                    var dependencies = new List<string>();
                    var visited = new HashSet<string>();
                    
                    // Start with the direct dependencies
                    foreach (var dependency in packageDependencyInfo.Dependencies)
                    {
                        if (!visited.Contains(dependency.Id))
                        {
                            visited.Add(dependency.Id);
                            dependencies.Add(dependency.Id);
                            
                            // Recursively gather deeper dependencies
                            await GatherDependenciesForPackageAsync(
                                dependency.Id, 
                                dependency.VersionRange.MinVersion ?? NuGetVersion.Parse("0.0.1"), 
                                dependencies, 
                                visited, 
                                1, 
                                3);
                        }
                    }
                    
                    analysis.TransitiveDependencies = dependencies;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving dependencies for {package.Id}: {ex.Message}");
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
                        var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                        
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

    private async Task GatherDependenciesForPackageAsync(string packageId, NuGetVersion packageVersion, List<string> dependencies, HashSet<string> visited, int currentDepth, int maxDepth)
    {
        if (currentDepth > maxDepth) return;
        if (visited.Contains(packageId)) return;
        visited.Add(packageId);

        dependencies.Add(packageId);

        try
        {
            var dependencyResource = await NuGetRepository.GetResourceAsync<DependencyInfoResource>();
            var dependencyInfo = await dependencyResource.ResolvePackage(
                new NuGet.Packaging.Core.PackageIdentity(packageId, packageVersion),
                NuGet.Frameworks.NuGetFramework.ParseFolder("net6.0"),
                Cache,
                NullLogger.Instance,
                CancellationToken.None);
            
            if (dependencyInfo != null)
            {
                foreach (var childDependency in dependencyInfo.Dependencies)
                {
                    if (!visited.Contains(childDependency.Id))
                    {
                        var minVersion = childDependency.VersionRange.MinVersion ?? NuGetVersion.Parse("0.0.1");
                        await GatherDependenciesForPackageAsync(
                            childDependency.Id, 
                            minVersion, 
                            dependencies, 
                            visited, 
                            currentDepth + 1, 
                            maxDepth);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error gathering dependencies for {packageId}: {ex.Message}");
        }
    }
}
