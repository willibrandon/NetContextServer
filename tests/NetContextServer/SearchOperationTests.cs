using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
public class SearchOperationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testCsFilePath;

    public SearchOperationTests()
    {
        // Setup test directory and files
        _testDir = Path.Combine(Path.GetTempPath(), $"NetContextServer_Test_{Guid.NewGuid()}");
        _testCsFilePath = Path.Combine(_testDir, "Test.cs");

        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_testCsFilePath, "public class TestClass { public void TestMethod() {} }");
        
        // Set base directory for tests
        Tools.SetBaseDirectory(_testDir);
    }

    [Fact]
    public void SearchCode_WithValidText_ReturnsMatches()
    {
        // Act
        var result = Tools.SearchCode("TestClass");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Contains("TestClass"));
    }

    [Fact]
    public void SearchCode_WithNonExistentText_ReturnsEmptyArray()
    {
        // Act
        var result = Tools.SearchCode("NonExistentText");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact(Skip = "Semantic search requires embedding model")]
    public void SemanticSearch_WithValidQuery_ReturnsResults()
    {
        // This test requires a semantic search implementation
        // which depends on embedding models, so we're skipping it
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