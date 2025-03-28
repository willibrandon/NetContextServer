using ModelContextProtocol.Client;
using ModelContextProtocol.Configuration;
using ModelContextProtocol.Protocol.Transport;
using System.Diagnostics;

namespace NetContextServer.Tests;

[CollectionDefinition("NetContextServer Collection", DisableParallelization = true)]
public class NetContextServerCollection : ICollectionFixture<NetContextServerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class NetContextServerFixture : IAsyncLifetime, IDisposable
{
    public IMcpClient Client { get; private set; } = null!;
    private Process? _serverProcess;
    private bool _disposed;
    private const int StartupTimeoutSeconds = 30;

    public async Task InitializeAsync()
    {
        // Kill any existing server processes
        await CleanupExistingProcesses();

        // Get the solution root directory (four levels up from the test output directory)
        // AppContext.BaseDirectory points to something like: D:\SRC\NetContextServer\tests\NetContextServer\bin\Debug\net9.0
        var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var projectPath = Path.Combine(solutionDir, "src", "NetContextServer", "NetContextServer.csproj");

        if (!File.Exists(projectPath))
        {
            throw new InvalidOperationException(
                $"Could not find the NetContextServer project at: {projectPath}");
        }

        // Start the server process
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = solutionDir
        };

        _serverProcess = new Process { StartInfo = startInfo };

        // Set up output handling before starting the process
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        _serverProcess.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                Debug.WriteLine($"Server Output: {e.Data}");
            }
        };

        _serverProcess.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                Debug.WriteLine($"Server Error: {e.Data}");
            }
        };

        try
        {
            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            // Wait a bit for the process to start
            await Task.Delay(1000);

            if (_serverProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Server process exited prematurely with code {_serverProcess.ExitCode}. " +
                    $"Output: {outputBuilder}\nError: {errorBuilder}");
            }

            // Create client options with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(StartupTimeoutSeconds));

            var clientOptions = new McpClientOptions
            {
                ClientInfo = new() { Name = "NetContextServer.Tests", Version = "1.0.0" }
            };

            // Create server configuration
            var serverConfig = new McpServerConfig
            {
                Id = "netcontext-test",
                Name = "NetContextServer",
                TransportType = TransportTypes.StdIo,
                TransportOptions = new Dictionary<string, string>
                {
                    ["command"] = startInfo.FileName,
                    ["arguments"] = startInfo.Arguments
                }
            };

            try
            {
                // Create MCP client
                Client = await McpClientFactory.CreateAsync(serverConfig, clientOptions);

                // Verify connection with a hello request
                using var helloCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var helloResult = await Client.CallToolAsync("hello", 
                    new Dictionary<string, object?>(), helloCts.Token);
                
                if (helloResult.IsError)
                {
                    throw new InvalidOperationException(
                        $"Server hello failed. Output: {outputBuilder}\nError: {errorBuilder}");
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
                throw new TimeoutException(
                    $"Server failed to respond within {StartupTimeoutSeconds} seconds. " +
                    $"Output: {outputBuilder}\nError: {errorBuilder}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Server connection failed: {ex.Message}. Output: {outputBuilder}\nError: {errorBuilder}");
            }
        }
        catch
        {
            // Cleanup on failure
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill(true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_disposed) return;

        if (Client != null)
        {
            await Client.DisposeAsync();
        }

        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(true);
                await _serverProcess.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
            }
            catch
            {
                // Ignore errors when killing process
            }
            finally
            {
                _serverProcess.Dispose();
            }
        }

        await CleanupExistingProcesses();
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(true);
                _serverProcess.WaitForExit(1000);
            }
            catch
            {
                // Ignore errors when killing process
            }
            finally
            {
                _serverProcess.Dispose();
            }
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static async Task CleanupExistingProcesses()
    {
        foreach (var process in Process.GetProcessesByName("NetContextServer"))
        {
            try
            {
                process.Kill(true);
                await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
            }
            catch
            {
                // Ignore errors when killing processes
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
