using Microsoft.Extensions.Hosting;
using ModelContextProtocol;

// Build and run the MCP server
var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer(options => 
    {
        options.ServerInfo = new() { Name = "NetContextServer", Version = "1.0" };
    })
    .WithStdioServerTransport()
    .WithTools();

await builder.Build().RunAsync();
