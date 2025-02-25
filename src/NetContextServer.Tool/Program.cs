using MCPSharp;

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
                solutionRoot = args[0].Substring("--solution=".Length).Trim('"');
            }
            
            Console.WriteLine($"Scanning solution at: {solutionRoot}");

            // Create and start the MCP server
            await MCPServer.StartAsync("foo", "0.1.0");
            
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
