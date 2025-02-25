using MCPSharp;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NetContextServer.TestClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("NetContextServer Test Client");
        Console.WriteLine("----------------------------");
        
        Console.WriteLine("Note: Make sure the MCP server is running in another terminal with: net-context-server");
        Console.WriteLine();
        
        // Create an MCP client
        using var client = new MCPClient("NetContextServer.TestClient", "1.0.0", @"D:\SRC\NetContextServer\src\NetContextServer.Tool\bin\Debug\net9.0\NetContextServer.Tool");
        
        try
        {
            // List available tools
            Console.WriteLine("Listing available tools...");
            var tools = await client.GetToolsAsync();
            Console.WriteLine($"Found {tools.Count} tools:");
            foreach (var tool in tools)
            {
                Console.WriteLine($"- {tool.Name}: {tool.Description}");
                // Note: MCPSharp API doesn't expose Functions property directly
                // We'll just show the tool names for now
            }
            
            // Test the list_projects tool
            await TestListProjectsAsync(client);
            
            // Test the search_code tool
            Console.WriteLine("\nEnter a keyword to search for in the code (or press Enter to skip):");
            var keyword = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                await TestSearchCodeAsync(client, keyword);
            }
            
            // Test the get_doc_for_symbol tool
            Console.WriteLine("\nEnter a symbol name to get documentation for (or press Enter to skip):");
            var symbolName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(symbolName))
            {
                await TestGetDocForSymbolAsync(client, symbolName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }
    
    static async Task TestListProjectsAsync(MCPClient client)
    {
        Console.WriteLine("\nTesting list_projects tool...");
        
        try
        {
            // Call the tool without parameters
            var response = await client.CallToolAsync("project_tools.list_projects");
            
            Console.WriteLine("Projects in solution:");
            
            // Convert the response to a JsonNode
            var jsonNode = JsonSerializer.Deserialize<JsonNode>(response.ToString());
            
            if (jsonNode is JsonArray projects)
            {
                foreach (var project in projects)
                {
                    Console.WriteLine($"- {project}");
                }
            }
            else
            {
                Console.WriteLine("No projects found or invalid response.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    static async Task TestSearchCodeAsync(MCPClient client, string keyword)
    {
        Console.WriteLine($"\nTesting search_code tool with keyword '{keyword}'...");
        
        try
        {
            var response = await client.CallToolAsync($"file_tools.search_code?keyword={Uri.EscapeDataString(keyword)}");
            
            Console.WriteLine("Search results:");
            
            // Convert the response to a JsonNode
            var jsonNode = JsonSerializer.Deserialize<JsonNode>(response.ToString());
            
            if (jsonNode is JsonArray results && results.AsArray().Count > 0)
            {
                foreach (var resultItem in results.AsArray())
                {
                    if (resultItem is JsonObject resultObj)
                    {
                        var filePath = resultObj["FilePath"]?.GetValue<string>();
                        var lineNumber = resultObj["LineNumber"]?.GetValue<int>();
                        var lineText = resultObj["LineText"]?.GetValue<string>();
                        
                        Console.WriteLine($"- {filePath}:{lineNumber}: {lineText}");
                    }
                }
            }
            else
            {
                Console.WriteLine("No matches found or invalid response.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    static async Task TestGetDocForSymbolAsync(MCPClient client, string symbolName)
    {
        Console.WriteLine($"\nTesting get_doc_for_symbol tool with symbol '{symbolName}'...");
        
        try
        {
            var response = await client.CallToolAsync($"symbol_tools.get_doc_for_symbol?symbolName={Uri.EscapeDataString(symbolName)}");
            
            Console.WriteLine("Documentation:");
            
            // Convert the response to a JsonNode
            var jsonNode = JsonSerializer.Deserialize<JsonNode>(response.ToString());
            
            if (jsonNode is JsonObject docObj)
            {
                var symbolName2 = docObj["SymbolName"]?.GetValue<string>();
                var filePath = docObj["FilePath"]?.GetValue<string>();
                var lineNumber = docObj["LineNumber"]?.GetValue<int>();
                var documentation = docObj["Documentation"]?.GetValue<string>();
                var symbolType = docObj["SymbolType"]?.GetValue<string>();
                
                Console.WriteLine($"Symbol: {symbolName2}");
                Console.WriteLine($"Type: {symbolType}");
                Console.WriteLine($"File: {filePath}:{lineNumber}");
                Console.WriteLine($"Documentation: {documentation}");
            }
            else
            {
                Console.WriteLine("No documentation found or invalid response.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
} 