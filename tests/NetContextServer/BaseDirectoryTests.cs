using ModelContextProtocol.Client;
using System.Text.Json;
using Xunit;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class BaseDirectoryTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly IMcpClient _client;

    public BaseDirectoryTests(NetContextServerFixture fixture)
    {
        _client = fixture.Client;
        _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerBaseDirectoryTests_" + Guid.NewGuid());
    }

    public async Task InitializeAsync()
    {
        await Task.Run(() => Directory.CreateDirectory(_testDir));
    }

    public async Task DisposeAsync()
    {
        try
        {
            // Reset the base directory
            await _client.CallToolAsync("set_base_directory", 
                new Dictionary<string, object> { ["directory"] = Directory.GetCurrentDirectory() });
        }
        catch
        {
            // Ignore errors when resetting base directory
        }

        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task SetBaseDirectory_ValidPath_ShouldSetBaseDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(_testDir, "ValidBaseDir");
        Directory.CreateDirectory(tempDir);

        // Act
        var result = await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = tempDir });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var responseArray = JsonSerializer.Deserialize<string[]>(content.Text);
        Assert.NotNull(responseArray);
        Assert.Single(responseArray);
        Assert.Contains("Base directory set to:", responseArray[0]);
    }

    [Fact]
    public async Task SetBaseDirectory_InvalidPath_ShouldReturnError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistentDir");

        // Act
        var result = await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = invalidPath });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var responseArray = JsonSerializer.Deserialize<string[]>(content.Text);
        Assert.NotNull(responseArray);
        Assert.Single(responseArray);
        Assert.Equal("Error: Directory not found", responseArray[0]);
    }

    [Fact]
    public async Task GetBaseDirectory_ShouldReturnCurrentBaseDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(_testDir, "GetBaseDir");
        Directory.CreateDirectory(tempDir);

        // Set the base directory first
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = tempDir });

        // Act
        var result = await _client.CallToolAsync("get_base_directory", 
            new Dictionary<string, object>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<BaseDirectoryResponse>(
            content.Text,
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
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = tempDir });

        // Delete the directory
        Directory.Delete(tempDir);

        // Act
        var result = await _client.CallToolAsync("get_base_directory", 
            new Dictionary<string, object>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<BaseDirectoryResponse>(
            content.Text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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
