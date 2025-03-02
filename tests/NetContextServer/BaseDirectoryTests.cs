using MCPSharp;
using System.Diagnostics;
using System.Text.Json;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
[Collection("NetContextServer Tests")]
public class BaseDirectoryTests : IDisposable
{
    private readonly string _testDir;
    private readonly MCPClient client;

    public BaseDirectoryTests()
    {
        // Kill any running NetContextServer processes
        try
        {
            foreach (var process in Process.GetProcessesByName("NetContextServer"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(3000); // Wait up to 3 seconds for the process to exit
                }
                catch
                {
                    // Ignore errors when trying to kill processes
                }
            }
        }
        catch
        {
            // Ignore any exceptions when trying to get or kill processes
        }

        // Setup test directory
        _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerBaseDirectoryTests");
        Directory.CreateDirectory(_testDir);

        var executableName = OperatingSystem.IsWindows() ? "NetContextServer.exe" : "NetContextServer";
        client = new MCPClient("Test Client", "1.0.0", executableName);
    }

    [Fact]
    public async Task SetBaseDirectory_ValidPath_ShouldSetBaseDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(_testDir, "ValidBaseDir");
        Directory.CreateDirectory(tempDir);

        // Act
        var result = await client.CallToolAsync("set_base_directory", new Dictionary<string, object>
        {
            { "directory", tempDir }
        });

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Content);
        
        var responseJson = JsonDocument.Parse(result.Content[0].Text);
        var responseArray = responseJson.RootElement.EnumerateArray().ToArray();
        
        Assert.Single(responseArray);
        Assert.Contains("Base directory set to:", responseArray[0].GetString());
    }

    [Fact]
    public async Task SetBaseDirectory_InvalidPath_ShouldReturnError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistentDir");

        // Act
        var result = await client.CallToolAsync("set_base_directory", new Dictionary<string, object>
        {
            { "directory", invalidPath }
        });

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Content);
        
        var responseJson = JsonDocument.Parse(result.Content[0].Text);
        var responseArray = responseJson.RootElement.EnumerateArray().ToArray();
        
        Assert.Single(responseArray);
        Assert.Equal("Error: Directory not found", responseArray[0].GetString());
    }

    [Fact]
    public async Task GetBaseDirectory_ShouldReturnCurrentBaseDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(_testDir, "GetBaseDir");
        Directory.CreateDirectory(tempDir);

        // Set the base directory first
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object>
        {
            { "directory", tempDir }
        });

        // Act
        var result = await client.CallToolAsync("get_base_directory", new Dictionary<string, object>());

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Content);
        
        var response = JsonSerializer.Deserialize<BaseDirectoryResponse>(
            result.Content[0].Text, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Assert.NotNull(response);
        Assert.Equal(tempDir, response.BaseDirectory);
        Assert.True(response.Exists);
    }

    [Fact]
    public async Task GetBaseDirectory_AfterDirectoryDeleted_ShouldShowExistsFalse()
    {
        // Arrange
        var tempDir = Path.Combine(_testDir, "DeletedBaseDir");
        Directory.CreateDirectory(tempDir);

        // Set the base directory
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object>
        {
            { "directory", tempDir }
        });

        // Delete the directory
        Directory.Delete(tempDir);

        // Act
        var result = await client.CallToolAsync("get_base_directory", new Dictionary<string, object>());

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Content);
        
        var response = JsonSerializer.Deserialize<BaseDirectoryResponse>(
            result.Content[0].Text, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Assert.NotNull(response);
        Assert.Equal(tempDir, response.BaseDirectory);
        Assert.False(response.Exists);
    }

    public void Dispose()
    {
        // Reset the base directory
        try
        {
            NetContextServer.SetBaseDirectory(Directory.GetCurrentDirectory());
        }
        catch
        {
            // Ignore errors when resetting base directory
        }

        // Cleanup test directory
        try
        {
            Directory.Delete(_testDir, true);
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Dispose the client
        try
        {
            client?.Dispose();
        }
        catch
        {
            // Ignore errors when disposing client
        }

        // Kill any remaining NetContextServer processes
        try
        {
            foreach (var process in Process.GetProcessesByName("NetContextServer"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(1000);
                }
                catch
                {
                    // Ignore errors when killing processes
                }
            }
        }
        catch
        {
            // Ignore errors when getting processes
        }

        GC.SuppressFinalize(this);
    }

    private class BaseDirectoryResponse
    {
        public string BaseDirectory { get; set; } = string.Empty;
        public bool Exists { get; set; }
    }
} 