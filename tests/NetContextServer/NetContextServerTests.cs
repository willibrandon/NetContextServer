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

        var executableName = OperatingSystem.IsWindows() ? "NetContextServer.exe" : "NetContextServer";
        client = new MCPClient("Test Client", "1.0.0", executableName);
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

    [SkippableFact]
    [Trait("Category", "AI_Generated")]
    public async Task SemanticSearch_ReturnsSemanticallySimilarResults()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        
        Skip.If(string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key), 
            "Skipping test because Azure OpenAI credentials are not available");

        // Create test files with semantically related but textually different content
        var testFiles = new Dictionary<string, string>
        {
            { "Authentication.cs", @"
namespace Security {
    public class Authentication {
        public bool ValidateUserCredentials(string username, string password) {
            // Check if username and password are valid
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
                return false;
            }
            
            // In a real system, this would check against a database
            return username == ""admin"" && password == ""secure123"";
        }
    }
}" },
            { "Security.cs", @"
namespace Access {
    public class Security {
        public bool CheckAccess(string userId, string resource) {
            // Verify if the user has access to the requested resource
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(resource)) {
                return false;
            }
            
            // In a real system, this would check permissions in a database
            return userId == ""admin"" || resource.StartsWith(""public"");
        }
    }
}" },
            { "UserManagement.cs", @"
namespace Users {
    public class UserManagement {
        public bool Login(string username, string password) {
            // Authenticate user login
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
                return false;
            }
            
            // Validate credentials (simplified for testing)
            return username.Length > 3 && password.Length > 6;
        }
    }
}" },
            { "Logging.cs", @"
namespace System {
    public class Logging {
        public void LogError(string message, Exception? exception = null) {
            // Log error message and exception details
            var timestamp = DateTime.Now.ToString(""yyyy-MM-dd HH:mm:ss"");
            var logEntry = $""[ERROR] {timestamp}: {message}"";
            
            if (exception != null) {
                logEntry += $""\nException: {exception.Message}\n{exception.StackTrace}"";
            }
            
            // In a real system, this would write to a log file or database
            Console.Error.WriteLine(logEntry);
        }
    }
}" }
        };

        // Create the test files
        foreach (var file in testFiles)
        {
            File.WriteAllText(Path.Combine(_testDir, file.Key), file.Value);
        }

        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        
        // Dictionary of queries and expected files that should be returned
        var queryTests = new Dictionary<string, string[]>
        {
            { "how to verify user login", new[] { "Authentication.cs", "UserManagement.cs" } },
            { "check user permissions", new[] { "Security.cs", "UserManagement.cs" } },
            { "handle authentication errors", new[] { "Authentication.cs", "Logging.cs", "UserManagement.cs" } }
        };
        
        foreach (var queryTest in queryTests)
        {
            var query = queryTest.Key;
            var expectedFiles = queryTest.Value;
            
            Console.WriteLine($"Testing query: {query}");
            Console.WriteLine($"Expected files: {string.Join(", ", expectedFiles)}");
            
            var result = await client.CallToolAsync("semantic_search", new Dictionary<string, object> 
            { 
                { "query", query },
                { "topK", 3 }
            });

            var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result.Content[0].Text);
            Assert.NotNull(response);
            
            var resultsJson = System.Text.Json.JsonSerializer.Serialize(response["Results"]);
            var results = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(resultsJson);
            
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            
            // Log the found files and their scores
            var foundFiles = results.Select(r => Path.GetFileName(r["FilePath"]?.ToString() ?? string.Empty)).ToArray();
            var scores = results.Select(r => ((JsonElement)r["Score"]).GetDouble()).ToArray();
            
            Console.WriteLine($"Found files: {string.Join(", ", foundFiles)}");
            Console.WriteLine($"Scores: {string.Join(", ", scores)}");
            
            // Verify that each expected file is found in the results
            foreach (var expectedFile in expectedFiles)
            {
                Assert.Contains(results, r => 
                    Path.GetFileName(r["FilePath"]?.ToString() ?? string.Empty)
                        .Equals(expectedFile, StringComparison.OrdinalIgnoreCase));
            }
            
            // Verify that the results are in descending order by score
            for (int i = 1; i < scores.Length; i++)
            {
                Assert.True(scores[i - 1] >= scores[i], 
                    $"Results should be in descending order by score. Score at position {i-1} ({scores[i-1]}) should be >= score at position {i} ({scores[i]})");
            }
            
            // Verify that each result has a parent scope that is relevant to the domain
            foreach (var resultItem in results)
            {
                var parentScope = resultItem["ParentScope"]?.ToString() ?? string.Empty;
                Assert.True(
                    parentScope.Contains("Authentication", StringComparison.OrdinalIgnoreCase) ||
                    parentScope.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
                    parentScope.Contains("User", StringComparison.OrdinalIgnoreCase) ||
                    parentScope.Contains("Login", StringComparison.OrdinalIgnoreCase) ||
                    parentScope.Contains("Log", StringComparison.OrdinalIgnoreCase) ||
                    parentScope.Contains("Validate", StringComparison.OrdinalIgnoreCase) ||
                    parentScope.Contains("Check", StringComparison.OrdinalIgnoreCase) ||
                    parentScope.Contains("Access", StringComparison.OrdinalIgnoreCase),
                    $"Parent scope should be relevant to the domain. Got: {parentScope}"
                );
            }
        }
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public async Task ListFiles_ShouldFindAllNetFileTypes()
    {
        // Create test files of different types
        var testFiles = new Dictionary<string, string>
        {
            { "Test.cs", "public class Test { }" },
            { "Test.vb", "Public Class Test\nEnd Class" },
            { "Test.fs", "module Test" },
            { "Test.fsx", "let x = 42" },
            { "Test.fsi", "module Test" },
            { "Test.cshtml", "@page" },
            { "Test.vbhtml", "@Code End Code" },
            { "Test.razor", "@page \"/test\"" },
            { "NotADotNetFile.txt", "Hello" }
        };

        foreach (var file in testFiles)
        {
            File.WriteAllText(Path.Combine(_testDir, file.Key), file.Value);
        }

        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("list_files", new Dictionary<string, object> { { "projectPath", _testDir } });
        var files = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(files);
        // Should find all .NET files but not the .txt file
        Assert.Equal(8, files!.Length);
        Assert.Contains(files, f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".fs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".fsi", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".vbhtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public async Task SearchCode_ShouldSearchAllNetFileTypes()
    {
        // Create test files with a common searchable term
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
            File.WriteAllText(Path.Combine(_testDir, file.Key), file.Value);
        }

        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("search_code", new Dictionary<string, object> { { "searchText", "SearchableTest" } });
        var matches = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

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

    [Fact]
    [Trait("Category", "AI_Generated")]
    public async Task ListSourceFiles_ShouldFindAllNetSourceFiles()
    {
        // Create test files in a project directory structure
        var projectDir = Path.Combine(_testDir, "TestProject");
        Directory.CreateDirectory(projectDir);
        
        var testFiles = new Dictionary<string, string>
        {
            { "Test.cs", "public class Test { }" },
            { "Test.vb", "Public Class Test\nEnd Class" },
            { "Test.fs", "module Test" },
            { "Test.fsx", "let x = 42" },
            { "Test.fsi", "module Test" },
            { "Test.cshtml", "@page" },
            { "Test.vbhtml", "@Code End Code" },
            { "Test.razor", "@page \"/test\"" },
            { "NotADotNetFile.txt", "Hello" }
        };

        foreach (var file in testFiles)
        {
            File.WriteAllText(Path.Combine(projectDir, file.Key), file.Value);
        }

        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("list_source_files", new Dictionary<string, object> { { "projectDir", projectDir } });
        var files = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(files);
        // Should find all .NET files but not the .txt file
        Assert.Equal(8, files!.Length);
        Assert.Contains(files, f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".fs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".fsi", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".vbhtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
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
} 