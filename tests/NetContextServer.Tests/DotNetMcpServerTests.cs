using NetContextServer.Core;
using System.IO;
using System.Reflection;

namespace NetContextServer.Tests;

public class DotNetMcpServerTests : IDisposable
{
    private readonly string _testDirectory;

    public DotNetMcpServerTests()
    {
        // Create a test directory structure
        _testDirectory = Path.Combine(Path.GetTempPath(), "NetContextServerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Create a test project file
        var projectDir = Path.Combine(_testDirectory, "TestProject");
        Directory.CreateDirectory(projectDir);
        var projectPath = Path.Combine(projectDir, "TestProject.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        // Create a test source file
        var sourceDir = Path.Combine(projectDir, "src");
        Directory.CreateDirectory(sourceDir);
        var sourcePath = Path.Combine(sourceDir, "TestClass.cs");
        File.WriteAllText(sourcePath, "namespace TestProject { public class TestClass { public void TestMethod() { } } }");
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void Constructor_InitializesWithSolutionPath()
    {
        // Act
        var server = new DotNetMcpServer(_testDirectory);

        // Assert
        Assert.NotNull(server);
        // No exception means success
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void Constructor_WithInvalidPath_DoesNotThrow()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        var server = new DotNetMcpServer(invalidPath);
        Assert.NotNull(server);
        // No exception means success - the actual validation happens in StartAsync
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public async Task StartAsync_WithInvalidPath_ThrowsException()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var server = new DotNetMcpServer(invalidPath);
        
        // Act & Assert
        // This should throw because the ProjectIndexer can't find any projects in the invalid path
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => server.StartAsync());
    }

    // Note: We can't easily test StartAsync directly because it starts an MCP server
    // that blocks the thread. In a real-world scenario, we would mock the MCPServer
    // or use a test-specific implementation.

    public void Dispose()
    {
        // Clean up the test directory
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
} 