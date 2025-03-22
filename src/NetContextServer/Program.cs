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
