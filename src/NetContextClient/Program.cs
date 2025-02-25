using MCPSharp;
using System.CommandLine;
using System.Text.Json;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("NetContext Client - MCP client for .NET codebase interaction");
        
        // Hello command
        var helloCommand = new Command("hello", "Send a hello request to the server");
        helloCommand.SetHandler(async () =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var result = await client.CallToolAsync("hello");
                await Console.Out.WriteLineAsync(result.Content[0].Text);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        // Echo command
        var echoCommand = new Command("echo", "Echo back the input message");
        var messageOption = new Option<string>("--message", "The message to echo") { IsRequired = true };
        echoCommand.AddOption(messageOption);
        echoCommand.SetHandler(async (string message) =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var result = await client.CallToolAsync("echo", new Dictionary<string, object> { { "input", message } });
                await Console.Out.WriteLineAsync(result.Content[0].Text);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, messageOption);

        // Add command
        var addCommand = new Command("add", "Add two numbers");
        var aOption = new Option<int>("--a", "First number") { IsRequired = true };
        var bOption = new Option<int>("--b", "Second number") { IsRequired = true };
        addCommand.AddOption(aOption);
        addCommand.AddOption(bOption);
        addCommand.SetHandler(async (int a, int b) =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var result = await client.CallToolAsync("add", new Dictionary<string, object> 
                { 
                    { "a", a },
                    { "b", b }
                });
                await Console.Out.WriteLineAsync(result.Content[0].Text);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, aOption, bOption);

        // Add Complex command
        var addComplexCommand = new Command("add-complex", "Add a complex object with name, age, and hobbies");
        var nameOption = new Option<string>("--name", "Person's name") { IsRequired = true };
        var ageOption = new Option<int>("--age", "Person's age") { IsRequired = true };
        var hobbiesOption = new Option<string[]>("--hobbies", "List of hobbies") { IsRequired = true, AllowMultipleArgumentsPerToken = true };
        
        addComplexCommand.AddOption(nameOption);
        addComplexCommand.AddOption(ageOption);
        addComplexCommand.AddOption(hobbiesOption);
        
        addComplexCommand.SetHandler(async (string name, int age, string[] hobbies) =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var complexObj = new Dictionary<string, object>
                {
                    { "Name", name },
                    { "Age", age },
                    { "Hobbies", hobbies }
                };
                
                var result = await client.CallToolAsync("add_complex", new Dictionary<string, object> { { "obj", complexObj } });
                await Console.Out.WriteLineAsync(result.Content[0].Text);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, nameOption, ageOption, hobbiesOption);

        // Set Base Directory command
        var setBaseDirCommand = new Command("set-base-dir", "Set the base directory for file operations");
        var dirOption = new Option<string>("--directory", "The base directory path") { IsRequired = true };
        setBaseDirCommand.AddOption(dirOption);
        setBaseDirCommand.SetHandler(async (string directory) =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var result = await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", directory } });
                await Console.Out.WriteLineAsync("Base directory set successfully.");
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, dirOption);

        // List Projects command
        var listProjectsCommand = new Command("list-projects", "List all projects in the solution");
        listProjectsCommand.SetHandler(async () =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var result = await client.CallToolAsync("list_projects");
                var projects = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
                foreach (var project in projects!)
                {
                    await Console.Out.WriteLineAsync(project);
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        // List Files command
        var listFilesCommand = new Command("list-files", "List all files in a project");
        var projectPathOption = new Option<string>("--project-path", "The project path") { IsRequired = true };
        listFilesCommand.AddOption(projectPathOption);
        listFilesCommand.SetHandler(async (string projectPath) =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var result = await client.CallToolAsync("list_files", new Dictionary<string, object> { { "projectPath", projectPath } });
                var files = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
                foreach (var file in files!)
                {
                    await Console.Out.WriteLineAsync(file);
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, projectPathOption);

        // Open File command
        var openFileCommand = new Command("open-file", "Open and display a file's contents");
        var filePathOption = new Option<string>("--file-path", "The file path to open") { IsRequired = true };
        openFileCommand.AddOption(filePathOption);
        openFileCommand.SetHandler(async (string filePath) =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var result = await client.CallToolAsync("open_file", new Dictionary<string, object> { { "filePath", filePath } });
                await Console.Out.WriteLineAsync(result.Content[0].Text);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, filePathOption);

        // Search Code command
        var searchCodeCommand = new Command("search-code", "Search for text in the codebase");
        var searchTextOption = new Option<string>("--text", "The text to search for") { IsRequired = true };
        searchCodeCommand.AddOption(searchTextOption);
        searchCodeCommand.SetHandler(async (string searchText) =>
        {
            try
            {
                using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
                var result = await client.CallToolAsync("search_code", new Dictionary<string, object> { { "searchText", searchText } });
                var matches = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
                foreach (var match in matches!)
                {
                    await Console.Out.WriteLineAsync(match);
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, searchTextOption);
        
        rootCommand.AddCommand(helloCommand);
        rootCommand.AddCommand(echoCommand);
        rootCommand.AddCommand(addCommand);
        rootCommand.AddCommand(addComplexCommand);
        rootCommand.AddCommand(setBaseDirCommand);
        rootCommand.AddCommand(listProjectsCommand);
        rootCommand.AddCommand(listFilesCommand);
        rootCommand.AddCommand(openFileCommand);
        rootCommand.AddCommand(searchCodeCommand);
        
        return await rootCommand.InvokeAsync(args);
    }
}