using MCPSharp;
using System.Diagnostics;
using System.Text.Json;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
[Collection("NetContextServer Tests")]
public class FileOperationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;

    private readonly MCPClient client;

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
