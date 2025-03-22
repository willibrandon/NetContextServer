using ModelContextProtocol.Client;
using System.Diagnostics;
using System.Text.Json;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
[Collection("NetContextServer Tests")]
public class IgnoreOperationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;

    private readonly IMcpClient client;

    public IgnoreOperationTests()
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
    public async Task ClearIgnorePatterns_RemovesAllPatterns()
    {
        var result = await client.CallToolAsync("clear_ignore_patterns");
        var patterns = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(patterns);
        Assert.Empty(patterns);
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
