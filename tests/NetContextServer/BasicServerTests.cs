using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Types;
using System.Text.Json;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class BasicServerTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;
    private readonly IMcpClient _client;

    public BasicServerTests(NetContextServerFixture fixture)
    {
        _client = fixture.Client;
        _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerTests_" + Guid.NewGuid());
        _testProjectPath = Path.Combine(_testDir, "Test.csproj");
        _testCsFilePath = Path.Combine(_testDir, "Test.cs");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ListTools_ReturnsValidToolList()
    {
        // Act
        var tools = new List<Tool>();
        await foreach (var tool in _client.ListToolsAsync())
        {
            tools.Add(tool);
        }

        // Assert
        Assert.NotNull(tools);
        Assert.NotEmpty(tools);
        
        foreach (var tool in tools)
        {
            Assert.NotNull(tool.Name);
            Assert.NotNull(tool.Description);
            Assert.NotEmpty(tool.Name);
            Assert.NotEmpty(tool.Description);
        }
    }

    [Fact]
    public async Task Hello_ReturnsSuccess()
    {
        // Act
        var result = await _client.CallToolAsync("hello", new Dictionary<string, object>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        Console.WriteLine($"Actual hello response: {content.Text}");
        Assert.Equal("hello, claude.", content.Text);
    }

    [Fact]
    public async Task CallInvalidTool_ReturnsError()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<McpClientException>(
            () => _client.CallToolAsync("invalid_tool", new Dictionary<string, object>()));
        Assert.Contains("Unknown tool", ex.Message);
    }

    [Fact]
    public async Task CallToolWithInvalidParameters_ReturnsError()
    {
        // Act
        var result = await _client.CallToolAsync("list_files", new Dictionary<string, object>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        Console.WriteLine($"Raw response: {content.Text}");
        var response = JsonSerializer.Deserialize<string[]>(content.Text);
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.StartsWith("Error:", response[0]);
    }

    private class ToolInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
    }
}
