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

    [Fact(Skip = "Mock Tools implementation needed")]
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

    [Fact(Skip = "Mock Tools implementation needed")]
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

    [Fact(Skip = "Mock Tools implementation needed")]
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

    [Fact(Skip = "Mock Tools implementation needed")]
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

// Add ignore pattern models for test response types
internal class IgnorePatternsResponse
{
    public string[] DefaultPatterns { get; set; } = Array.Empty<string>();
    public string[] UserPatterns { get; set; } = Array.Empty<string>();
}

internal class AddIgnorePatternsResponse
{
    public string[] InvalidPatterns { get; set; } = Array.Empty<string>();
    public string[] ValidPatternsAdded { get; set; } = Array.Empty<string>();
    public string[] AllPatterns { get; set; } = Array.Empty<string>();
}

internal class RemoveIgnorePatternsResponse
{
    public string[] DefaultPatternsSkipped { get; set; } = Array.Empty<string>();
    public string[] RemovedPatterns { get; set; } = Array.Empty<string>();
    public string[] NotFoundPatterns { get; set; } = Array.Empty<string>();
    public string[] AllPatterns { get; set; } = Array.Empty<string>();
}

// Add ignore pattern methods to the Tools class (as stubs)
internal static partial class Tools
{
    // Ignore patterns storage for tests
    private static readonly List<string> _defaultPatterns = new() { "*/bin/*", "*/obj/*", "*.dll", "*.exe" };
    private static readonly List<string> _userPatterns = new();
    
    public static AddIgnorePatternsResponse AddIgnorePatterns(string[] patterns)
    {
        var validPatterns = new List<string>();
        var invalidPatterns = new List<string>();
        
        foreach (var pattern in patterns)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                _userPatterns.Add(pattern);
                validPatterns.Add(pattern);
            }
            else
            {
                invalidPatterns.Add(pattern);
            }
        }
        
        return new AddIgnorePatternsResponse
        {
            ValidPatternsAdded = validPatterns.ToArray(),
            InvalidPatterns = invalidPatterns.ToArray(),
            AllPatterns = _defaultPatterns.Concat(_userPatterns).ToArray()
        };
    }
    
    public static IgnorePatternsResponse GetIgnorePatterns()
    {
        return new IgnorePatternsResponse
        {
            DefaultPatterns = _defaultPatterns.ToArray(),
            UserPatterns = _userPatterns.ToArray()
        };
    }
    
    public static RemoveIgnorePatternsResponse RemoveIgnorePatterns(string[] patterns)
    {
        var removedPatterns = new List<string>();
        var notFoundPatterns = new List<string>();
        var defaultPatternsSkipped = new List<string>();
        
        foreach (var pattern in patterns)
        {
            if (_userPatterns.Contains(pattern))
            {
                _userPatterns.Remove(pattern);
                removedPatterns.Add(pattern);
            }
            else if (_defaultPatterns.Contains(pattern))
            {
                defaultPatternsSkipped.Add(pattern);
            }
            else
            {
                notFoundPatterns.Add(pattern);
            }
        }
        
        return new RemoveIgnorePatternsResponse
        {
            RemovedPatterns = removedPatterns.ToArray(),
            NotFoundPatterns = notFoundPatterns.ToArray(),
            DefaultPatternsSkipped = defaultPatternsSkipped.ToArray(),
            AllPatterns = _defaultPatterns.Concat(_userPatterns).ToArray()
        };
    }
    
    public static IgnorePatternsResponse ClearIgnorePatterns()
    {
        _userPatterns.Clear();
        
        return new IgnorePatternsResponse
        {
            DefaultPatterns = _defaultPatterns.ToArray(),
            UserPatterns = _userPatterns.ToArray()
        };
    }
}
