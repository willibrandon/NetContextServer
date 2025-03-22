using System;
using System.IO;
using Xunit;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
public class BasicServerTests : IDisposable
{
    private readonly string _testDir;

    public BasicServerTests()
    {
        // Setup test directory and files
        _testDir = Path.Combine(Path.GetTempPath(), $"NetContextServer_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        
        // Set base directory for tests
        Tools.SetBaseDirectory(_testDir);
    }

    [Fact(Skip = "Not implemented yet")]
    public void ShouldConnectToServer()
    {
        // Test would verify server connection
    }

    [Fact(Skip = "Not implemented yet")]
    public void ShouldReturnServerInfo()
    {
        // Test would verify server info
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