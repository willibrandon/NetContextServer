using ModelContextProtocol.Client;
using ModelContextProtocol.Configuration;
using ModelContextProtocol.Protocol.Transport;
using NetContextClient.Models;
using System.CommandLine;
using System.Text.Json;

/// <summary>
/// Command-line interface for the .NET Context Client, which interacts with the MCP server
/// to provide codebase analysis and management capabilities.
/// 
/// Built using the official C# SDK for MCP (https://github.com/modelcontextprotocol/csharp-sdk),
/// this client provides commands for:
/// - File system operations and project discovery
/// - Code search (both text-based and semantic)
/// - Package analysis
/// - Ignore pattern management
/// </summary>
class Program
{
    /// <summary>
    /// Entry point for the command-line interface. Sets up the MCP client and defines
    /// the command structure using System.CommandLine.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the program.</param>
    /// <returns>
    /// 0 for successful execution, non-zero for errors:
    /// - 1: General execution error
    /// - Other codes as defined by System.CommandLine
    /// </returns>
    /// <remarks>
    /// Available commands:
    /// - hello: Basic connectivity test
    /// - set-base-dir: Set working directory
    /// - get-base-dir: Show current working directory
    /// - list-projects: Show all .NET projects
    /// - list-files: List files in a project
    /// - open-file: View file contents
    /// - search-code: Text-based code search
    /// - semantic-search: AI-powered code search
    /// - analyze-packages: Analyze NuGet packages
    /// - Ignore pattern management commands:
    ///   * add-ignore-patterns
    ///   * remove-ignore-patterns
    ///   * get-ignore-patterns
    ///   * clear-ignore-patterns
    ///   * get-state-file-location
    /// </remarks>
    static async Task<int> Main(string[] args)
    {
        // Define client and server configuration
        var clientOptions = new McpClientOptions
        {
            ClientInfo = new() { Name = "NetContextClient", Version = "1.0.0" }
        };

        var serverConfig = new McpServerConfig
        {
            Id = "netcontext",
            Name = "NetContextServer",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new()
            {
                ["command"] = "dotnet",
                ["arguments"] = "run --project ./src/NetContextServer/NetContextServer.csproj",
            }
        };

        // Create MCP client
        await using var client = await McpClientFactory.CreateAsync(serverConfig, clientOptions);
        
        var rootCommand = new RootCommand("NetContext Client - MCP client for .NET codebase interaction");
        
        // Hello command
        var helloCommand = new Command("hello", "Send a hello request to the server");
        helloCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("hello", []);
                var content = result.Content.First(c => c.Type == "text");
                await Console.Out.WriteLineAsync(content.Text);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        // Set Base Directory command
        var setBaseDirCommand = new Command("set-base-dir", "Set the base directory for file operations");
        var dirOption = new Option<string>("--directory", "The base directory path") { IsRequired = true };
        setBaseDirCommand.AddOption(dirOption);
        setBaseDirCommand.SetHandler(async (string directory) =>
        {
            try
            {
                var result = await client.CallToolAsync("set_base_directory", new() { ["directory"] = directory });
                await Console.Out.WriteLineAsync("Base directory set successfully. ");

                if (result != null && result.Content != null && result.Content.Count > 0)
                {
                    var content = result.Content.First(c => c.Type == "text");
                    await Console.Out.WriteLineAsync(content.Text);
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, dirOption);

        // Get Base Directory command
        var getBaseDirCommand = new Command("get-base-dir", "Get the current base directory");
        getBaseDirCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("get_base_directory", []);
                var jsonResponse = result.Content.First(c => c.Type == "text").Text;
                
                try
                {
                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        // Try to deserialize as JDocument first to inspect the structure
                        var jsonObj = JsonDocument.Parse(jsonResponse);
                        var rootElement = jsonObj.RootElement;
                        
                        // Check if the response is an error message
                        if (rootElement.ValueKind == JsonValueKind.Object && 
                            rootElement.TryGetProperty("Error", out var errorElement))
                        {
                            await Console.Error.WriteLineAsync($"Error: {errorElement}");
                            return;
                        }
                        
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var response = JsonSerializer.Deserialize<BaseDirectoryResponse>(jsonResponse, options);
                        
                        if (response != null)
                        {
                            await Console.Out.WriteLineAsync($"Current base directory: {response.BaseDirectory}");
                            
                            if (!response.Exists)
                            {
                                await Console.Out.WriteLineAsync("⚠️ Warning: This directory does not exist!");
                            }
                        }
                        else
                        {
                            await Console.Error.WriteLineAsync("Failed to parse response from server.");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    await Console.Error.WriteLineAsync($"Error parsing JSON response: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            }
        });

        // List Projects command
        var listProjectsCommand = new Command("list-projects", "List all projects in the solution");
        listProjectsCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("list_projects", []);
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var projects = JsonSerializer.Deserialize<string[]>(jsonText);
                    foreach (var project in projects!)
                    {
                        await Console.Out.WriteLineAsync(project);
                    }
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
                var result = await client.CallToolAsync("list_files", new() { ["projectPath"] = projectPath });
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var files = JsonSerializer.Deserialize<string[]>(jsonText);
                    foreach (var file in files!)
                    {
                        await Console.Out.WriteLineAsync(file);
                    }
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
                var result = await client.CallToolAsync("open_file", new() { ["filePath"] = filePath });
                await Console.Out.WriteLineAsync(result.Content.First(c => c.Type == "text").Text);
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
                var result = await client.CallToolAsync("search_code", new() { ["searchText"] = searchText });
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var matches = JsonSerializer.Deserialize<string[]>(jsonText);
                    foreach (var match in matches!)
                    {
                        await Console.Out.WriteLineAsync(match);
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, searchTextOption);

        // List Solutions command
        var listSolutionsCommand = new Command("list-solutions", "List all solution files");
        listSolutionsCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("list_solutions", []);
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var solutions = JsonSerializer.Deserialize<string[]>(jsonText);
                    foreach (var solution in solutions!)
                    {
                        await Console.Out.WriteLineAsync(solution);
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        // List Projects In Directory command
        var listProjectsInDirCommand = new Command("list-projects-in-dir", "List all projects in a directory");
        var directoryOption = new Option<string>("--directory", "The directory to search in") { IsRequired = true };
        listProjectsInDirCommand.AddOption(directoryOption);
        listProjectsInDirCommand.SetHandler(async (string directory) =>
        {
            try
            {
                var result = await client.CallToolAsync("list_projects_in_dir", new() { ["directory"] = directory });
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var projects = JsonSerializer.Deserialize<string[]>(jsonText);
                    foreach (var project in projects!)
                    {
                        await Console.Out.WriteLineAsync(project);
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, directoryOption);

        // List Source Files command
        var listSourceFilesCommand = new Command("list-source-files", "List all source files in a project");
        var projectDirOption = new Option<string>("--project-dir", "The project directory") { IsRequired = true };
        listSourceFilesCommand.AddOption(projectDirOption);
        listSourceFilesCommand.SetHandler(async (string projectDir) =>
        {
            try
            {
                var result = await client.CallToolAsync("list_source_files", new() { ["projectDir"] = projectDir });
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var files = JsonSerializer.Deserialize<string[]>(jsonText);
                    foreach (var file in files!.Where(f => !f.Contains("\\obj\\")))
                    {
                        await Console.Out.WriteLineAsync(file);
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, projectDirOption);

        // Add Ignore Patterns command
        var addIgnorePatternsCommand = new Command("add-ignore-patterns", "Add patterns to ignore when listing files");
        var patternsOption = new Option<string[]>("--patterns", "The patterns to ignore") { IsRequired = true, AllowMultipleArgumentsPerToken = true };
        addIgnorePatternsCommand.AddOption(patternsOption);
        addIgnorePatternsCommand.SetHandler(async (string[] patterns) =>
        {
            try
            {
                var result = await client.CallToolAsync("add_ignore_patterns", new() { ["patterns"] = patterns });
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var response = JsonSerializer.Deserialize<AddIgnorePatternsResponse>(jsonText);
                    
                    if (response!.InvalidPatterns.Length > 0)
                    {
                        await Console.Out.WriteLineAsync("Invalid patterns (not added):");
                        foreach (var pattern in response.InvalidPatterns)
                        {
                            await Console.Out.WriteLineAsync($"  {pattern}");
                        }
                        await Console.Out.WriteLineAsync();
                    }

                    if (response.ValidPatternsAdded.Length > 0)
                    {
                        await Console.Out.WriteLineAsync("Added user patterns:");
                        foreach (var pattern in response.ValidPatternsAdded)
                        {
                            await Console.Out.WriteLineAsync($"  {pattern}");
                        }
                        await Console.Out.WriteLineAsync();
                    }
                    
                    await Console.Out.WriteLineAsync("All active patterns:");
                    foreach (var pattern in response.AllPatterns)
                    {
                        await Console.Out.WriteLineAsync($"  {pattern}");
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, patternsOption);

        // Remove Ignore Patterns command
        var removeIgnorePatternsCommand = new Command("remove-ignore-patterns", "Remove specific ignore patterns");
        var removePatternsOption = new Option<string[]>("--patterns", "The patterns to remove") { IsRequired = true, AllowMultipleArgumentsPerToken = true };
        removeIgnorePatternsCommand.AddOption(removePatternsOption);
        removeIgnorePatternsCommand.SetHandler(async (string[] patterns) =>
        {
            try
            {
                var result = await client.CallToolAsync("remove_ignore_patterns", new() { ["patterns"] = patterns });
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var response = JsonSerializer.Deserialize<RemoveIgnorePatternsResponse>(jsonText);

                    if (response!.DefaultPatternsSkipped.Length > 0)
                    {
                        await Console.Out.WriteLineAsync("Default patterns (cannot be removed):");
                        foreach (var pattern in response.DefaultPatternsSkipped)
                        {
                            await Console.Out.WriteLineAsync($"  {pattern}");
                        }
                        await Console.Out.WriteLineAsync();
                    }

                    if (response.RemovedPatterns.Length > 0)
                    {
                        await Console.Out.WriteLineAsync("Successfully removed patterns:");
                        foreach (var pattern in response.RemovedPatterns)
                        {
                            await Console.Out.WriteLineAsync($"  {pattern}");
                        }
                        await Console.Out.WriteLineAsync();
                    }

                    if (response.NotFoundPatterns.Length > 0)
                    {
                        await Console.Out.WriteLineAsync("Patterns not found:");
                        foreach (var pattern in response.NotFoundPatterns)
                        {
                            await Console.Out.WriteLineAsync($"  {pattern}");
                        }
                        await Console.Out.WriteLineAsync();
                    }

                    await Console.Out.WriteLineAsync("Remaining patterns:");
                    foreach (var pattern in response.AllPatterns)
                    {
                        await Console.Out.WriteLineAsync($"  {pattern}");
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, removePatternsOption);

        // Get State File Location command
        var getStateFileLocationCommand = new Command("get-state-file-location", "Show the location of the ignore patterns state file");
        getStateFileLocationCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("get_state_file_location", []);
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var response = JsonSerializer.Deserialize<StateFileLocationResponse>(jsonText);
                    await Console.Out.WriteLineAsync($"State file location: {response!.StateFilePath}");
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        // Get Ignore Patterns command
        var getIgnorePatternsCommand = new Command("get-ignore-patterns", "Get current ignore patterns");
        getIgnorePatternsCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("get_ignore_patterns", []);
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var response = JsonSerializer.Deserialize<IgnorePatternsResponse>(jsonText);
                    
                    await Console.Out.WriteLineAsync("Default patterns:");
                    foreach (var pattern in response!.DefaultPatterns)
                    {
                        await Console.Out.WriteLineAsync($"  {pattern}");
                    }
                    
                    if (response.UserPatterns.Length != 0)
                    {
                        await Console.Out.WriteLineAsync("\nUser-added patterns:");
                        foreach (var pattern in response.UserPatterns)
                        {
                            await Console.Out.WriteLineAsync($"  {pattern}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        // Clear Ignore Patterns command
        var clearIgnorePatternsCommand = new Command("clear-ignore-patterns", "Clear all user-added ignore patterns");
        clearIgnorePatternsCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("clear_ignore_patterns", []);
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var response = JsonSerializer.Deserialize<IgnorePatternsResponse>(jsonText);
                    
                    await Console.Out.WriteLineAsync("Cleared all user-added patterns.");
                    await Console.Out.WriteLineAsync("\nRemaining default patterns:");
                    foreach (var pattern in response!.DefaultPatterns)
                    {
                        await Console.Out.WriteLineAsync($"  {pattern}");
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        // Semantic Search command
        var semanticSearchCommand = new Command("semantic-search", "Search code using semantic similarity");
        var queryOption = new Option<string>("--query", "The search query") { IsRequired = true };
        var topKOption = new Option<int?>("--top", "Number of results to return") { IsRequired = false };
        semanticSearchCommand.AddOption(queryOption);
        semanticSearchCommand.AddOption(topKOption);
        semanticSearchCommand.SetHandler<string, int?>(async (query, top) =>
        {
            try
            {
                var args = new Dictionary<string, object> { ["query"] = query };
                if (top.HasValue)
                {
                    args["topK"] = top.Value;
                }
                
                var result = await client.CallToolAsync("semantic_search", args);
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                if (jsonText != null)
                {
                    var response = JsonSerializer.Deserialize<SemanticSearchResponse>(jsonText);
                    
                    await Console.Out.WriteLineAsync($"Found {response!.Results.Length} results:\n");
                    foreach (var match in response.Results)
                    {
                        await Console.Out.WriteLineAsync($"File: {match.FilePath}");
                        if (!string.IsNullOrEmpty(match.ParentScope))
                        {
                            await Console.Out.WriteLineAsync($"Scope: {match.ParentScope}");
                        }
                        await Console.Out.WriteLineAsync($"Lines {match.StartLine}-{match.EndLine} (Score: {match.Score}%)");
                        await Console.Out.WriteLineAsync("Content:");
                        await Console.Out.WriteLineAsync(match.Content);
                        await Console.Out.WriteLineAsync(new string('-', 80));
                        await Console.Out.WriteLineAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, queryOption, topKOption);

        // Analyze Packages command
        var analyzePackagesCommand = new Command("analyze-packages", "Analyze NuGet packages in all projects in the base directory");
        analyzePackagesCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("analyze_packages", []);
                var jsonText = result.Content.First(c => c.Type == "text").Text;
                
                // Try to deserialize to our expected type
                try
                {
                    if (!string.IsNullOrEmpty(jsonText))
                    {
                        // Try to deserialize as JDocument first to inspect the structure
                        var jsonObj = JsonDocument.Parse(jsonText);
                        var rootElement = jsonObj.RootElement;
                        
                        // Check if the response is an error message
                        if (rootElement.ValueKind == JsonValueKind.Object && 
                            rootElement.TryGetProperty("Error", out var errorElement))
                        {
                            await Console.Out.WriteLineAsync($"Error from server: {errorElement}");
                            return;
                        }
                        
                        // Check if the response is a message
                        if (rootElement.ValueKind == JsonValueKind.Object && 
                            rootElement.TryGetProperty("Message", out var messageElement))
                        {
                            await Console.Out.WriteLineAsync($"Message from server: {messageElement}");
                            return;
                        }
                        
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var projectAnalyses = JsonSerializer.Deserialize<List<ProjectPackageAnalysis>>(jsonText, options);

                        if (projectAnalyses == null || projectAnalyses.Count == 0)
                        {
                            await Console.Out.WriteLineAsync("No projects or packages found in the base directory.");
                            return;
                        }

                        await Console.Out.WriteLineAsync($"Found {projectAnalyses.Count} project(s) with packages:\n");

                        foreach (var projectAnalysis in projectAnalyses)
                        {
                            await Console.Out.WriteLineAsync($"Project: {projectAnalysis.ProjectPath}");
                            
                            if (projectAnalysis.Packages.Count == 0)
                            {
                                await Console.Out.WriteLineAsync("  No packages found in this project.\n");
                                continue;
                            }

                            await Console.Out.WriteLineAsync($"  Found {projectAnalysis.Packages.Count} package(s):");
                            
                            foreach (var package in projectAnalysis.Packages)
                            {
                                var statusSymbol = package.HasSecurityIssues ? "🔴" : 
                                                (package.HasUpdate ? "🔄" : 
                                                (!package.IsUsed ? "⚠️" : "✅"));
                                
                                await Console.Out.WriteLineAsync($"  - {statusSymbol} {package.PackageId} ({package.Version}{(package.HasUpdate ? $" → {package.LatestVersion}" : "")})");
                                
                                if (!string.IsNullOrEmpty(package.RecommendedAction))
                                {
                                    await Console.Out.WriteLineAsync($"    {package.RecommendedAction}");
                                }
                                
                                if (package.UsageLocations.Count > 0)
                                {
                                    await Console.Out.WriteLineAsync($"    Used in {package.UsageLocations.Count} location(s)");
                                }
                            }
                            
                            await Console.Out.WriteLineAsync();
                        }
                    }
                }
                catch (JsonException ex)
                {
                    await Console.Error.WriteLineAsync($"Error parsing JSON response: {ex.Message}");
                    await Console.Error.WriteLineAsync("Please ensure the server and client models are compatible.");
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error analyzing packages: {ex.Message}");
            }
        });

        rootCommand.AddCommand(helloCommand);
        rootCommand.AddCommand(setBaseDirCommand);
        rootCommand.AddCommand(getBaseDirCommand);
        rootCommand.AddCommand(listProjectsCommand);
        rootCommand.AddCommand(listFilesCommand);
        rootCommand.AddCommand(openFileCommand);
        rootCommand.AddCommand(searchCodeCommand);
        rootCommand.AddCommand(listSolutionsCommand);
        rootCommand.AddCommand(listProjectsInDirCommand);
        rootCommand.AddCommand(listSourceFilesCommand);
        rootCommand.AddCommand(addIgnorePatternsCommand);
        rootCommand.AddCommand(getIgnorePatternsCommand);
        rootCommand.AddCommand(clearIgnorePatternsCommand);
        rootCommand.AddCommand(removeIgnorePatternsCommand);
        rootCommand.AddCommand(getStateFileLocationCommand);
        rootCommand.AddCommand(semanticSearchCommand);
        rootCommand.AddCommand(analyzePackagesCommand);
        
        return await rootCommand.InvokeAsync(args);
    }
}