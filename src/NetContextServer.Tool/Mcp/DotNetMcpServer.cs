using MCPSharp;
using NetContextServer.Core.Indexer;

namespace NetContextServer.Tool.Mcp;

/// <summary>
/// Represents a .NET MCP server that provides tools for working with .NET codebases.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DotNetMcpServer"/> class.
/// </remarks>
/// <param name="solutionPath">The path to the solution directory.</param>
/// <param name="serverName">The name of the server.</param>
/// <param name="serverVersion">The version of the server.</param>
public class DotNetMcpServer(string solutionPath, string serverName = "NetContextServer", string serverVersion = "1.0.0")
{
    private readonly string _solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));
    private readonly string _serverName = serverName ?? throw new ArgumentNullException(nameof(serverName));
    private readonly string _serverVersion = serverVersion ?? throw new ArgumentNullException(nameof(serverVersion));

    /// <summary>
    /// Starts the MCP server using standard input/output for communication.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync()
    {
        Console.WriteLine($"Starting {_serverName} v{_serverVersion} for solution: {_solutionPath}");
        
        try
        {
            // Build the project index
            var index = ProjectIndexer.BuildIndex(_solutionPath);
            Console.WriteLine($"Indexed {index.ProjectPaths.Count} projects with {ProjectIndexer.GetAllSourceFiles(index).Count} source files");
            
            // Initialize the tool classes with the project index
            ProjectTools.Initialize(index);
            FileTools.Initialize(index);
            SymbolTools.Initialize(index);
            
            // Start the MCP server
            await MCPServer.StartAsync(_serverName, _serverVersion);
            
            Console.WriteLine("MCP server started and ready to receive requests");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error starting MCP server: {ex.Message}");
            throw;
        }
    }
} 