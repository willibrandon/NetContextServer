using NetContextServer.Core;
using System.IO;
using System.Reflection;
using MCPSharp;

namespace NetContextServer.Tests;

public class McpToolsTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ProjectIndex _testIndex;
    private readonly McpTools _tools;

    public McpToolsTests()
    {
        // Create a test directory structure
        _testDirectory = Path.Combine(Path.GetTempPath(), "NetContextServerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Create a test project file
        var projectDir = Path.Combine(_testDirectory, "TestProject");
        Directory.CreateDirectory(projectDir);
        var projectPath = Path.Combine(projectDir, "TestProject.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        // Create a test source file
        var sourceDir = Path.Combine(projectDir, "src");
        Directory.CreateDirectory(sourceDir);
        var sourcePath = Path.Combine(sourceDir, "TestClass.cs");
        File.WriteAllText(sourcePath, "namespace TestProject { public class TestClass { public void TestMethod() { } } }");

        // Create a test index
        _testIndex = new ProjectIndex(_testDirectory);
        _testIndex.ProjectPaths.Add(projectPath);
        _testIndex.FilesByProject[projectPath] = new List<string> { sourcePath };

        // Create the tools
        _tools = new McpTools(_testIndex);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ListProjects_ReturnsAllProjects()
    {
        // Act
        var projects = _tools.ListProjects();

        // Assert
        Assert.Single(projects);
        Assert.Contains(projects, p => p.EndsWith("TestProject.csproj"));
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ListFiles_WithValidProject_ReturnsProjectFiles()
    {
        // Arrange
        var projectPath = _testIndex.ProjectPaths[0];

        // Act
        var files = _tools.ListFiles(projectPath);

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("TestClass.cs"));
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ListFiles_WithInvalidProject_ReturnsEmptyList()
    {
        // Act
        var files = _tools.ListFiles("NonExistentProject.csproj");

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void OpenFile_WithValidFile_ReturnsFileContents()
    {
        // Arrange
        var filePath = _testIndex.FilesByProject[_testIndex.ProjectPaths[0]][0];

        // Act
        var content = _tools.OpenFile(filePath);

        // Assert
        Assert.Contains("namespace TestProject", content);
        Assert.Contains("public class TestClass", content);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void OpenFile_WithInvalidFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => _tools.OpenFile("NonExistentFile.cs"));
        Assert.Equal("File not found", exception.Message);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void SearchCode_WithMatchingKeyword_ReturnsResults()
    {
        // Act
        var results = _tools.SearchCode("TestClass");

        // Assert
        Assert.Single(results);
        Assert.Contains(results, r => r.LineText.Contains("public class TestClass"));
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void SearchCode_WithNonMatchingKeyword_ReturnsEmptyList()
    {
        // Act
        var results = _tools.SearchCode("NonExistentKeyword");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void McpTools_HasCorrectAttributes()
    {
        // Verify the class has the McpTool attribute
        var classAttributes = typeof(McpTools).GetCustomAttributes(typeof(McpToolAttribute), false);
        Assert.Single(classAttributes);
        
        // Verify each method has the McpFunction attribute
        var listProjectsMethod = typeof(McpTools).GetMethod("ListProjects");
        var listProjectsAttributes = listProjectsMethod.GetCustomAttributes(typeof(McpFunctionAttribute), false);
        Assert.Single(listProjectsAttributes);
        
        var listFilesMethod = typeof(McpTools).GetMethod("ListFiles");
        var listFilesAttributes = listFilesMethod.GetCustomAttributes(typeof(McpFunctionAttribute), false);
        Assert.Single(listFilesAttributes);
        
        var openFileMethod = typeof(McpTools).GetMethod("OpenFile");
        var openFileAttributes = openFileMethod.GetCustomAttributes(typeof(McpFunctionAttribute), false);
        Assert.Single(openFileAttributes);
        
        var searchCodeMethod = typeof(McpTools).GetMethod("SearchCode");
        var searchCodeAttributes = searchCodeMethod.GetCustomAttributes(typeof(McpFunctionAttribute), false);
        Assert.Single(searchCodeAttributes);
    }

    public void Dispose()
    {
        // Clean up the test directory
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
} 