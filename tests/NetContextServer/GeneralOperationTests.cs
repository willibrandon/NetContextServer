using ModelContextProtocol.Client;
using System.Text.Json;
using NetContextServer.Services;
using NetContextServer.Models;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class GeneralOperationTests : IAsyncLifetime
{
    private readonly IMcpClient _client;
    private readonly string _testDir;

    public GeneralOperationTests(NetContextServerFixture fixture)
    {
        _client = fixture.Client;
        _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerTests_" + Guid.NewGuid());
    }

    public async Task InitializeAsync()
    {
        // Create test directory
        Directory.CreateDirectory(_testDir);

        // Set base directory for tests
        var args = new Dictionary<string, object?> { ["directory"] = _testDir };
        await _client.CallToolAsync("set_base_directory", args);
    }

    public async Task DisposeAsync()
    {
        try
        {
            // Reset the base directory
            var args = new Dictionary<string, object?> { ["directory"] = Directory.GetCurrentDirectory() };
            await _client.CallToolAsync("set_base_directory", args);
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
    public async Task Hello_ReturnsExpectedGreeting()
    {
        // Act
        var result = await _client.CallToolAsync("hello", new Dictionary<string, object?>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        Assert.Equal("hello, claude.", content.Text);
    }

    [Fact]
    public async Task Version_ReturnsValidInformation()
    {
        // Act
        var result = await _client.CallToolAsync("version", new Dictionary<string, object?>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var versionInfo = JsonSerializer.Deserialize<VersionInfo>(content.Text, options);
        Assert.NotNull(versionInfo);
        Assert.NotNull(versionInfo.Version);
        Assert.NotEqual("unknown", versionInfo.Version);
        Assert.NotEmpty(versionInfo.RuntimeVersion);
        Assert.NotEmpty(versionInfo.OperatingSystem);
        Assert.NotNull(versionInfo.ActiveConfiguration);
    }

    [Fact]
    public async Task Version_ContainsRequiredConfiguration()
    {
        // Act
        var result = await _client.CallToolAsync("version", new Dictionary<string, object?>());

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var versionInfo = JsonSerializer.Deserialize<VersionInfo>(content.Text, options);
        Assert.NotNull(versionInfo);
        Assert.NotNull(versionInfo.ActiveConfiguration);

        // Check required configuration keys
        Assert.Contains("BaseDirectory", versionInfo.ActiveConfiguration.Keys);
        Assert.Contains("HasIgnorePatterns", versionInfo.ActiveConfiguration.Keys);
        Assert.Contains("StateFileLocation", versionInfo.ActiveConfiguration.Keys);
    }

    [Fact(Skip = "Azure OpenAI credentials are not available")]
    public async Task Version_DetectsSemanticSearchConfiguration()
    {
        // Skip if Azure OpenAI credentials are not available
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")))
        {
            return;
        }

        // Arrange
        var originalEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var originalKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        try
        {
            // Test with no configuration
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);

            var disabledResult = await _client.CallToolAsync("version", new Dictionary<string, object?>());
            Assert.NotNull(disabledResult);
            var disabledContent = disabledResult.Content.FirstOrDefault(c => c.Type == "text");
            Assert.NotNull(disabledContent);
            Assert.NotNull(disabledContent.Text);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var disabledInfo = JsonSerializer.Deserialize<VersionInfo>(disabledContent.Text, options);
            Assert.NotNull(disabledInfo);
            Assert.False(disabledInfo.SemanticSearchEnabled);

            // Test with configuration
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://test.openai.azure.com");
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", "test-key");

            var enabledResult = await _client.CallToolAsync("version", new Dictionary<string, object?>());
            Assert.NotNull(enabledResult);
            var enabledContent = enabledResult.Content.FirstOrDefault(c => c.Type == "text");
            Assert.NotNull(enabledContent);
            Assert.NotNull(enabledContent.Text);

            var enabledInfo = JsonSerializer.Deserialize<VersionInfo>(enabledContent.Text, options);
            Assert.NotNull(enabledInfo);
            Assert.True(enabledInfo.SemanticSearchEnabled);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", originalEndpoint);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", originalKey);
        }
    }
} 