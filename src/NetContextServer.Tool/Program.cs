using MCPSharp;
using NetContextServer.Core.Indexer;
using NetContextServer.Core.MCP;

namespace NetContextServer.Tool;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("NetContextServer - .NET Codebase Context MCP Server");
        Console.WriteLine("----------------------------------------------------");

        try
        {
            // Get the solution directory (current directory for now)
            var solutionRoot = Directory.GetCurrentDirectory();
            
            // Parse command line arguments (to be expanded in future chunks)
            if (args.Length > 0 && args[0].StartsWith("--solution="))
            {
                solutionRoot = args[0]["--solution=".Length..].Trim('"');
            }
            
            Console.WriteLine($"Scanning solution at: {solutionRoot}");

            // Build the project index
            var index = ProjectIndexer.BuildIndex(solutionRoot);
            Console.WriteLine($"Indexed {index.ProjectPaths.Count} projects with {ProjectIndexer.GetAllSourceFiles(index).Count} source files");
            
            // Initialize the tool classes with the project index
            ProjectTools.Initialize(index);
            FileTools.Initialize(index);
            
            // Start the MCP server
            Console.WriteLine("Starting MCP server...");
            await MCPServer.StartAsync("NetContextServer", "1.0.0");
            
            // The server will block until terminated
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }
}
