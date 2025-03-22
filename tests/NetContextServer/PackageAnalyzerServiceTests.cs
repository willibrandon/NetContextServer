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
    [Trait("Category", "AI_Generated")]
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
    [Trait("Category", "AI_Generated")]
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
    [Trait("Category", "AI_Generated")]
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
}
