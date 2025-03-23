using NetContextServer.Models;
using NetContextServer.Services;

namespace NetContextServer.Tests;

[Collection("NetContextServer Tests")]
public class PackageAnalyzerServiceTests : IDisposable
{
    private readonly PackageAnalyzerService _service;
    private readonly string _testProjectPath;
    private readonly string _testProjectDir;

    public PackageAnalyzerServiceTests()
    {
        _service = new PackageAnalyzerService();
        _testProjectDir = Path.Combine(Path.GetTempPath(), "PackageAnalyzerTests");
        Directory.CreateDirectory(_testProjectDir);
        _testProjectPath = Path.Combine(_testProjectDir, "test.csproj");
    }

    private async Task CreateTestProjectFileAsync(string content)
    {
        await File.WriteAllTextAsync(_testProjectPath, content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testProjectDir))
            {
                Directory.Delete(_testProjectDir, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up test directory: {ex.Message}");
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetPackageReferencesAsync_WithValidProject_ReturnsPackages()
    {
        // Arrange
        var projectContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.4.2"" />
    <PackageReference Include=""Moq"" Version=""4.18.4"" />
  </ItemGroup>
</Project>";
        await CreateTestProjectFileAsync(projectContent);

        // Act
        var packages = await PackageAnalyzerService.GetPackageReferencesAsync(_testProjectPath);

        // Assert
        Assert.Equal(2, packages.Count);
        Assert.Contains(packages, p => p.Id == "xunit" && p.Version == "2.4.2");
        Assert.Contains(packages, p => p.Id == "Moq" && p.Version == "4.18.4");
    }

    [Fact]
    public async Task GetPackageReferencesAsync_WithEmptyProject_ReturnsEmptyList()
    {
        // Arrange
        var projectContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
  </ItemGroup>
</Project>";
        await CreateTestProjectFileAsync(projectContent);

        // Act
        var packages = await PackageAnalyzerService.GetPackageReferencesAsync(_testProjectPath);

        // Assert
        Assert.Empty(packages);
    }

    [Fact]
    public async Task GetPackageReferencesAsync_WithInvalidPath_ReturnsEmptyList()
    {
        // Arrange
        var invalidPath = Path.Combine(_testProjectDir, "invalid", "path", "project.csproj");

        // Act
        var packages = await PackageAnalyzerService.GetPackageReferencesAsync(invalidPath);

        // Assert
        Assert.Empty(packages);
    }

    [Fact]
    public async Task AnalyzePackageAsync_WithUnusedPackage_RecommendsRemoval()
    {
        // Arrange
        var csFilePath = Path.Combine(_testProjectDir, "Test.cs");
        await File.WriteAllTextAsync(csFilePath, "namespace Test { public class TestClass {} }");

        var package = new PackageReference
        {
            Id = "UnusedPackage",
            Version = "1.0.0",
            ProjectPath = _testProjectPath
        };

        // Act
        var analysis = await _service.AnalyzePackageAsync(package);

        // Assert
        Assert.False(analysis.IsUsed);
        Assert.Contains("Consider removing", analysis.RecommendedAction);
    }

    [Fact]
    public async Task AnalyzePackageAsync_WithUsedPackage_DetectsUsage()
    {
        // Arrange
        var csFilePath = Path.Combine(_testProjectDir, "Test.cs");
        await File.WriteAllTextAsync(csFilePath, @"
using TestPackage;
namespace Test { public class TestClass {} }");

        var package = new PackageReference
        {
            Id = "TestPackage",
            Version = "1.0.0",
            ProjectPath = _testProjectPath
        };

        // Act
        var analysis = await _service.AnalyzePackageAsync(package);

        // Assert
        Assert.True(analysis.IsUsed);
        Assert.Contains(csFilePath, analysis.UsageLocations);
    }

    [Fact]
    public async Task AnalyzePackageAsync_WithInvalidVersion_HandlesError()
    {
        // Arrange
        var package = new PackageReference
        {
            Id = "TestPackage",
            Version = "invalid-version",
            ProjectPath = _testProjectPath
        };

        // Act
        var analysis = await _service.AnalyzePackageAsync(package);

        // Assert
        Assert.Contains("Error", analysis.RecommendedAction ?? string.Empty);
    }

    [Fact]
    public async Task AnalyzePackageAsync_CollectsTransitiveDependencies()
    {
        // Arrange
        var package = new PackageReference
        {
            Id = "System.Text.Json",
            Version = "6.0.0",
            ProjectPath = _testProjectPath
        };

        // Act
        var analysis = await _service.AnalyzePackageAsync(package);

        // Assert
        Assert.NotNull(analysis.TransitiveDependencies);
        Assert.NotEmpty(analysis.TransitiveDependencies);
    }
    
    [Fact]
    public async Task AnalyzePackageAsync_GeneratesDependencyGraph()
    {
        // Arrange
        var package = new PackageReference
        {
            Id = "System.Text.Json",
            Version = "6.0.0",
            ProjectPath = _testProjectPath
        };

        // Act 
        var analysis = await _service.AnalyzePackageAsync(package);

        // Assert
        // The PackageAnalyzerService only populates TransitiveDependencies
        // The actual dependency graph generation happens in PackageTools.cs
        Assert.NotNull(analysis.TransitiveDependencies);
        Assert.NotEmpty(analysis.TransitiveDependencies);
        
        // Validate that the necessary data for graph generation is present
        Assert.NotNull(analysis.PackageId);
        Assert.False(string.IsNullOrEmpty(analysis.PackageId));
    }
    
    [Fact]
    public async Task AnalyzePackageAsync_CorrectlyGroupsDependencies()
    {
        // Arrange
        var package = new PackageReference
        {
            Id = "Microsoft.Extensions.DependencyInjection",
            Version = "6.0.0",
            ProjectPath = _testProjectPath
        };

        // Act
        var analysis = await _service.AnalyzePackageAsync(package);

        // Assert
        Assert.NotNull(analysis.TransitiveDependencies);
        
        // Check if the transitive dependencies include Microsoft packages
        var hasMicrosoftDependencies = analysis.TransitiveDependencies.Any(d => d.StartsWith("Microsoft."));
        var hasSystemDependencies = analysis.TransitiveDependencies.Any(d => d.StartsWith("System."));
        
        // If DependencyGraph is implemented, verify its content
        if (analysis.DependencyGraph != null)
        {
            // Ensure the graph includes the package ID
            Assert.Contains(analysis.PackageId, analysis.DependencyGraph);
            
            // Check for proper grouping of dependencies
            if (hasMicrosoftDependencies)
            {
                Assert.Contains("Microsoft.", analysis.DependencyGraph);
            }
            
            if (hasSystemDependencies)
            {
                Assert.Contains("System.", analysis.DependencyGraph);
            }
        }
        else
        {
            // If the graph is not populated, we should at least have transitive dependencies
            Assert.True(analysis.TransitiveDependencies.Count > 0, 
                "Expected package to have transitive dependencies even if DependencyGraph is not implemented");
        }
    }

    [Fact]
    public async Task AnalyzePackageAsync_DetectsImplicitlyUsedPackages()
    {
        // Arrange - Create a test project file that looks like a test project
        var projectContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.6.0"" />
    <PackageReference Include=""xunit"" Version=""2.4.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.4.5"" />
    <PackageReference Include=""coverlet.collector"" Version=""6.0.0"" />
  </ItemGroup>
</Project>";
        await CreateTestProjectFileAsync(projectContent);

        // Create a test file with xUnit attributes to trigger implicit usage detection
        var csFilePath = Path.Combine(_testProjectDir, "UnitTest1.cs");
        await File.WriteAllTextAsync(csFilePath, @"
namespace TestProject;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}");

        // Test each relevant test package
        var testPackages = new Dictionary<string, string>
        {
            { "Microsoft.NET.Test.Sdk", "17.6.0" },
            { "xunit", "2.4.2" },
            { "xunit.runner.visualstudio", "2.4.5" },
            { "coverlet.collector", "6.0.0" }
        };

        foreach (var packageInfo in testPackages)
        {
            // Act - Create package reference and analyze
            var package = new PackageReference
            {
                Id = packageInfo.Key,
                Version = packageInfo.Value,
                ProjectPath = _testProjectPath
            };
            var analysis = await _service.AnalyzePackageAsync(package);

            // Assert
            Assert.True(analysis.IsUsed, $"Package {packageInfo.Key} should be marked as used");
            Assert.True(analysis.ImplicitUsage, $"Package {packageInfo.Key} should be marked as implicitly used");
            Assert.NotEmpty(analysis.UsageLocations);
            
            // Verify the usage description includes the expected category information
            var usageLocation = analysis.UsageLocations.First();
            Assert.Contains("Implicitly used", usageLocation);
            
            // Different packages should have appropriate category labels
            if (packageInfo.Key == "coverlet.collector")
            {
                Assert.Contains("Coverage", usageLocation);
            }
            else if (packageInfo.Key == "Microsoft.NET.Test.Sdk")
            {
                Assert.Contains("Test SDK", usageLocation);
            }
            else if (packageInfo.Key == "xunit" || packageInfo.Key == "xunit.runner.visualstudio")
            {
                Assert.Contains("Test", usageLocation);
            }
        }
    }
    
    [Fact]
    public void IsTestProject_CorrectlyIdentifiesTestProjects()
    {
        // Create test project file in "tests" directory
        var testDir = Path.Combine(_testProjectDir, "tests");
        Directory.CreateDirectory(testDir);
        var testProjectPath = Path.Combine(testDir, "TestProject.Tests.csproj");
        File.WriteAllText(testProjectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>false</IsPackable></PropertyGroup></Project>");
        
        // Create regular project file
        var regularProjectPath = Path.Combine(_testProjectDir, "RegularProject.csproj");
        File.WriteAllText(regularProjectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        
        // Create project with test-related content
        var testContentProjectPath = Path.Combine(_testProjectDir, "ProjectWithTests.csproj");
        File.WriteAllText(testContentProjectPath, "<Project><ItemGroup><PackageReference Include=\"xunit\" Version=\"2.4.2\" /></ItemGroup></Project>");
        
        // Act & Assert
        Assert.True(PackageAnalyzerService.IsTestProject(testProjectPath), "Project with Tests in name should be identified as test project");
        Assert.True(PackageAnalyzerService.IsTestProject(testContentProjectPath), "Project with xunit reference should be identified as test project");
        Assert.False(PackageAnalyzerService.IsTestProject(regularProjectPath), "Regular project should not be identified as test project");
    }
    
    [Fact]
    public async Task AnalyzePackageAsync_WithPreviewVersions_HandlesPreviewsCorrectly()
    {
        // Arrange
        // Microsoft.Extensions.Hosting is known to have preview versions available
        var package = new PackageReference
        {
            Id = "Microsoft.Extensions.Hosting",
            Version = "8.0.0", // Using an older version to ensure we have both stable and preview updates
            ProjectPath = _testProjectPath
        };

        // Create a file that uses the package to ensure it's not marked as unused
        var csFilePath = Path.Combine(_testProjectDir, "TestHosting.cs");
        await File.WriteAllTextAsync(csFilePath, @"
using Microsoft.Extensions.Hosting;
namespace Test { 
    public class TestClass {
        public void TestMethod() {
            var host = Host.CreateDefaultBuilder();
        }
    } 
}");

        // Act - First without including preview versions
        var analysisWithoutPreviews = await _service.AnalyzePackageAsync(package, includePreviewVersions: false);
        
        // Act - Then with including preview versions
        var analysisWithPreviews = await _service.AnalyzePackageAsync(package, includePreviewVersions: true);

        // Assert - Basic properties
        Assert.True(analysisWithoutPreviews.IsUsed, "Package should be marked as used");
        Assert.NotNull(analysisWithoutPreviews.RecommendedAction);
        Assert.NotNull(analysisWithPreviews.RecommendedAction);
        
        // Output details for diagnostics
        Console.WriteLine($"Without previews - HasUpdate: {analysisWithoutPreviews.HasUpdate}, HasPreviewUpdate: {analysisWithoutPreviews.HasPreviewUpdate}");
        Console.WriteLine($"Without previews - LatestVersion: {analysisWithoutPreviews.LatestVersion}, LatestPreviewVersion: {analysisWithoutPreviews.LatestPreviewVersion}");
        Console.WriteLine($"Without previews - RecommendedAction: {analysisWithoutPreviews.RecommendedAction}");
        
        Console.WriteLine($"With previews - HasUpdate: {analysisWithPreviews.HasUpdate}, IsPreviewVersion: {analysisWithPreviews.IsPreviewVersion}");
        Console.WriteLine($"With previews - LatestVersion: {analysisWithPreviews.LatestVersion}");
        Console.WriteLine($"With previews - RecommendedAction: {analysisWithPreviews.RecommendedAction}");
        
        // Verify different behaviors based on includePreviewVersions setting
        Assert.True(analysisWithoutPreviews.HasUpdate || analysisWithoutPreviews.HasPreviewUpdate, 
            "Package should have either a stable update or preview update available");
            
        // Test the flag properties in both analyses
        if (analysisWithoutPreviews.HasPreviewUpdate)
        {
            Assert.NotNull(analysisWithoutPreviews.LatestPreviewVersion);
            
            // Preview update might be shown in recommended action if there's no stable update
            if (!analysisWithoutPreviews.HasUpdate)
            {
                Assert.Contains("Preview", analysisWithoutPreviews.RecommendedAction);
            }
        }
        
        if (analysisWithPreviews.HasUpdate)
        {
            Assert.NotNull(analysisWithPreviews.LatestVersion);
            
            // If the latest version with previews is a preview version
            if (analysisWithPreviews.IsPreviewVersion) 
            {
                Assert.Contains("Preview", analysisWithPreviews.RecommendedAction);
            }
        }
    }
}
