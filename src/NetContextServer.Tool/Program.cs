using NetContextServer.Core;

namespace NetContextServer.Tool;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var solutionPath = ParseArgs(args);
            
            Console.WriteLine($"Starting .NET Context MCP Server for solution: {solutionPath}");
            
            var server = new DotNetMcpServer(solutionPath);
            await server.StartAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            PrintUsage();
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
                PrintUsage();
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

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: net-context-server [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --solution, -s <path>    Path to the solution directory");
        Console.WriteLine("  --help, -h               Show this help message");
    }
}
