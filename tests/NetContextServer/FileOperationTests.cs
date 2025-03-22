using ModelContextProtocol.Client;
using System.Text.Json;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class FileOperationTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;
    private readonly IMcpClient _client;

    public FileOperationTests(NetContextServerFixture fixture)
    {
        _client = fixture.Client;

        // Setup test directory and files
        _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerTests_" + Guid.NewGuid());
        _testProjectPath = Path.Combine(_testDir, "Test.csproj");
        _testCsFilePath = Path.Combine(_testDir, "Test.cs");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_testDir);
        await File.WriteAllTextAsync(_testProjectPath, "<Project />");
        await File.WriteAllTextAsync(_testCsFilePath, "public class Test { }");
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
    public async Task ListFiles_WithValidPath_ReturnsJsonArray()
    {
        // Arrange: Set base directory to our test directory
        var setResult = await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });
        Assert.NotNull(setResult);

        // Act: List files in the directory
        var result = await _client.CallToolAsync("list_files", 
            new Dictionary<string, object> { ["projectPath"] = _testDir });
        
        // Assert: Verify response structure and content
        Assert.NotNull(result);
        
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var files = content.Text != null ? JsonSerializer.Deserialize<string[]>(content.Text) : null;
        Assert.NotNull(files);
        Assert.Contains(files, f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListFiles_WithInvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistent");

        // Act
        var result = await _client.CallToolAsync("list_files", 
            new Dictionary<string, object> { ["projectPath"] = invalidPath });
        
        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var error = content.Text != null ? JsonSerializer.Deserialize<string[]>(content.Text) : null;
        Assert.NotNull(error);
        Assert.Contains(error, e => e.StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OpenFile_WithValidPath_ReturnsContent()
    {
        // Arrange
        var content = "test content";
        await File.WriteAllTextAsync(_testCsFilePath, content);
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("open_file", 
            new Dictionary<string, object> { ["filePath"] = _testCsFilePath });
        
        // Assert
        Assert.NotNull(result);
        var responseContent = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(responseContent);
        Assert.Equal(content, responseContent.Text);
    }

    [Fact]
    public async Task OpenFile_WithInvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDir, "NonExistent.cs");
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("open_file", 
            new Dictionary<string, object> { ["filePath"] = invalidPath });
        
        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        Assert.StartsWith("Error:", content.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenFile_WithLargeContent_ReturnsTruncated()
    {
        // Arrange
        var largeContent = new string('x', 150_000);
        await File.WriteAllTextAsync(_testCsFilePath, largeContent);
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("open_file", 
            new Dictionary<string, object> { ["filePath"] = _testCsFilePath });
        
        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        Assert.Contains("[Truncated]", content.Text);
        Assert.True(content.Text.Length < largeContent.Length);
    }

    [Fact]
    public async Task ListSourceFiles_WithValidPath_ReturnsSourceFiles()
    {
        // Arrange: Create test source files
        await File.WriteAllTextAsync(Path.Combine(_testDir, "Test.cs"), "");
        await File.WriteAllTextAsync(Path.Combine(_testDir, "Test.vb"), "");
        await File.WriteAllTextAsync(Path.Combine(_testDir, "Test.fs"), "");
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("list_source_files", 
            new Dictionary<string, object> { ["projectDir"] = _testDir });
        
        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var files = content.Text != null ? JsonSerializer.Deserialize<string[]>(content.Text) : null;
        Assert.NotNull(files);
        Assert.Contains(files, f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith(".fs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListSourceFiles_WithInvalidPath_ReturnsError()
    {
        var invalidPath = Path.Combine(_testDir, "NonExistent");
        var result = await _client.CallToolAsync("list_source_files", new Dictionary<string, object> { { "projectDir", invalidPath } });
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        var content = result.Content[0];
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        var error = content.Text != null ? JsonSerializer.Deserialize<string[]>(content.Text) : null;

        Assert.NotNull(error);
        Assert.Contains(error, e => e.StartsWith("Error:"));
    }

    [Fact]
    public async Task ListFiles_IgnoresSensitiveFiles()
    {
        // Arrange: Create test files including sensitive ones
        var testCsPath = Path.Combine(_testDir, "test.cs");
        var envPath = Path.Combine(_testDir, "secrets.env");
        var configPath = Path.Combine(_testDir, "appsettings.Production.json");

        await File.WriteAllTextAsync(testCsPath, "");
        await File.WriteAllTextAsync(envPath, "");
        await File.WriteAllTextAsync(configPath, "");
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("list_files", 
            new Dictionary<string, object> { ["projectPath"] = _testDir });
        
        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var files = content.Text != null ? JsonSerializer.Deserialize<string[]>(content.Text) : null;
        Assert.NotNull(files);
        Assert.Single(files); // Should only contain the .cs file
        Assert.Equal(testCsPath, files[0], ignoreCase: true);
    }

    [Fact]
    public async Task OpenFile_BlocksAccessOutsideBaseDirectory()
    {
        // Arrange
        var outsidePath = Path.Combine(Path.GetTempPath(), "outside.txt");
        await File.WriteAllTextAsync(outsidePath, "secret content");

        try
        {
            // Act
            var result = await _client.CallToolAsync("open_file", 
                new Dictionary<string, object> { ["filePath"] = outsidePath });
            
            // Assert
            Assert.NotNull(result);
            var content = result.Content.FirstOrDefault(c => c.Type == "text");
            Assert.NotNull(content);
            Assert.NotNull(content.Text);
            Assert.Contains("Error: Access to this file is not allowed", content.Text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outsidePath))
            {
                File.Delete(outsidePath);
            }
        }
    }

    [Fact]
    public async Task OpenFile_BlocksSensitiveFiles()
    {
        // Arrange
        var secretFile = Path.Combine(_testDir, "secrets.env");
        await File.WriteAllTextAsync(secretFile, "secret content");
        await _client.CallToolAsync("set_base_directory",
            new Dictionary<string, object> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("open_file",
            new Dictionary<string, object> { ["filePath"] = secretFile });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        Assert.Contains("Error: This file type is restricted", content.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
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

        await _client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await _client.CallToolAsync("list_files", new Dictionary<string, object> { { "projectPath", _testDir } });
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        var content = result.Content[0];
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        var files = content.Text != null ? JsonSerializer.Deserialize<string[]>(content.Text) : null;

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

        await _client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await _client.CallToolAsync("list_source_files", new Dictionary<string, object> { { "projectDir", projectDir } });
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        var content = result.Content[0];
        Assert.NotNull(content);
        Assert.NotNull(content.Text);
        var files = content.Text != null ? JsonSerializer.Deserialize<string[]>(content.Text) : null;

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
}
