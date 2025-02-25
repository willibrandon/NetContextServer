using MCPSharp;

namespace NetContextServer.Core;

/// <summary>
/// Represents a .NET MCP server that provides access to a .NET codebase.
/// </summary>
public class DotNetMcpServer
{
    private readonly string _solutionPath;
    private ProjectIndex? _projectIndex;
    private McpTools? _tools;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotNetMcpServer"/> class.
    /// </summary>
    /// <param name="solutionPath">The path to the solution to serve.</param>
    public DotNetMcpServer(string solutionPath)
    {
        _solutionPath = solutionPath;
    }

    /// <summary>
    /// Starts the MCP server using standard input/output for communication.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync()
    {
        try
        {
            // Build the project index
            await Console.Out.WriteLineAsync($"Building project index for solution: {_solutionPath}");
            _projectIndex = ProjectIndexer.BuildIndex(_solutionPath);
            
            // Create the tools
            _tools = new McpTools(_projectIndex);

            // Start the MCP server
            await Console.Out.WriteLineAsync("Starting .NET Context MCP Server...");
            await Console.Out.WriteLineAsync($"Found {_projectIndex.ProjectPaths.Count} projects and {_projectIndex.FilesByProject.Values.SelectMany(v => v).Count()} source files.");
            
            // The MCPSharp library automatically discovers and registers tools with the [McpTool] attribute
            await MCPServer.StartAsync("NetContextServer", "1.0.0");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error starting server: {ex.Message}");
            throw;
        }
    }
} 