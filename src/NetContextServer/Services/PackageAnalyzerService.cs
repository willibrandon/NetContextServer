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
    /// Known infrastructure packages that are used implicitly without direct code references
    /// </summary>
    private static readonly Dictionary<string, string> KnownSpecialPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Test infrastructure
        { "xunit", "Testing Framework" },
        { "xunit.runner.visualstudio", "Test Runner" },
        { "xunit.analyzers", "Test Analyzer" },
        { "xunit.extensibility.core", "Test Framework" },
        { "xunit.extensibility.execution", "Test Framework" },
        { "xunit.assert", "Test Framework" },
        { "xunit.core", "Test Framework" },
        { "Xunit.SkippableFact", "Test Framework" },
        { "NUnit", "Testing Framework" },
        { "NUnit3TestAdapter", "Test Runner" },
        { "MSTest.TestAdapter", "Test Runner" },
        { "MSTest.TestFramework", "Test Framework" },
        { "Moq", "Test Mocking" },
        { "FluentAssertions", "Test Assertions" },
        { "coverlet.collector", "Code Coverage Tool" },
        { "coverlet.msbuild", "Code Coverage Tool" },
        
        // Build and analysis tools
        { "Microsoft.NET.Test.Sdk", "Test SDK" },
        { "Microsoft.CodeAnalysis.Analyzers", "Code Analyzer" },
        { "Microsoft.CodeQuality.Analyzers", "Code Analyzer" },
        { "Microsoft.CodeAnalysis.CSharp", "Code Analyzer" },
        { "Microsoft.SourceLink", "Source Linking" },
        { "Microsoft.Build", "Build Infrastructure" },
        { "Microsoft.CodeCoverage", "Code Coverage" },
        { "Microsoft.TestPlatform", "Test Platform" },
        
        // Common infrastructure packages
        { "Microsoft.Extensions.Configuration", "Configuration Infrastructure" },
        { "Microsoft.Extensions.DependencyInjection", "DI Infrastructure" },
        { "Microsoft.Extensions.Logging", "Logging Infrastructure" }
    };

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
    /// Determines if a project is a test project based on various heuristics.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>True if the project appears to be a test project, false otherwise.</returns>
    public static bool IsTestProject(string projectPath)
    {
        // Check the project name for test indicators
        var fileName = Path.GetFileNameWithoutExtension(projectPath);
        if (fileName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if the project is in a tests directory
        var directoryPath = Path.GetDirectoryName(projectPath) ?? string.Empty;
        var directories = directoryPath.Split(Path.DirectorySeparatorChar);
        if (directories.Any(d => d.Equals("tests", StringComparison.OrdinalIgnoreCase) || 
                                 d.Equals("test", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        try
        {
            // Check project file content for test references
            var content = File.ReadAllText(projectPath);
            if (content.Contains("Microsoft.NET.Test.Sdk") ||
                content.Contains("xunit") ||
                content.Contains("NUnit") ||
                content.Contains("MSTest") ||
                content.Contains("IsTestProject") ||
                content.Contains("TestAdapter"))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading project file: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Analyzes a package reference to determine its usage, available updates, and provides recommendations.
    /// </summary>
    /// <param name="package">The package reference to analyze.</param>
    /// <param name="includePreviewVersions">Whether to include preview/prerelease versions in update recommendations.</param>
    /// <returns>A PackageAnalysis object containing the analysis results.</returns>
    /// <remarks>
    /// The analysis includes:
    /// - Checking for available updates (stable only by default)
    /// - Detecting package usage in source files
    /// - Generating recommendations based on usage and updates
    /// </remarks>
    public async Task<PackageAnalysis> AnalyzePackageAsync(PackageReference package, bool includePreviewVersions = false)
    {
        var analysis = new PackageAnalysis
        {
            PackageId = package.Id,
            Version = package.Version
        };

        try
        {
            // Check if this is a known special package
            bool isKnownSpecialPackage = KnownSpecialPackages.TryGetValue(package.Id, out string? packageCategory);
            bool isTestProject = IsTestProject(package.ProjectPath);

            // Check for updates
            if (!NuGetVersion.TryParse(package.Version, out var currentVersion))
            {
                analysis.RecommendedAction = $"Error: Invalid package version format '{package.Version}'";
                return analysis;
            }

            var resource = await NuGetRepository.GetResourceAsync<FindPackageByIdResource>();
            var allVersions = await resource.GetAllVersionsAsync(package.Id, Cache, NullLogger.Instance, CancellationToken.None);
            
            if (allVersions.Any())
            {
                // Filter versions based on includePreviewVersions parameter
                var versions = includePreviewVersions 
                    ? allVersions 
                    : [.. allVersions.Where(v => !v.IsPrerelease)];
                
                if (versions.Any())
                {
                    var latestVersion = versions.Max();
                    
                    if (latestVersion != null && latestVersion > currentVersion)
                    {
                        analysis.HasUpdate = true;
                        analysis.LatestVersion = latestVersion.ToString();
                        analysis.IsPreviewVersion = latestVersion.IsPrerelease;
                        
                        // If no preview versions requested but we have newer preview versions, check for that too
                        if (!includePreviewVersions && allVersions.Any(v => v.IsPrerelease && v > latestVersion))
                        {
                            var previewVersions = allVersions.Where(v => v.IsPrerelease).ToList();
                            if (previewVersions.Count != 0)
                            {
                                var latestPreviewVersion = previewVersions.Max();
                                if (latestPreviewVersion != null)
                                {
                                    analysis.HasPreviewUpdate = true;
                                    analysis.LatestPreviewVersion = latestPreviewVersion.ToString();
                                }
                            }
                        }
                    }
                    else if (!includePreviewVersions)
                    {
                        // Check if there are preview updates available when not including them in regular updates
                        var previewVersions = allVersions.Where(v => v.IsPrerelease && v > currentVersion).ToList();
                        if (previewVersions.Count != 0)
                        {
                            var latestPreviewVersion = previewVersions.Max();
                            if (latestPreviewVersion != null)
                            {
                                analysis.HasPreviewUpdate = true;
                                analysis.LatestPreviewVersion = latestPreviewVersion.ToString();
                            }
                        }
                    }
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

            // Mark package as used if it's a known special package in an appropriate project
            if (isKnownSpecialPackage)
            {
                // For test packages, they're only considered used in test projects
                if (packageCategory?.Contains("Test") == true && isTestProject)
                {
                    analysis.IsUsed = true;
                    analysis.ImplicitUsage = true;
                    analysis.UsageLocations.Add($"[{packageCategory}] - Implicitly used in test project");
                }
                // For non-test infrastructure packages, they're always considered used
                else if (!packageCategory?.Contains("Test") == true)
                {
                    analysis.IsUsed = true;
                    analysis.ImplicitUsage = true;
                    analysis.UsageLocations.Add($"[{packageCategory}] - Implicitly used infrastructure package");
                }
            }

            // If not already determined to be used, check for code references
            if (!analysis.IsUsed)
            {
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

                            // Look for attribute usage in test projects which often don't have direct using statements
                            if (isTestProject && !analysis.IsUsed)
                            {
                                var attributePatterns = new[] { 
                                    $"[Fact", $"[Theory", $"[Test", $"[TestMethod", 
                                    $"[Collection", $"[Trait", $"[SkippableFact", $"[Skip" 
                                };
                                
                                foreach (var line in lines)
                                {
                                    var trimmedLine = line.Trim();
                                    if (attributePatterns.Any(pattern => trimmedLine.StartsWith(pattern)))
                                    {
                                        if (package.Id.Contains("xunit") || package.Id.Contains("NUnit") || 
                                            package.Id.Contains("MSTest") || package.Id.Contains("Fact"))
                                        {
                                            analysis.IsUsed = true;
                                            analysis.ImplicitUsage = true;
                                            analysis.UsageLocations.Add($"{file} (attribute usage)");
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            if (analysis.IsUsed) break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading file {file}: {ex.Message}");
                        }
                    }

                    // Check for implicit usage in other file types
                    if (!analysis.IsUsed)
                    {
                        // Check for analyzer usage in .editorconfig, Directory.Build.props, etc.
                        var buildFiles = Directory.GetFiles(projectDir, "*.props", SearchOption.AllDirectories)
                            .Concat(Directory.GetFiles(projectDir, "*.targets", SearchOption.AllDirectories))
                            .Concat(Directory.GetFiles(projectDir, ".editorconfig", SearchOption.AllDirectories))
                            .Concat(Directory.GetFiles(projectDir, "*.ruleset", SearchOption.AllDirectories))
                            .ToArray();

                        foreach (var file in buildFiles)
                        {
                            try
                            {
                                var content = await File.ReadAllTextAsync(file);
                                if (content.Contains(package.Id, StringComparison.OrdinalIgnoreCase))
                                {
                                    analysis.IsUsed = true;
                                    analysis.ImplicitUsage = true;
                                    analysis.UsageLocations.Add($"{file} (build configuration)");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error reading file {file}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accessing directory: {ex.Message}");
                }
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
                    var updateMessage = $"Update available: {analysis.LatestVersion}";
                    if (analysis.IsPreviewVersion)
                    {
                        updateMessage += " (Preview)";
                    }
                    analysis.RecommendedAction = updateMessage;
                }
                else if (analysis.HasPreviewUpdate && !analysis.HasUpdate)
                {
                    analysis.RecommendedAction = $"Preview update available: {analysis.LatestPreviewVersion}";
                }
            }
        }
        catch (Exception ex)
        {
            analysis.RecommendedAction = $"Error analyzing package: {ex.Message}";
        }

        return analysis;
    }

    private static async Task GatherDependenciesForPackageAsync(
        string packageId,
        NuGetVersion packageVersion,
        List<string> dependencies,
        HashSet<string> visited,
        int currentDepth,
        int maxDepth)
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
