/// <summary>
/// Entry point for the .NET Context Server, which implements a Model Context Protocol (MCP) server
/// for providing codebase context to Large Language Models.
/// 
/// This server is built using the official C# SDK for MCP (https://github.com/modelcontextprotocol/csharp-sdk)
/// and provides tools for:
/// - File system operations and project discovery
/// - Code search (both text-based and semantic)
/// - Package analysis
/// - Ignore pattern management
/// </summary>
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using System.Reflection;

// Build and run the MCP server
var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "NetContextServer", Version = "1.0" };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly(Assembly.GetExecutingAssembly());

await builder.Build().RunAsync();
