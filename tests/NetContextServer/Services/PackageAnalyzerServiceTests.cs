using NetContextServer.Services;

namespace NetContextServer.Tests.Services;

[Trait("Category", "AI_Generated")]
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
        var packages = await _service.GetPackageReferencesAsync(_testProjectPath);

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
        var packages = await _service.GetPackageReferencesAsync(_testProjectPath);

        // Assert
        Assert.Empty(packages);
    }

    [Fact]
    public async Task GetPackageReferencesAsync_WithInvalidPath_ReturnsEmptyList()
    {
        // Arrange
        var invalidPath = Path.Combine(_testProjectDir, "invalid", "path", "project.csproj");

        // Act
        var packages = await _service.GetPackageReferencesAsync(invalidPath);

        // Assert
        Assert.Empty(packages);
    }

    [Fact]
    public async Task AnalyzePackageAsync_WithUnusedPackage_RecommendsRemoval()
    {
        // Arrange
        var csFilePath = Path.Combine(_testProjectDir, "Test.cs");
        await File.WriteAllTextAsync(csFilePath, "namespace Test { public class TestClass {} }");

        var package = new PackageAnalyzerService.PackageReference
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

        var package = new PackageAnalyzerService.PackageReference
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
        var package = new PackageAnalyzerService.PackageReference
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
}
