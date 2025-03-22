using System.Diagnostics;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
[Collection("NetContextServer Tests")]
public class FileOperationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;

    public FileOperationTests()
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
        _testDir = Path.Combine(Path.GetTempPath(), $"NetContextServer_Test_{Guid.NewGuid()}");
        _testProjectPath = Path.Combine(_testDir, "Test.csproj");
        _testCsFilePath = Path.Combine(_testDir, "Test.cs");

        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_testProjectPath, "<Project />");
        File.WriteAllText(_testCsFilePath, "public class Test { }");

        // Set base directory for tests
        Tools.SetBaseDirectory(_testDir);
    }

    [Fact]
    public void ListFiles_WithValidPath_ReturnsFiles()
    {
        // Act
        var files = Tools.ListFiles(_testDir);

        // Assert
        Assert.NotNull(files);
        Assert.Contains(files, f => f.EndsWith("test.cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ListFiles_WithInvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistent");
        
        // Act
        var result = Tools.ListFiles(invalidPath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, e => e.StartsWith("Error:"));
    }

    [Fact]
    public void OpenFile_WithValidPath_ReturnsContent()
    {
        // Arrange
        var content = "test content";
        File.WriteAllText(_testCsFilePath, content);

        // Act
        var result = Tools.OpenFile(_testCsFilePath);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void OpenFile_WithInvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistent.cs");
        
        // Act
        var result = Tools.OpenFile(invalidPath);

        // Assert
        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void OpenFile_WithLargeContent_ReturnsTruncated()
    {
        // Arrange
        var largeContent = new string('x', 150_000);
        File.WriteAllText(_testCsFilePath, largeContent);

        // Act
        var result = Tools.OpenFile(_testCsFilePath);

        // Assert
        Assert.Contains("[Truncated]", result);
        Assert.True(result.Length < largeContent.Length);
    }

    [Fact]
    public void ListSourceFiles_WithValidPath_ReturnsSourceFiles()
    {
        // Arrange
        // Create test source files
        File.WriteAllText(Path.Combine(_testDir, "Test.cs"), "");
        File.WriteAllText(Path.Combine(_testDir, "Test.vb"), "");
        File.WriteAllText(Path.Combine(_testDir, "Test.fs"), "");

        // Act
        var files = Tools.ListSourceFiles(_testDir);

        // Assert
        Assert.NotNull(files);
        Assert.Contains(files, f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".fs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ListSourceFiles_WithInvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistent");
        
        // Act
        var result = Tools.ListSourceFiles(invalidPath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, e => e.StartsWith("Error:"));
    }

    [Fact]
    public void ListFiles_IgnoresSensitiveFiles()
    {
        // Create test files
        var testCsPath = Path.Combine(_testDir, "test.cs");
        var envPath = Path.Combine(_testDir, "secrets.env");
        var configPath = Path.Combine(_testDir, "appsettings.Production.json");

        File.WriteAllText(testCsPath, "");
        File.WriteAllText(envPath, "");
        File.WriteAllText(configPath, "");

        var result = Tools.ListFiles(_testDir);

        Assert.NotNull(result);
        Assert.Single(result); // Should only contain the .cs file
        Assert.Equal(testCsPath, result[0], ignoreCase: true);
    }

    [Fact]
    public void OpenFile_BlocksAccessOutsideBaseDirectory()
    {
        var outsidePath = Path.Combine(Path.GetTempPath(), "outside.txt");
        File.WriteAllText(outsidePath, "secret content");

        var result = Tools.OpenFile(outsidePath);
        Assert.Contains("Error: Access to this file is not allowed", result);
    }

    [Fact]
    public void OpenFile_BlocksSensitiveFiles()
    {
        var secretFile = Path.Combine(_testDir, "secrets.env");
        File.WriteAllText(secretFile, "secret content");

        var result = Tools.OpenFile(secretFile);
        Assert.Contains("Error: This file type is restricted", result);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ListFiles_ShouldFindAllNetFileTypes()
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

        var result = Tools.ListFiles(_testDir);

        Assert.NotNull(result);
        // Should find all .NET files but not the .txt file
        Assert.Equal(8, result!.Length);
        Assert.Contains(result, f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".fs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".fsi", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".vbhtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result, f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ListSourceFiles_ShouldFindAllNetSourceFiles()
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

        var result = Tools.ListSourceFiles(projectDir);

        Assert.NotNull(result);
        // Should find all .NET files but not the .txt file
        Assert.Equal(8, result!.Length);
        Assert.Contains(result, f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".fs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".fsi", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".vbhtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result, f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
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
