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
            // Parse command line arguments
            var (solutionPath, port, serverName, serverVersion) = ParseArgs(args);
            
            Console.WriteLine($"Scanning solution at: {solutionPath}");

            // Build the project index
            var index = ProjectIndexer.BuildIndex(solutionPath);
            Console.WriteLine($"Indexed {index.ProjectPaths.Count} projects with {ProjectIndexer.GetAllSourceFiles(index).Count} source files");
            
            // Initialize the tool classes with the project index
            ProjectTools.Initialize(index);
            FileTools.Initialize(index);
            
            // Create and start the MCP server
            var server = new DotNetMcpServer(solutionPath, serverName, serverVersion);
            await server.StartAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Parses command line arguments.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>A tuple containing the solution path, port, server name, and server version.</returns>
    private static (string solutionPath, int? port, string serverName, string serverVersion) ParseArgs(string[] args)
    {
        // Default values
        var solutionPath = Directory.GetCurrentDirectory();
        int? port = null;
        var serverName = "NetContextServer";
        var serverVersion = "1.0.0";

        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--solution="))
            {
                solutionPath = arg["--solution=".Length..].Trim('"');
            }
            else if (arg == "--solution" && i + 1 < args.Length)
            {
                solutionPath = args[++i].Trim('"');
            }
            else if (arg.StartsWith("--port="))
            {
                if (int.TryParse(arg["--port=".Length..], out var parsedPort))
                {
                    port = parsedPort;
                }
            }
            else if (arg == "--port" && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedPort))
                {
                    port = parsedPort;
                }
            }
            else if (arg.StartsWith("--name="))
            {
                serverName = arg["--name=".Length..];
            }
            else if (arg == "--name" && i + 1 < args.Length)
            {
                serverName = args[++i];
            }
            else if (arg.StartsWith("--version="))
            {
                serverVersion = arg["--version=".Length..];
            }
            else if (arg == "--version" && i + 1 < args.Length)
            {
                serverVersion = args[++i];
            }
            else if (arg == "--help" || arg == "-h")
            {
                ShowHelp();
                Environment.Exit(0);
            }
        }

        // Validate solution path
        if (!Directory.Exists(solutionPath))
        {
            throw new DirectoryNotFoundException($"Solution directory not found: {solutionPath}");
        }

        return (solutionPath, port, serverName, serverVersion);
    }

    /// <summary>
    /// Shows help information.
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine("Usage: net-context-server [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --solution, --solution=PATH    Path to the solution directory (default: current directory)");
        Console.WriteLine("  --port, --port=PORT            Port to use for TCP communication (default: STDIO)");
        Console.WriteLine("  --name, --name=NAME            Server name (default: NetContextServer)");
        Console.WriteLine("  --version, --version=VERSION   Server version (default: 1.0.0)");
        Console.WriteLine("  --help, -h                     Show help information");
    }
}
