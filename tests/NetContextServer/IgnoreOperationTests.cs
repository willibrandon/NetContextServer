using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
public class IgnoreOperationTests : IDisposable
{
    private readonly string _testDir;

    public IgnoreOperationTests()
    {
        // Setup test directory
        _testDir = Path.Combine(Path.GetTempPath(), $"NetContextServer_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        
        // Set base directory for tests
        Tools.SetBaseDirectory(_testDir);
    }

    [Fact]
    public void AddIgnorePatterns_ShouldAddValidPatterns()
    {
        // Arrange
        var patterns = new[] { "*.log", "*/temp/*" };
        
        // Act
        var result = Tools.AddIgnorePatterns(patterns);
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains(patterns[0], result.ValidPatternsAdded);
        Assert.Contains(patterns[1], result.ValidPatternsAdded);
    }

    [Fact]
    public void GetIgnorePatterns_ShouldReturnPatterns()
    {
        // Arrange
        var patterns = new[] { "*.log", "*/temp/*" };
        Tools.AddIgnorePatterns(patterns);
        
        // Act
        var result = Tools.GetIgnorePatterns();
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains(patterns[0], result.UserPatterns);
        Assert.Contains(patterns[1], result.UserPatterns);
    }

    [Fact]
    public void RemoveIgnorePatterns_ShouldRemoveUserPatterns()
    {
        // Arrange
        var patterns = new[] { "*.log", "*/temp/*" };
        Tools.AddIgnorePatterns(patterns);
        
        // Act
        var result = Tools.RemoveIgnorePatterns(new[] { "*.log" });
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains("*.log", result.RemovedPatterns);
        Assert.DoesNotContain("*.log", result.AllPatterns);
        Assert.Contains("*/temp/*", result.AllPatterns);
    }

    [Fact]
    public void ClearIgnorePatterns_ShouldRemoveAllUserPatterns()
    {
        // Arrange
        var patterns = new[] { "*.log", "*/temp/*" };
        Tools.AddIgnorePatterns(patterns);
        
        // Act
        var result = Tools.ClearIgnorePatterns();
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.UserPatterns);
        Assert.DoesNotContain("*.log", result.UserPatterns);
        Assert.DoesNotContain("*/temp/*", result.UserPatterns);
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
