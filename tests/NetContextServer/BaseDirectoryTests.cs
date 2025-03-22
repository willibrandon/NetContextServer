using System.Text.Json;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
public class BaseDirectoryTests
{
    private readonly string _testDir;
    
    public BaseDirectoryTests()
    {
        // Create temp test directory
        _testDir = Path.Combine(Path.GetTempPath(), $"NetContextServer_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }
    
    // Test the service directly instead of through MCP
    [Fact]
    public void SetBaseDirectory_ValidPath_ShouldSetBaseDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(_testDir, "ValidBaseDir");
        Directory.CreateDirectory(tempDir);

        // Act - Call the service directly instead of through MCP
        var result = Tools.SetBaseDirectory(tempDir);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Base directory set to:", result[0]);
    }

    [Fact]
    public void SetBaseDirectory_InvalidPath_ShouldReturnError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistentDir");

        // Act - Call the service directly instead of through MCP
        var result = Tools.SetBaseDirectory(invalidPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Error: Directory not found", result[0]);
    }

    [Fact]
    public void GetBaseDirectory_ShouldReturnCurrentBaseDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(_testDir, "GetBaseDir");
        Directory.CreateDirectory(tempDir);

        // Set the base directory first
        Tools.SetBaseDirectory(tempDir);

        // Act - Call the service directly instead of through MCP
        var result = Tools.GetBaseDirectory();
        var response = JsonSerializer.Deserialize<BaseDirectoryResponse>(
            result, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(response);
        Assert.Equal(tempDir, response.BaseDirectory);
        Assert.True(response.Exists);
    }

    [Fact]
    public void GetBaseDirectory_AfterDirectoryDeleted_ShouldShowExistsFalse()
    {
        // Arrange
        var tempDir = Path.Combine(_testDir, "DeletedBaseDir");
        Directory.CreateDirectory(tempDir);

        // Set the base directory
        Tools.SetBaseDirectory(tempDir);

        // Delete the directory
        Directory.Delete(tempDir);

        // Act - Call the service directly instead of through MCP
        var result = Tools.GetBaseDirectory();
        var response = JsonSerializer.Deserialize<BaseDirectoryResponse>(
            result, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(response);
        Assert.Equal(tempDir, response.BaseDirectory);
        Assert.False(response.Exists);
    }

    private class BaseDirectoryResponse
    {
        public string BaseDirectory { get; set; } = string.Empty;
        public bool Exists { get; set; }
    }
}
