using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class ThinkOperationTests(NetContextServerFixture fixture) : IAsyncLifetime
{
    private readonly IMcpClient _client = fixture.Client;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private class ThinkResponse
    {
        public string Thought { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
        public string? Category { get; set; }
        public string? Timestamp { get; set; }
        public int? CharacterCount { get; set; }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Think_WithValidThought_ReturnsThoughtAndMessage()
    {
        // Arrange
        var testThought = "Testing the think tool with a simple thought";

        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = testThought });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.Equal(testThought, response.Thought);
        Assert.NotEmpty(response.Message);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task Think_WithEmptyThought_ReturnsEmptyThought()
    {
        // Arrange
        var emptyThought = "";

        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = emptyThought });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Contains("Missing required parameter", response.Error);
    }

    [Fact]
    public async Task Think_WithLongThought_HandlesLongTextCorrectly()
    {
        // Arrange
        var longThought = new string('x', 10000); // Create a 10,000 character thought

        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = longThought });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.Equal(longThought, response.Thought);
        Assert.NotEmpty(response.Message);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task Think_WithSpecialCharacters_HandlesSpecialCharactersCorrectly()
    {
        // Arrange
        var thoughtWithSpecialChars = "Testing\nwith\tspecial\r\ncharacters!@#$%^&*()_+";

        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = thoughtWithSpecialChars });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.Equal(thoughtWithSpecialChars, response.Thought);
        Assert.NotEmpty(response.Message);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task Think_WithoutThoughtParameter_ReturnsError()
    {
        // Act
        var result = await _client.CallToolAsync("think", new Dictionary<string, object?>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        // Try to parse as JSON first, if that fails, check plain text
        try
        {
            var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
            Assert.NotNull(response);
            Assert.NotNull(response.Error);
            Assert.Contains("Missing required parameter", response.Error);
        }
        catch (JsonException)
        {
            // If not JSON, it should be a plain text error message
            Assert.StartsWith("Missing required parameter", content.Text);
        }
    }

    [Fact]
    public async Task Think_WithOversizedThought_ReturnsError()
    {
        // Arrange
        var oversizedThought = new string('x', 40000); // Create a 40KB thought (exceeds 32KB limit)

        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = oversizedThought });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Contains("exceeds maximum length", response.Error);
        Assert.Contains("32768", response.Error); // Verify the error message includes the size limit
    }

    [Fact]
    public async Task Think_WithPotentiallyHarmfulContent_ReturnsError()
    {
        // Arrange
        var harmfulThought = "Let's try system('rm -rf /') to clean up";

        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = harmfulThought });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Contains("invalid content", response.Error.ToLower());
    }

    [Fact]
    public async Task Think_WithUnicodeAndEmoji_HandlesSpecialCharactersCorrectly()
    {
        // Arrange
        var unicodeThought = "Testing emoji: 🔍 🚀 👍";

        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = unicodeThought });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.Equal(unicodeThought, response.Thought);
        Assert.Contains("🔍", response.Thought);
        Assert.Contains("🚀", response.Thought);
        Assert.Contains("👍", response.Thought);
        Assert.NotEmpty(response.Message);
        Assert.Null(response.Error);
    }

    [Theory]
    [InlineData("Let's refactor this method", "Refactoring")]
    [InlineData("Need to fix security vulnerability", "Security")]
    [InlineData("Performance optimization needed", "Performance")]
    [InlineData("Adding new test cases", "Testing")]
    [InlineData("Reviewing architecture design", "Architecture")]
    [InlineData("Random thought", "General")]
    public async Task Think_WithDifferentCategories_ReturnsCategorizedThought(string thought, string expectedCategory)
    {
        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = thought });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.Equal(expectedCategory, response.Category);
        Assert.Equal(thought.Length, response.CharacterCount);
    }

    [Fact]
    public async Task Think_WithValidThought_ReturnsISO8601Timestamp()
    {
        // Arrange
        var testThought = "Testing timestamp format";

        // Act
        var result = await _client.CallToolAsync("think", 
            new Dictionary<string, object?> { ["thought"] = testThought });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<ThinkResponse>(content.Text, DefaultJsonOptions);
        Assert.NotNull(response);
        Assert.NotNull(response.Timestamp);
        Assert.True(DateTime.TryParse(response.Timestamp, out _));
        Assert.Contains("T", response.Timestamp);
        Assert.Contains(":", response.Timestamp);
    }
}
