using MCPSharp;
using System.Diagnostics;
using System.Text.Json;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
public class NetContextServerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;

    private readonly MCPClient client;

    public NetContextServerTests()
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

        // Setup test directory and files
        _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerTests");
        _testProjectPath = Path.Combine(_testDir, "Test.csproj");
        _testCsFilePath = Path.Combine(_testDir, "Test.cs");

        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_testProjectPath, "<Project />");
        File.WriteAllText(_testCsFilePath, "public class Test { }");

        // Use platform-specific executable name
        string executableName = GetPlatformSpecificExecutableName();
        client = new MCPClient("Test Client", "1.0.0", executableName);
    }

    private string GetPlatformSpecificExecutableName()
    {
        // Check if we're running on Windows or non-Windows
        bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        
        // For Windows, use .exe extension; for Linux/macOS, don't use extension
        string executableName = isWindows ? "NetContextServer.exe" : "NetContextServer";
        
        // Check if the executable exists in the current directory
        string currentDir = Directory.GetCurrentDirectory();
        string executablePath = Path.Combine(currentDir, executableName);
        
        if (File.Exists(executablePath))
        {
            // Use the executable in the current directory
            return executablePath;
        }
        
        // Check if the executable exists in the bin directory (relative to current directory)
        string binDir = Path.Combine(currentDir, "bin");
        if (Directory.Exists(binDir))
        {
            var executableFiles = Directory.GetFiles(binDir, executableName, SearchOption.AllDirectories);
            if (executableFiles.Length > 0)
            {
                return executableFiles[0];
            }
        }
        
        // Fall back to just the executable name and let the system find it
        return executableName;
    }

    [Fact]
    public async Task Test_ListTools()
    {
        var tools = await client.GetToolsAsync();
        Assert.NotNull(tools);
        Assert.True(tools.Count > 0);
        tools.ForEach(tool =>
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
        });
    }

    [Fact]
    public async Task TestPing()
    {
        await client.PingAsync();
    }

    [Fact]
    public async Task TestCallInvalidTool()
    {
        Assert.True((await client.CallToolAsync("NotARealTool")).IsError);
    }

    [Fact]
    public async Task Hello_ReturnsExpectedMessage()
    {
        // Set the base directory for NetContextServer to our test directory
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });

        var result = await client.CallToolAsync("hello");
        Assert.Equal("hello, claude.", result.Content[0].Text);
    }

    [Fact]
    public async Task Echo_ReturnsInputString()
    {
        var input = "test message";
        var result = await client.CallToolAsync("echo", new Dictionary<string, object> { { "input", input } });
        Assert.Equal(input, result.Content[0].Text);
    }

    [Fact]
    public async Task TestCallToolWithInvalidParameters()
    {
        var result = await client.CallToolAsync("Echo", new Dictionary<string, object> { { "invalid_param", "test" } });
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(1, 2, "3")]
    [InlineData(-1, 1, "0")]
    [InlineData(0, 0, "0")]
    public async Task Add_ReturnsSumAsString(int a, int b, string expected)
    {
        var result = await client.CallToolAsync("add", new Dictionary<string, object> 
        { 
            { "a", a },
            { "b", b }
        });
        Assert.Equal(expected, result.Content[0].Text);
    }

    [Fact]
    public async Task AddComplex_ReturnsFormattedString()
    {
        var obj = new Dictionary<string, object>
        {
            { "Name", "Test" },
            { "Age", 25 },
            { "Hobbies", new[] { "Reading", "Coding" } }
        };

        var result = await client.CallToolAsync("add_complex", new Dictionary<string, object> { { "obj", obj } });
        Assert.Equal("Name: Test, Age: 25, Hobbies: Reading, Coding", result.Content[0].Text);
    }

    [Fact]
    public async Task Exception_ThrowsException()
    {
        var result = await client.CallToolAsync("throw_exception");
        Assert.Contains("This is an exception", result.Content[0].Text);
    }

    [Fact]
    public async Task ListProjects_ReturnsJsonArray()
    {
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        
        var result = await client.CallToolAsync("list_projects");
        var projects = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
        
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.EndsWith(".csproj"));
    }

    [Fact]
    public async Task ListFiles_WithValidPath_ReturnsJsonArray()
    {
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        
        var result = await client.CallToolAsync("list_files", new Dictionary<string, object> { { "projectPath", _testDir } });
        var files = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
        
        Assert.NotNull(files);
        Assert.Contains(files, f => f.EndsWith(".cs"));
    }

    [Fact]
    public async Task ListFiles_WithInvalidPath_ReturnsError()
    {
        var invalidPath = Path.Combine(_testDir, "NonExistent");
        var result = await client.CallToolAsync("list_files", new Dictionary<string, object> { { "projectPath", invalidPath } });
        var error = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
        
        Assert.NotNull(error);
        Assert.Contains(error, e => e.StartsWith("Error:"));
    }

    [Fact]
    public async Task SearchCode_FindsMatchingContent()
    {
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        
        // Write test content
        File.WriteAllText(_testCsFilePath, "public class TestSearch { private string test = \"findme\"; }");
        
        var result = await client.CallToolAsync("search_code", new Dictionary<string, object> { { "searchText", "findme" } });
        var matches = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
        
        Assert.NotNull(matches);
        Assert.Contains(matches, m => m.Contains("findme"));
    }

    [Fact]
    public async Task OpenFile_WithValidPath_ReturnsContent()
    {
        var content = "test content";
        File.WriteAllText(_testCsFilePath, content);
        
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("open_file", new Dictionary<string, object> { { "filePath", _testCsFilePath } });
        Assert.Equal(content, result.Content[0].Text);
    }

    [Fact]
    public async Task OpenFile_WithInvalidPath_ReturnsError()
    {
        var invalidPath = Path.Combine(_testDir, "NonExistent.cs");
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("open_file", new Dictionary<string, object> { { "filePath", invalidPath } });
        Assert.StartsWith("Error:", result.Content[0].Text);
    }

    [Fact]
    public async Task OpenFile_WithLargeContent_ReturnsTruncated()
    {
        var largeContent = new string('x', 150_000);
        File.WriteAllText(_testCsFilePath, largeContent);
        
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("open_file", new Dictionary<string, object> { { "filePath", _testCsFilePath } });
        Assert.Contains("[Truncated]", result.Content[0].Text);
        Assert.True(result.Content[0].Text.Length < largeContent.Length);
    }

    [Fact]
    public async Task ListSolutions_ReturnsSolutionFiles()
    {
        // Create a test solution file
        var solutionPath = Path.Combine(_testDir, "Test.sln");
        File.WriteAllText(solutionPath, "");

        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("list_solutions");
        var solutions = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(solutions);
        Assert.Contains(solutions, s => s.EndsWith(".sln"));
    }

    [Fact]
    public async Task ListProjectsInDirectory_WithValidPath_ReturnsProjects()
    {
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("list_projects_in_dir", new Dictionary<string, object> { { "directory", _testDir } });
        var projects = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.EndsWith(".csproj"));
    }

    [Fact]
    public async Task ListProjectsInDirectory_WithInvalidPath_ReturnsError()
    {
        var invalidPath = Path.Combine(_testDir, "NonExistent");
        var result = await client.CallToolAsync("list_projects_in_dir", new Dictionary<string, object> { { "directory", invalidPath } });
        var error = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(error);
        Assert.Contains(error, e => e.StartsWith("Error:"));
    }

    [Fact]
    public async Task ListSourceFiles_WithValidPath_ReturnsSourceFiles()
    {
        // Create test source files
        File.WriteAllText(Path.Combine(_testDir, "Test.cs"), "");
        File.WriteAllText(Path.Combine(_testDir, "Test.vb"), "");
        File.WriteAllText(Path.Combine(_testDir, "Test.fs"), "");

        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("list_source_files", new Dictionary<string, object> { { "projectDir", _testDir } });
        var files = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(files);
        Assert.Contains(files, f => f.EndsWith(".cs"));
        Assert.Contains(files, f => f.EndsWith(".vb"));
        Assert.Contains(files, f => f.EndsWith(".fs"));
    }

    [Fact]
    public async Task ListSourceFiles_WithInvalidPath_ReturnsError()
    {
        var invalidPath = Path.Combine(_testDir, "NonExistent");
        var result = await client.CallToolAsync("list_source_files", new Dictionary<string, object> { { "projectDir", invalidPath } });
        var error = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(error);
        Assert.Contains(error, e => e.StartsWith("Error:"));
    }

    [Fact]
    public async Task AddIgnorePatterns_AddsNewPatterns()
    {
        var patterns = new[] { "*.secret", "password.txt" };
        var result = await client.CallToolAsync("add_ignore_patterns", new Dictionary<string, object> { { "patterns", patterns } });
        var updatedPatterns = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(updatedPatterns);
        Assert.Contains(updatedPatterns, p => p == "*.secret");
        Assert.Contains(updatedPatterns, p => p == "password.txt");
    }

    [Fact]
    public async Task GetIgnorePatterns_ReturnsCurrentPatterns()
    {
        var result = await client.CallToolAsync("get_ignore_patterns");
        var patterns = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(patterns);
        Assert.Contains(patterns, p => p == "*.env");
        Assert.Contains(patterns, p => p == "*.pfx");
    }

    [Fact]
    public async Task ClearIgnorePatterns_RemovesAllPatterns()
    {
        var result = await client.CallToolAsync("clear_ignore_patterns");
        var patterns = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(patterns);
        Assert.Empty(patterns);
    }

    [Fact]
    public async Task ListFiles_IgnoresSensitiveFiles()
    {
        // Create test files
        var testCsPath = Path.Combine(_testDir, "test.cs");
        var envPath = Path.Combine(_testDir, "secrets.env");
        var configPath = Path.Combine(_testDir, "appsettings.Production.json");

        File.WriteAllText(testCsPath, "");
        File.WriteAllText(envPath, "");
        File.WriteAllText(configPath, "");

        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("list_files", new Dictionary<string, object> { { "projectPath", _testDir } });
        var files = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(files);
        Assert.Single(files); // Should only contain the .cs file
        Assert.Equal(testCsPath, files[0], ignoreCase: true);
    }

    [Fact]
    public async Task OpenFile_BlocksAccessOutsideBaseDirectory()
    {
        var outsidePath = Path.Combine(Path.GetTempPath(), "outside.txt");
        File.WriteAllText(outsidePath, "secret content");

        var result = await client.CallToolAsync("open_file", new Dictionary<string, object> { { "filePath", outsidePath } });
        Assert.Contains("Error: Access to this file is not allowed", result.Content[0].Text);
    }

    [Fact]
    public async Task OpenFile_BlocksSensitiveFiles()
    {
        var secretFile = Path.Combine(_testDir, "secrets.env");
        File.WriteAllText(secretFile, "secret content");

        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("open_file", new Dictionary<string, object> { { "filePath", secretFile } });
        Assert.Contains("Error: This file type is restricted", result.Content[0].Text);
    }

    [SkippableFact]
    public async Task SemanticSearch_WithValidCredentials_ReturnsResults()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        
        Skip.If(string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key), 
            "Skipping test because Azure OpenAI credentials are not available");

        // Create a test file with some content
        File.WriteAllText(_testCsFilePath, "public class Test { public void HandleAuthentication() { } }");
        
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("semantic_search", new Dictionary<string, object> 
        { 
            { "query", "authentication method" },
            { "topK", 1 }
        });

        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result.Content[0].Text);
        Assert.NotNull(response);
        Assert.NotNull(response["Results"]);
    }

    [SkippableFact]
    public async Task SemanticSearch_ReturnsResultsOptimizedForAI()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        
        Skip.If(string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key), 
            "Skipping test because Azure OpenAI credentials are not available");

        // Create test files with various code structures
        var testFile1 = Path.Combine(_testDir, "TestClass.cs");
        var testFile2 = Path.Combine(_testDir, "TestMethod.cs");
        var testFile3 = Path.Combine(_testDir, "TestNamespace.cs");
        
        // File with class definition
        File.WriteAllText(testFile1, @"
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
}");

        // File with method containing specific keyword
        File.WriteAllText(testFile2, @"
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
}");

        // File with namespace and multiple classes
        File.WriteAllText(testFile3, @"
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
}");
        
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        
        // Search for "hello" which should find the method in TestMethod.cs
        var result = await client.CallToolAsync("semantic_search", new Dictionary<string, object> 
        { 
            { "query", "hello" },
            { "topK", 3 }
        });

        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result.Content[0].Text);
        Assert.NotNull(response);
        
        var resultsJson = System.Text.Json.JsonSerializer.Serialize(response["Results"]);
        var results = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(resultsJson);
        
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        
        // Verify the results contain the expected properties
        foreach (var item in results)
        {
            // Check that each result has the required properties
            Assert.True(item.ContainsKey("FilePath"));
            Assert.True(item.ContainsKey("StartLine"));
            Assert.True(item.ContainsKey("EndLine"));
            Assert.True(item.ContainsKey("Content"));
            Assert.True(item.ContainsKey("Score"));
            Assert.True(item.ContainsKey("ParentScope"));
            
            // Verify content is not empty
            Assert.NotEmpty(item["Content"]?.ToString() ?? string.Empty);
            
            // Verify score is a positive number
            var scoreElement = (JsonElement)item["Score"];
            var score = scoreElement.GetDouble();
            Assert.True(score > 0);
            
            // Verify content doesn't contain unnecessary whitespace at start/end
            var content = item["Content"]?.ToString() ?? string.Empty;
            Assert.Equal(content.Trim(), content);
            
            // Verify content doesn't contain using statements
            Assert.DoesNotContain("using ", content);
            
            // AI optimization checks
            // 1. Content should be concise (not too long)
            Assert.True(content.Length < 2000, "Content should be concise for AI processing");
            
            // 2. Content should not have excessive blank lines
            var lines = content.Split('\n');
            var blankLineCount = lines.Count(l => string.IsNullOrWhiteSpace(l));
            var blankLinePercentage = (double)blankLineCount / lines.Length;
            
            // Output diagnostic information
            Console.WriteLine($"Content length: {content.Length}");
            Console.WriteLine($"Total lines: {lines.Length}");
            Console.WriteLine($"Blank lines: {blankLineCount}");
            Console.WriteLine($"Blank line percentage: {blankLinePercentage:P}");
            Console.WriteLine($"Threshold: {1.0/3:P}");
            Console.WriteLine($"Content: {content}");
            
            // Modified assertion to handle edge cases where there are very few lines
            // If there are no blank lines, the test should pass regardless of total line count
            Assert.True(blankLineCount == 0 || blankLineCount < Math.Max(1, lines.Length / 3), 
                $"Content should not have excessive blank lines. Found {blankLineCount} blank lines out of {lines.Length} total lines ({blankLinePercentage:P})");
            
            // 3. Check that parent scope is provided
            Assert.NotEmpty(item["ParentScope"]?.ToString() ?? string.Empty);
            
            // 4. Check that the content is properly formatted code (contains braces, indentation)
            Assert.Contains("{", content);
            
            // 5. Check that the score is presented as a percentage (0-100 range)
            Assert.True(score <= 100, "Score should be in percentage format (0-100)");
        }
        
        // Check if at least one result contains our "hello" keyword or is from TestMethod.cs
        var hasRelevantResult = results.Any(r => 
            (r["Content"]?.ToString() ?? string.Empty).Contains("hello", StringComparison.OrdinalIgnoreCase) || 
            (r["FilePath"]?.ToString() ?? string.Empty).Contains("TestMethod.cs"));
        Assert.True(hasRelevantResult, "Results should include relevant content matching the query");
    }

    public void Dispose()
    {
        // Reset the base directory
        try
        {
            NetConextServer.SetBaseDirectory(Directory.GetCurrentDirectory());
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
} 