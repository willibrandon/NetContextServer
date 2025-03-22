using MCPSharp;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
[Collection("NetContextServer Tests")]
public class ProjectOperationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;
    private readonly MCPClient client;

    public ProjectOperationTests()
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
    public async Task ListProjects_ReturnsJsonArray()
    {
        await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", _testDir } });
        var result = await client.CallToolAsync("list_projects");
        var projects = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);

        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.EndsWith(".csproj"));
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