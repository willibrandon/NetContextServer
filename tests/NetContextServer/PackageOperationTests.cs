using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Types;
using NetContextServer.Models;
using NetContextServer.Services;
using System.Text.Json;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class PackageOperationTests : IAsyncLifetime
{
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly NetContextServerFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions;

    public PackageOperationTests(NetContextServerFixture fixture)
    {
        _fixture = fixture;
        _testProjectDir = Path.Combine(Path.GetTempPath(), "PackageOperationTests");
        Directory.CreateDirectory(_testProjectDir);
        _testProjectPath = Path.Combine(_testProjectDir, "test.csproj");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task InitializeAsync()
    {
        // Set up the test environment
        FileValidationService.SetBaseDirectory(_testProjectDir);
        
        // Ensure server is ready by sending a hello request
        var result = await _fixture.Client.CallToolAsync("hello", new Dictionary<string, object?>());
        if (result.IsError)
        {
            throw new InvalidOperationException("Failed to initialize server connection");
        }

        // Set the base directory in the server
        result = await _fixture.Client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testProjectDir });
        if (result.IsError)
        {
            throw new InvalidOperationException("Failed to set base directory");
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Directory.Exists(_testProjectDir))
            {
                await Task.Run(() => Directory.Delete(_testProjectDir, true));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up test directory: {ex.Message}");
        }
    }

    private async Task CreateTestProjectFileAsync(string content)
    {
        await File.WriteAllTextAsync(_testProjectPath, content);
        
        // Give the file system a moment to catch up
        await Task.Delay(100);
    }

    private T? DeserializeResponse<T>(CallToolResponse response) where T : class
    {
        var jsonText = response.Content.FirstOrDefault(c => c.Type == "text")?.Text;
        Assert.NotNull(jsonText);
        
        // Log the actual JSON for debugging
        Console.WriteLine($"Raw JSON response: {jsonText}");
        
        try
        {
            // First try to parse as JsonDocument to check the structure
            using var doc = JsonDocument.Parse(jsonText);
            
            // If it's an array and we're expecting a list, deserialize directly
            if (doc.RootElement.ValueKind == JsonValueKind.Array && 
                typeof(T).IsGenericType && 
                typeof(T).GetGenericTypeDefinition() == typeof(List<>))
            {
                return JsonSerializer.Deserialize<T>(jsonText, _jsonOptions);
            }
            
            // If it's an object, check for error/message properties
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                // Check if it's an error response
                if (doc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    Console.WriteLine($"Error response: {errorElement}");
                    return null;
                }
                
                // Check if it's a message response
                if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    if (messageElement.GetString()?.Contains("No project files found") == true)
                    {
                        // Return an empty list for "no projects" message
                        if (typeof(T) == typeof(List<ProjectPackageAnalysis>))
                        {
                            return (T)(object)new List<ProjectPackageAnalysis>();
                        }
                    }
                    Console.WriteLine($"Message response: {messageElement}");
                    return null;
                }
                
                return JsonSerializer.Deserialize<T>(jsonText, _jsonOptions);
            }
            
            throw new InvalidOperationException($"Unexpected JSON root type: {doc.RootElement.ValueKind}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON deserialization error: {ex.Message}");
            throw;
        }
    }

    [Fact]
    public async Task AnalyzePackagesAsync_WithValidProject_ReturnsAnalysis()
    {
        // Arrange
        var projectContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.4.2"" />
    <PackageReference Include=""Moq"" Version=""4.18.4"" />
  </ItemGroup>
</Project>";
        await CreateTestProjectFileAsync(projectContent);

        // Act
        var result = await _fixture.Client.CallToolAsync("analyze_packages", new Dictionary<string, object?>());
        var analysis = DeserializeResponse<List<ProjectPackageAnalysis>>(result);

        // Assert
        Assert.NotNull(analysis);
        Assert.Single(analysis); // One project
        Assert.Equal(2, analysis[0].Packages.Count); // Two packages
        Assert.Contains(analysis[0].Packages, p => p.PackageId == "xunit");
        Assert.Contains(analysis[0].Packages, p => p.PackageId == "Moq");
    }

    [Fact]
    public async Task AnalyzePackagesAsync_WithEmptyProject_ReturnsEmptyAnalysis()
    {
        // Arrange
        var projectContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
  </ItemGroup>
</Project>";
        await CreateTestProjectFileAsync(projectContent);

        // Act
        var result = await _fixture.Client.CallToolAsync("analyze_packages", new Dictionary<string, object?>());
        var analysis = DeserializeResponse<List<ProjectPackageAnalysis>>(result);

        // Assert
        Assert.NotNull(analysis);
        Assert.Single(analysis);
        Assert.Empty(analysis[0].Packages);
    }

    [Fact]
    public async Task AnalyzePackagesAsync_WithInvalidProject_HandlesError()
    {
        // Arrange
        var projectContent = "invalid xml content";
        await CreateTestProjectFileAsync(projectContent);

        // Act
        var result = await _fixture.Client.CallToolAsync("analyze_packages", new Dictionary<string, object?>());
        var jsonText = result.Content.FirstOrDefault(c => c.Type == "text")?.Text;
        Assert.NotNull(jsonText);
        
        using var doc = JsonDocument.Parse(jsonText);
        // Either an error or a "no projects found" message is acceptable
        Assert.True(
            doc.RootElement.TryGetProperty("error", out _) || 
            (doc.RootElement.TryGetProperty("message", out var msg) && 
             msg.GetString()?.Contains("No project files found") == true),
            $"Expected error or no projects message in response. Actual JSON: {jsonText}");
    }

    [Fact]
    public async Task AnalyzePackagesAsync_WithPreviewVersions_IncludesPreviewUpdates()
    {
        // Arrange
        var projectContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Microsoft.Extensions.Hosting"" Version=""8.0.0"" />
  </ItemGroup>
</Project>";
        await CreateTestProjectFileAsync(projectContent);

        // Act
        var result = await _fixture.Client.CallToolAsync("analyze_packages", 
            new Dictionary<string, object?> { ["includePreviewVersions"] = true });
        var analysis = DeserializeResponse<List<ProjectPackageAnalysis>>(result);

        // Assert
        Assert.NotNull(analysis);
        Assert.Single(analysis);
        var package = analysis[0].Packages.First();
        Assert.Equal("Microsoft.Extensions.Hosting", package.PackageId);
        
        // Either should have an update or a preview update
        Assert.True(package.HasUpdate || package.HasPreviewUpdate);
    }

    [Fact]
    public async Task AnalyzePackagesAsync_WithDependencyGraph_GeneratesGraph()
    {
        // Arrange
        var projectContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""8.0.0"" />
  </ItemGroup>
</Project>";
        await CreateTestProjectFileAsync(projectContent);

        // Act
        var result = await _fixture.Client.CallToolAsync("analyze_packages", new Dictionary<string, object?>());
        var analysis = DeserializeResponse<List<ProjectPackageAnalysis>>(result);

        // Assert
        Assert.NotNull(analysis);
        Assert.Single(analysis);
        var package = analysis[0].Packages.First();
        
        // Verify dependency graph
        Assert.NotNull(package.DependencyGraph);
        Assert.Contains("Microsoft.Extensions.DependencyInjection", package.DependencyGraph);
        Assert.Contains("└─", package.DependencyGraph); // Should contain tree structure characters
    }

    [Fact]
    public async Task AnalyzePackagesAsync_WithTestProject_DetectsImplicitUsage()
    {
        // Arrange
        var projectContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.4.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.4.5"" />
    <PackageReference Include=""coverlet.collector"" Version=""6.0.0"" />
  </ItemGroup>
</Project>";
        await CreateTestProjectFileAsync(projectContent);

        // Create a test file with xUnit attributes
        var testFilePath = Path.Combine(_testProjectDir, "UnitTest1.cs");
        await File.WriteAllTextAsync(testFilePath, @"
using Xunit;
namespace TestProject;
public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}");

        // Act
        var result = await _fixture.Client.CallToolAsync("analyze_packages", new Dictionary<string, object?>());
        var analysis = DeserializeResponse<List<ProjectPackageAnalysis>>(result);

        // Assert
        Assert.NotNull(analysis);
        Assert.Single(analysis);
        
        // Verify test packages are marked as implicitly used
        foreach (var package in analysis[0].Packages)
        {
            Assert.True(package.IsUsed);
            Assert.True(package.ImplicitUsage);
            Assert.NotEmpty(package.UsageLocations);
        }
    }

    [Fact]
    public async Task AnalyzePackagesAsync_WithNoProjects_ReturnsMessage()
    {
        // Arrange - Empty directory
        Directory.Delete(_testProjectDir, true);
        Directory.CreateDirectory(_testProjectDir);

        // Act
        var result = await _fixture.Client.CallToolAsync("analyze_packages", new Dictionary<string, object?>());
        var jsonText = result.Content.FirstOrDefault(c => c.Type == "text")?.Text;
        Assert.NotNull(jsonText);
        
        using var doc = JsonDocument.Parse(jsonText);
        Assert.True(doc.RootElement.TryGetProperty("message", out var messageValue));
        Assert.Contains("No project files found", messageValue.GetString());
    }
}
