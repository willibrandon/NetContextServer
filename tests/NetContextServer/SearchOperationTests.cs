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

// Add search operation methods to the Tools class
internal static partial class Tools
{
    public static string[] SearchCode(string searchText)
    {
        var results = new List<string>();
        
        foreach (var pattern in new[] { "*.cs", "*.vb", "*.fs", "*.fsx", "*.fsi", "*.cshtml", "*.vbhtml", "*.razor" })
        {
            var files = Directory.GetFiles(_baseDirectory, pattern, SearchOption.AllDirectories)
                .Where(f => !ShouldIgnoreFile(f));
            
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add($"{file}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
        }
        
        return results.ToArray();
    }
} 