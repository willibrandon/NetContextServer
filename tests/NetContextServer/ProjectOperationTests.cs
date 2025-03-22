using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
public class ProjectOperationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;

    public ProjectOperationTests()
    {
        // Setup test directory and files
        _testDir = Path.Combine(Path.GetTempPath(), $"NetContextServer_Test_{Guid.NewGuid()}");
        _testProjectPath = Path.Combine(_testDir, "Test.csproj");
        _testCsFilePath = Path.Combine(_testDir, "Test.cs");

        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_testProjectPath, "<Project />");
        File.WriteAllText(_testCsFilePath, "public class Test { }");
        
        // Set base directory for tests
        Tools.SetBaseDirectory(_testDir);
    }

    [Fact]
    public void ListProjects_ReturnsProjectFiles()
    {
        // Act
        var projects = Tools.ListProjects();
        
        // Assert
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.EndsWith(".csproj"));
    }

    [Fact]
    public void ListSolutions_ReturnsSolutionFiles()
    {
        // Arrange
        // Create a test solution file
        var solutionPath = Path.Combine(_testDir, "Test.sln");
        File.WriteAllText(solutionPath, "");

        // Act
        var solutions = Tools.ListSolutions();

        // Assert
        Assert.NotNull(solutions);
        Assert.Contains(solutions, s => s.EndsWith(".sln"));
    }

    [Fact]
    public void ListProjectsInDirectory_WithValidPath_ReturnsProjects()
    {
        // Act
        var projects = Tools.ListProjectsInDirectory(_testDir);

        // Assert
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.EndsWith(".csproj"));
    }

    [Fact]
    public void ListProjectsInDirectory_WithInvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistent");
        
        // Act
        var result = Tools.ListProjectsInDirectory(invalidPath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, e => e.StartsWith("Error:"));
    }

    public void Dispose()
    {
        // Cleanup test directory
        try
        {
            Directory.Delete(_testDir, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
