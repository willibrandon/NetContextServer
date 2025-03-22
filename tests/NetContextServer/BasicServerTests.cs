using System.Diagnostics;
using Xunit;

namespace NetContextServer.Tests;

[Trait("Category", "AI_Generated")]
[Collection("NetContextServer Tests")]
public class BasicServerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testProjectPath;
    private readonly string _testCsFilePath;
    private readonly MCPClient client;

    public BasicServerTests()
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
        Assert.True(tools.Count() > 0);
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
    public async Task TestCallToolWithInvalidParameters()
    {
        var result = await client.CallToolAsync("Echo", new Dictionary<string, object> { { "invalid_param", "test" } });
        Assert.True(result.IsError);
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
