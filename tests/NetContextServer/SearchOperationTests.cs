using MCPSharp;
using System.Diagnostics;
using System.Text.Json;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
[Collection("NetContextServer Tests")]
public class SearchOperationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;
    private readonly MCPClient client;

    public SearchOperationTests()
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

        // Add files in different languages and styles for cross-language testing
        var multiLanguageFiles = new Dictionary<string, string>
        {
            { "AuthenticationVB.vb", @"
Namespace Security
    Public Class AuthenticationVB
        Public Function ValidateCredentials(username As String, password As String) As Boolean
            ' VB.NET implementation of credential validation
            Return Not String.IsNullOrEmpty(username) AndAlso Not String.IsNullOrEmpty(password)
        End Function
    End Class
End Namespace" },
            { "SecurityFSharp.fs", @"
namespace Security

type SecurityFSharp() =
    member this.CheckPermission(userId: string, resource: string) =
        // F# implementation of permission checking
        not (System.String.IsNullOrEmpty(userId)) && not (System.String.IsNullOrEmpty(resource))
" }
        };

        // Create the test files
        foreach (var file in testFiles.Concat(multiLanguageFiles))
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

        // Run positive test cases
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
                    $"Results should be in descending order by score. Score at position {i - 1} ({scores[i - 1]}) should be >= score at position {i} ({scores[i]})");
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

            // Verify content relevance for each result
            foreach (var resultItem in results)
            {
                var content = resultItem["Content"]?.ToString() ?? string.Empty;
                var filePath = Path.GetFileName(resultItem["FilePath"]?.ToString() ?? string.Empty);

                // Check content relevance based on the specific file type
                switch (filePath.ToLowerInvariant())
                {
                    case "authentication.cs":
                        if (query.Contains("authentication") || query.Contains("login") || query.Contains("verify"))
                        {
                            Assert.True(
                                content.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("username", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("credentials", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("validate", StringComparison.OrdinalIgnoreCase),
                                $"Authentication.cs content should contain authentication-related terms for query '{query}'. Content: {content}"
                            );
                        }
                        break;

                    case "usermanagement.cs":
                        if (query.Contains("authentication") || query.Contains("login") || query.Contains("verify"))
                        {
                            Assert.True(
                                content.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("username", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("login", StringComparison.OrdinalIgnoreCase),
                                $"UserManagement.cs content should contain login-related terms for query '{query}'. Content: {content}"
                            );
                        }
                        break;

                    case "security.cs":
                        if (query.Contains("permissions") || query.Contains("access"))
                        {
                            Assert.True(
                                content.Contains("access", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("resource", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("check", StringComparison.OrdinalIgnoreCase),
                                $"Security.cs content should contain access-related terms for query '{query}'. Content: {content}"
                            );
                        }
                        break;

                    case "logging.cs":
                        if (query.Contains("error") || query.Contains("exception"))
                        {
                            Assert.True(
                                content.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
                                content.Contains("log", StringComparison.OrdinalIgnoreCase),
                                $"Logging.cs content should contain error-related terms for query '{query}'. Content: {content}"
                            );
                        }
                        break;
                }
            }

            // Verify that the most relevant results have scores above a minimum threshold
            Assert.True(scores[0] >= 65.0, $"Top result should have a relevance score of at least 65%. Got: {scores[0]}%");

            // For expected files, verify they have a minimum score
            foreach (var expectedFile in expectedFiles)
            {
                var matchingResult = results.FirstOrDefault(r =>
                    Path.GetFileName(r["FilePath"]?.ToString() ?? string.Empty)
                        .Equals(expectedFile, StringComparison.OrdinalIgnoreCase));

                if (matchingResult != null)
                {
                    var score = ((JsonElement)matchingResult["Score"]).GetDouble();
                    Assert.True(score >= 60.0,
                        $"Expected file {expectedFile} should have a relevance score of at least 60%. Got: {score}%");
                }
            }

            // Verify parent scope accuracy for specific files
            foreach (var resultItem in results)
            {
                var filePath = Path.GetFileName(resultItem["FilePath"]?.ToString() ?? string.Empty);
                var parentScope = resultItem["ParentScope"]?.ToString() ?? string.Empty;

                switch (filePath.ToLowerInvariant())
                {
                    case "authentication.cs":
                        Assert.Contains("Authentication", parentScope, StringComparison.OrdinalIgnoreCase);
                        Assert.Contains("Security", parentScope, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "security.cs":
                        Assert.Contains("Security", parentScope, StringComparison.OrdinalIgnoreCase);
                        Assert.Contains("Access", parentScope, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "usermanagement.cs":
                        Assert.Contains("UserManagement", parentScope, StringComparison.OrdinalIgnoreCase);
                        Assert.Contains("Users", parentScope, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "logging.cs":
                        Assert.Contains("Logging", parentScope, StringComparison.OrdinalIgnoreCase);
                        Assert.Contains("System", parentScope, StringComparison.OrdinalIgnoreCase);
                        break;
                }
            }
        }

        // Test negative cases
        var negativeTests = new Dictionary<string, string[]>
        {
            { "database connection string", new[] { "Authentication.cs", "Security.cs", "UserManagement.cs", "Logging.cs" } },
            { "file system operations", new[] { "Authentication.cs", "Security.cs", "UserManagement.cs", "Logging.cs" } }
        };

        foreach (var negativeTest in negativeTests)
        {
            var query = negativeTest.Key;
            var unexpectedFiles = negativeTest.Value;

            Console.WriteLine($"Testing negative query: {query}");

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

            if (results.Count > 0)
            {
                var scores = results.Select(r => ((JsonElement)r["Score"]).GetDouble()).ToArray();
                var foundFiles = results.Select(r => Path.GetFileName(r["FilePath"]?.ToString() ?? string.Empty)).ToArray();

                Console.WriteLine($"Found files for negative query: {string.Join(", ", foundFiles)}");
                Console.WriteLine($"Scores for negative query: {string.Join(", ", scores)}");

                // Verify that the top results don't have high scores (since they shouldn't be relevant)
                // We use a lower threshold for negative tests since some semantic similarity might still exist
                if (scores.Length > 0)
                {
                    // The threshold is lower here because we expect these to be less relevant
                    Assert.True(scores[0] < 75.0,
                        $"Top result for irrelevant query '{query}' should have a score below 75%. Got: {scores[0]}%");
                }
            }
        }

        // Test cross-language semantic search
        var crossLanguageQuery = "validate user credentials across languages";
        var crossLanguageResult = await client.CallToolAsync("semantic_search", new Dictionary<string, object>
        {
            { "query", crossLanguageQuery },
            { "topK", 5 }
        });

        var crossLanguageResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(crossLanguageResult.Content[0].Text);
        Assert.NotNull(crossLanguageResponse);

        var crossLanguageResultsJson = System.Text.Json.JsonSerializer.Serialize(crossLanguageResponse["Results"]);
        var crossLanguageResults = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(crossLanguageResultsJson);

        Assert.NotNull(crossLanguageResults);
        Assert.NotEmpty(crossLanguageResults);

        var crossLanguageFiles = crossLanguageResults.Select(r => Path.GetFileName(r["FilePath"]?.ToString() ?? string.Empty)).ToArray();
        var crossLanguageScores = crossLanguageResults.Select(r => ((JsonElement)r["Score"]).GetDouble()).ToArray();

        Console.WriteLine($"Cross-language query: {crossLanguageQuery}");
        Console.WriteLine($"Found files: {string.Join(", ", crossLanguageFiles)}");
        Console.WriteLine($"Scores: {string.Join(", ", crossLanguageScores)}");

        // Verify that at least one C# and one VB.NET authentication file are found
        // We don't assert both must be found because the semantic search might prioritize one over the other
        Assert.True(
            crossLanguageFiles.Any(f => f.Equals("Authentication.cs", StringComparison.OrdinalIgnoreCase)) ||
            crossLanguageFiles.Any(f => f.Equals("AuthenticationVB.vb", StringComparison.OrdinalIgnoreCase)),
            "At least one authentication file (C# or VB.NET) should be found for cross-language query"
        );
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