using NetContextServer.Core;

namespace NetContextServer.Tool;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var solutionPath = ParseArgs(args);
            
            await Console.Out.WriteLineAsync($"Starting .NET Context MCP Server for solution: {solutionPath}");
            
            var server = new DotNetMcpServer(solutionPath);
            await server.StartAsync();
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            await PrintUsageAsync();
            Environment.Exit(1);
        }
    }

    private static string ParseArgs(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("No arguments provided.");
        }

        string? solutionPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--solution" || args[i] == "-s")
            {
                if (i + 1 < args.Length)
                {
                    solutionPath = args[i + 1];
                    i++; // Skip the next argument
                }
                else
                {
                    throw new ArgumentException("Missing value for --solution parameter.");
                }
            }
            else if (args[i] == "--help" || args[i] == "-h")
            {
                // We can't use await here since this is a synchronous method
                // Instead, we'll print the usage synchronously to avoid deadlocks
                Console.WriteLine("Usage: net-context-server [options]");
                Console.WriteLine("Options:");
                Console.WriteLine("  --solution, -s <path>    Path to the solution directory");
                Console.WriteLine("  --help, -h               Show this help message");
                Environment.Exit(0);
            }
        }

        if (string.IsNullOrEmpty(solutionPath))
        {
            // If no solution path is specified, use the current directory
            solutionPath = Environment.CurrentDirectory;
        }

        return solutionPath;
    }

    private static async Task PrintUsageAsync()
    {
        await Console.Out.WriteLineAsync("Usage: net-context-server [options]");
        await Console.Out.WriteLineAsync("Options:");
        await Console.Out.WriteLineAsync("  --solution, -s <path>    Path to the solution directory");
        await Console.Out.WriteLineAsync("  --help, -h               Show this help message");
    }
}
