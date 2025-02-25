using MCPSharp;
using System.CommandLine;

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
        
        rootCommand.AddCommand(helloCommand);
        rootCommand.AddCommand(echoCommand);
        rootCommand.AddCommand(addCommand);
        rootCommand.AddCommand(addComplexCommand);
        
        return await rootCommand.InvokeAsync(args);
    }
}