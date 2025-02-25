using System.Text.Json;
using MCPSharp.Example;

namespace MCPSharp.Test;

[Trait("Category", "AI_Generated")]
public class MCPDevTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;

    public MCPDevTests()
    {
        // Setup test directory and files
        _testDir = Path.Combine(Path.GetTempPath(), "MCPDevTests");
        _testProjectPath = Path.Combine(_testDir, "Test.csproj");
        _testCsFilePath = Path.Combine(_testDir, "Test.cs");

        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_testProjectPath, "<Project />");
        File.WriteAllText(_testCsFilePath, "public class Test { }");

        // Set the base directory for MCPDev to our test directory
        MCPDev.SetBaseDirectory(_testDir);
    }

    [Fact(DisplayName = "Hello_ReturnsExpectedMessage")]
    public void Hello_ReturnsExpectedMessage()
    {
        var result = MCPDev.Hello();
        Assert.Equal("hello, claude.", result);
    }

    [Fact(DisplayName = "Echo_ReturnsInputString")]
    public void Echo_ReturnsInputString()
    {
        var input = "test message";
        var result = MCPDev.Echo(input);
        Assert.Equal(input, result);
    }

    [Theory(DisplayName = "Add_ReturnsSumAsString")]
    [InlineData(1, 2, "3")]
    [InlineData(-1, 1, "0")]
    [InlineData(0, 0, "0")]
    public void Add_ReturnsSumAsString(int a, int b, string expected)
    {
        var result = MCPDev.Add(a, b);
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "AddComplex_ReturnsFormattedString")]
    public void AddComplex_ReturnsFormattedString()
    {
        var obj = new ComplicatedObject
        {
            Name = "Test",
            Age = 25,
            Hobbies = ["Reading", "Coding"]
        };

        var result = MCPDev.AddComplex(obj);
        Assert.Equal("Name: Test, Age: 25, Hobbies: Reading, Coding", result);
    }

    [Fact(DisplayName = "Exception_ThrowsException")]
    public void Exception_ThrowsException()
    {
        var ex = Assert.Throws<Exception>(() => MCPDev.Exception());
        Assert.Equal("This is an exception", ex.Message);
    }

    [Fact(DisplayName = "ListProjects_ReturnsJsonArray")]
    public void ListProjects_ReturnsJsonArray()
    {
        var result = MCPDev.ListProjects();
        var projects = JsonSerializer.Deserialize<string[]>(result);
        
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.EndsWith(".csproj"));
    }

    [Fact(DisplayName = "ListFiles_WithValidPath_ReturnsJsonArray")]
    public void ListFiles_WithValidPath_ReturnsJsonArray()
    {
        var result = MCPDev.ListFiles(_testDir);
        var files = JsonSerializer.Deserialize<string[]>(result);
        
        Assert.NotNull(files);
        Assert.Contains(files, f => f.EndsWith(".cs"));
    }

    [Fact(DisplayName = "ListFiles_WithInvalidPath_ReturnsError")]
    public void ListFiles_WithInvalidPath_ReturnsError()
    {
        var result = MCPDev.ListFiles(Path.Combine(_testDir, "NonExistent"));
        var error = JsonSerializer.Deserialize<string[]>(result);
        
        Assert.NotNull(error);
        Assert.Contains(error, e => e.StartsWith("Error:"));
    }

    [Fact(DisplayName = "SearchCode_FindsMatchingContent")]
    public void SearchCode_FindsMatchingContent()
    {
        // Write test content
        File.WriteAllText(_testCsFilePath, "public class TestSearch { private string test = \"findme\"; }");
        
        var result = MCPDev.SearchCode("findme");
        var matches = JsonSerializer.Deserialize<string[]>(result);
        
        Assert.NotNull(matches);
        Assert.Contains(matches, m => m.Contains("findme"));
    }

    [Fact(DisplayName = "OpenFile_WithValidPath_ReturnsContent")]
    public void OpenFile_WithValidPath_ReturnsContent()
    {
        var content = "test content";
        File.WriteAllText(_testCsFilePath, content);
        
        var result = MCPDev.OpenFile(_testCsFilePath);
        Assert.Equal(content, result);
    }

    [Fact(DisplayName = "OpenFile_WithInvalidPath_ReturnsError")]
    public void OpenFile_WithInvalidPath_ReturnsError()
    {
        var result = MCPDev.OpenFile(Path.Combine(_testDir, "NonExistent.cs"));
        Assert.StartsWith("Error:", result);
    }

    [Fact(DisplayName = "OpenFile_WithLargeContent_ReturnsTruncated")]
    public void OpenFile_WithLargeContent_ReturnsTruncated()
    {
        var largeContent = new string('x', 150_000);
        File.WriteAllText(_testCsFilePath, largeContent);
        
        var result = MCPDev.OpenFile(_testCsFilePath);
        Assert.Contains("[Truncated]", result);
        Assert.True(result.Length < largeContent.Length);
    }

    public void Dispose()
    {
        // Reset the base directory
        MCPDev.SetBaseDirectory(Directory.GetCurrentDirectory());

        // Cleanup test directory
        try
        {
            Directory.Delete(_testDir, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
} 