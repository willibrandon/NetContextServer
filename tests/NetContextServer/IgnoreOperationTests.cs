using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.Text.Json;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class IgnoreOperationTests(NetContextServerFixture fixture) : IAsyncLifetime
{
    private readonly IMcpClient _client = fixture.Client;
    private readonly string _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerTests_" + Guid.NewGuid());

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private class AddIgnorePatternsResponse
    {
        public string[] InvalidPatterns { get; set; } = [];
        public string[] ValidPatternsAdded { get; set; } = [];
        public string[] AllPatterns { get; set; } = [];
    }

    private class IgnorePatternsResponse
    {
        public string[] DefaultPatterns { get; set; } = [];
        public string[] UserPatterns { get; set; } = [];
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_testDir);
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });
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
    public async Task AddIgnorePatterns_AddsNewPatterns()
    {
        // Arrange
        var patterns = new[] { "*.txt", "*.log" };

        // Act
        var result = await _client.CallToolAsync("add_ignore_patterns", 
            new Dictionary<string, object?> { ["patterns"] = patterns });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        // Debug output
        Console.WriteLine($"Actual JSON response: {content.Text}");
        
        var response = JsonSerializer.Deserialize<AddIgnorePatternsResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.Empty(response.InvalidPatterns);
        Assert.Equal(patterns.Length, response.ValidPatternsAdded.Length);
        Assert.Contains(patterns[0], response.AllPatterns);
        Assert.Contains(patterns[1], response.AllPatterns);
    }

    [Fact]
    public async Task ClearIgnorePatterns_RemovesAllPatterns()
    {
        // Arrange
        var patterns = new[] { "*.txt", "*.log" };
        await _client.CallToolAsync("add_ignore_patterns", 
            new Dictionary<string, object?> { ["patterns"] = patterns });

        // Act
        var result = await _client.CallToolAsync("clear_ignore_patterns", 
            new Dictionary<string, object?>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        
        var response = JsonSerializer.Deserialize<IgnorePatternsResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.NotEmpty(response.DefaultPatterns);
        Assert.Empty(response.UserPatterns);
    }

    [Fact]
    public async Task GetIgnorePatterns_ReturnsCurrentPatterns()
    {
        // Arrange
        var patterns = new[] { "*.txt", "*.log" };
        await _client.CallToolAsync("add_ignore_patterns", 
            new Dictionary<string, object?> { ["patterns"] = patterns });

        // Act
        var result = await _client.CallToolAsync("get_ignore_patterns", 
            new Dictionary<string, object?>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        
        var response = JsonSerializer.Deserialize<IgnorePatternsResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.NotEmpty(response.DefaultPatterns);
        Assert.Equal(2, response.UserPatterns.Length);
        Assert.Contains(patterns[0], response.UserPatterns);
        Assert.Contains(patterns[1], response.UserPatterns);
    }

    [Fact]
    public async Task RemoveIgnorePatterns_RemovesSpecificPatterns()
    {
        // Arrange: First add some patterns
        var addPatterns = new[] { "*.secret", "password.txt", "*.config" };
        await _client.CallToolAsync("add_ignore_patterns", 
            new Dictionary<string, object?> { ["patterns"] = addPatterns });

        // Act: Remove specific patterns
        var removePatterns = new[] { "*.secret", "password.txt" };
        var result = await _client.CallToolAsync("remove_ignore_patterns", 
            new Dictionary<string, object?> { ["patterns"] = removePatterns });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<RemoveIgnorePatternsResponse>(content.Text);
        Assert.NotNull(response);
        Assert.Contains("*.secret", response.RemovedPatterns);
        Assert.Contains("password.txt", response.RemovedPatterns);
        Assert.Contains("*.config", response.AllPatterns); // Should still be in the list
        Assert.Empty(response.NotFoundPatterns);
    }

    private class RemoveIgnorePatternsResponse
    {
        public string[] RemovedPatterns { get; set; } = [];
        public string[] NotFoundPatterns { get; set; } = [];
        public string[] DefaultPatternsSkipped { get; set; } = [];
        public string[] AllPatterns { get; set; } = [];
    }
}
