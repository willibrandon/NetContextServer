using ModelContextProtocol.Client;
using System.Text.Json;
using Xunit;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class SearchOperationTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;
    private readonly IMcpClient _client;

    public SearchOperationTests(NetContextServerFixture fixture)
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
    public async Task SearchCode_FindsMatchingContent()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        await File.WriteAllTextAsync(_testCsFilePath, 
            "public class TestSearch { private string test = \"findme\"; }");

        // Act
        var result = await _client.CallToolAsync("search_code", 
            new Dictionary<string, object> { ["searchText"] = "findme" });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var matches = JsonSerializer.Deserialize<string[]>(content.Text);
        Assert.NotNull(matches);
        Assert.Contains(matches, m => m.Contains("findme"));
    }

    [SkippableFact]
    public async Task SemanticSearch_WithValidCredentials_ReturnsResults()
    {
        // Arrange
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        Skip.If(string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key),
            "Skipping test because Azure OpenAI credentials are not available");

        await File.WriteAllTextAsync(_testCsFilePath, 
            "public class Test { public void HandleAuthentication() { } }");

        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("semantic_search", new Dictionary<string, object>
        {
            ["query"] = "authentication method",
            ["topK"] = 1
        });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonSerializer.Deserialize<SemanticSearchResponse>(content.Text);
        Assert.NotNull(response);
        Assert.NotEmpty(response.Results);
    }

    [Fact]
    public async Task SearchCode_ShouldSearchAllNetFileTypes()
    {
        // Arrange: Create test files with a common searchable term
        var testFiles = new Dictionary<string, string>
        {
            { "Test.cs", "public class SearchableTest { }" },
            { "Test.vb", "Public Class SearchableTest\nEnd Class" },
            { "Test.fs", "type SearchableTest = class end" },
            { "Test.fsx", "// SearchableTest script" },
            { "Test.fsi", "type SearchableTest = class end" },
            { "Test.cshtml", "@* SearchableTest view *@" },
            { "Test.vbhtml", "@* SearchableTest view *@" },
            { "Test.razor", "@* SearchableTest component *@" },
            { "NotADotNetFile.txt", "SearchableTest in txt file" }
        };

        foreach (var file in testFiles)
        {
            await File.WriteAllTextAsync(Path.Combine(_testDir, file.Key), file.Value);
        }

        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("search_code", 
            new Dictionary<string, object> { ["searchText"] = "SearchableTest" });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var matches = JsonSerializer.Deserialize<string[]>(content.Text);
        Assert.NotNull(matches);

        // Should find "SearchableTest" in all .NET files but not in the .txt file
        Assert.Equal(8, matches!.Count(m => m.Contains("SearchableTest")));
        Assert.Contains(matches, m => m.Contains(".cs"));
        Assert.Contains(matches, m => m.Contains(".vb"));
        Assert.Contains(matches, m => m.Contains(".fs"));
        Assert.Contains(matches, m => m.Contains(".fsx"));
        Assert.Contains(matches, m => m.Contains(".fsi"));
        Assert.Contains(matches, m => m.Contains(".cshtml"));
        Assert.Contains(matches, m => m.Contains(".vbhtml"));
        Assert.Contains(matches, m => m.Contains(".razor"));
        Assert.DoesNotContain(matches, m => m.Contains(".txt"));
    }

    [SkippableFact]
    public async Task SemanticSearch_ReturnsResultsOptimizedForAI()
    {
        // Arrange
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        Skip.If(string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key),
            "Skipping test because Azure OpenAI credentials are not available");

        Console.WriteLine($"Azure OpenAI Endpoint: {endpoint?.Substring(0, Math.Min(10, endpoint.Length))}...");
        Console.WriteLine($"Azure OpenAI Key: {(string.IsNullOrEmpty(key) ? "Not Set" : "Set")}");

        // Create test files with various code structures
        var testFiles = new Dictionary<string, string>
        {
            { "TestClass.cs", @"
namespace TestNamespace {
    public class TestClass 
    {
        private string _name;
        
        public TestClass(string name) 
        {
            _name = name;
        }
        
        public string GetName() 
        {
            return _name;
        }
    }
}" },
            { "TestMethod.cs", @"
public class HelperMethods 
{
    public void ProcessData() 
    {
        // This method processes data
        var data = GetData();
        TransformData(data);
    }
    
    private string[] GetData() 
    {
        return new[] { ""hello"", ""world"" };
    }
    
    private void TransformData(string[] data) 
    {
        // Transform the data
    }
}" },
            { "TestNamespace.cs", @"
namespace TestProject 
{
    public interface IProcessor 
    {
        void Process();
    }
    
    public class DataProcessor : IProcessor 
    {
        public void Process() 
        {
            // Implementation
        }
    }
}" }
        };

        foreach (var file in testFiles)
        {
            var path = Path.Combine(_testDir, file.Key);
            await File.WriteAllTextAsync(path, file.Value);
            Console.WriteLine($"Created test file: {path}");
        }

        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        Console.WriteLine($"Set base directory to: {_testDir}");

        // Act: Search for "hello" which should find the method in TestMethod.cs
        var result = await _client.CallToolAsync("semantic_search", new Dictionary<string, object>
        {
            ["query"] = "hello",
            ["topK"] = 3
        });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        Console.WriteLine($"Raw response from semantic search: {content.Text}");

        var response = JsonSerializer.Deserialize<SemanticSearchResponse>(content.Text);
        Assert.NotNull(response);
        Assert.NotEmpty(response.Results);

        // Verify the results contain the expected properties
        foreach (var item in response.Results)
        {
            // Verify content is not empty
            Assert.NotEmpty(item.Content);

            // Verify score is a positive number
            Assert.True(item.Score > 0);

            // Verify content doesn't contain unnecessary whitespace at start/end
            Assert.Equal(item.Content.Trim(), item.Content);

            // Verify content doesn't contain using statements
            Assert.DoesNotContain("using ", item.Content);
        }
    }

    private class SemanticSearchResponse
    {
        public SemanticSearchResult[] Results { get; set; } = Array.Empty<SemanticSearchResult>();
    }

    private class SemanticSearchResult
    {
        public string FilePath { get; set; } = string.Empty;
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
        public string ParentScope { get; set; } = string.Empty;
    }
}