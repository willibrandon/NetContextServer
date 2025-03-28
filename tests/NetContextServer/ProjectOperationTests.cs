using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class ProjectOperationTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;
    private readonly IMcpClient _client;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }

    public ProjectOperationTests(NetContextServerFixture fixture)
    {
        _client = fixture.Client;
        _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerTests_" + Guid.NewGuid());
        _testProjectPath = Path.Combine(_testDir, "Test.csproj");
        _testCsFilePath = Path.Combine(_testDir, "Test.cs");
    }

    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(_testDir);
            File.WriteAllText(_testProjectPath, "<Project />");
            File.WriteAllText(_testCsFilePath, "public class Test { }");
        });
    }

    public async Task DisposeAsync()
    {
        try
        {
            // Reset the base directory
            await _client.CallToolAsync("set_base_directory", 
                new Dictionary<string, object?> { ["directory"] = Directory.GetCurrentDirectory() });
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
    public async Task ListProjects_ReturnsJsonArray()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("list_projects", 
            new Dictionary<string, object?>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var projects = JsonSerializer.Deserialize<string[]>(content.Text, DefaultJsonOptions);
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListSolutions_ReturnsSolutionFiles()
    {
        // Arrange: Create a test solution file
        var solutionPath = Path.Combine(_testDir, "Test.sln");
        await File.WriteAllTextAsync(solutionPath, "");
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("list_solutions", 
            new Dictionary<string, object?>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var solutions = JsonSerializer.Deserialize<string[]>(content.Text, DefaultJsonOptions);
        Assert.NotNull(solutions);
        Assert.Contains(solutions, s => s.EndsWith(".sln", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListProjectsInDirectory_WithValidPath_ReturnsProjects()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("list_projects_in_dir", 
            new Dictionary<string, object?> { ["directory"] = _testDir });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var projects = JsonSerializer.Deserialize<string[]>(content.Text, DefaultJsonOptions);
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListProjectsInDirectory_WithInvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistent");

        // Act
        var result = await _client.CallToolAsync("list_projects_in_dir", 
            new Dictionary<string, object?> { ["directory"] = invalidPath });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<string[]>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.StartsWith("Error:", response[0]);
    }
}