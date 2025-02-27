﻿using MCPSharp;
using System.CommandLine;
using System.Text.Json;

class Program
{
    static async Task<int> Main(string[] args)
    {
        using var client = new MCPClient("NetContextClient", "1.0.0", "NetContextServer.exe");
        var rootCommand = new RootCommand("NetContext Client - MCP client for .NET codebase interaction");
        
        // Hello command
        var helloCommand = new Command("hello", "Send a hello request to the server");
        helloCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("hello");
                await Console.Out.WriteLineAsync(result.Content[0].Text);
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
                var result = await client.CallToolAsync("set_base_directory", new Dictionary<string, object> { { "directory", directory } });
                await Console.Out.WriteLineAsync("Base directory set successfully. ");

                if (result != null && result.Content != null && result.Content.Length > 0)
                {
                    await Console.Out.WriteLineAsync(result.Content[0]?.Text);
                }
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

        // List Solutions command
        var listSolutionsCommand = new Command("list-solutions", "List all solution files");
        listSolutionsCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("list_solutions");
                var solutions = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
                foreach (var solution in solutions!)
                {
                    await Console.Out.WriteLineAsync(solution);
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
                var result = await client.CallToolAsync("list_projects_in_dir", new Dictionary<string, object> { { "directory", directory } });
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
        }, directoryOption);

        // List Source Files command
        var listSourceFilesCommand = new Command("list-source-files", "List all source files in a project");
        var projectDirOption = new Option<string>("--project-dir", "The project directory") { IsRequired = true };
        listSourceFilesCommand.AddOption(projectDirOption);
        listSourceFilesCommand.SetHandler(async (string projectDir) =>
        {
            try
            {
                var result = await client.CallToolAsync("list_source_files", new Dictionary<string, object> { { "projectDir", projectDir } });
                var files = JsonSerializer.Deserialize<string[]>(result.Content[0].Text);
                foreach (var file in files!.Where(f => !f.Contains("\\obj\\")))
                {
                    await Console.Out.WriteLineAsync(file);
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
                var result = await client.CallToolAsync("add_ignore_patterns", new Dictionary<string, object> { { "patterns", patterns } });
                var response = JsonSerializer.Deserialize<AddIgnorePatternsResponse>(result.Content[0].Text);
                
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
                var result = await client.CallToolAsync("remove_ignore_patterns", new Dictionary<string, object> { { "patterns", patterns } });
                var response = JsonSerializer.Deserialize<RemoveIgnorePatternsResponse>(result.Content[0].Text);

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
                var result = await client.CallToolAsync("get_state_file_location");
                var response = JsonSerializer.Deserialize<StateFileLocationResponse>(result.Content[0].Text);
                await Console.Out.WriteLineAsync($"State file location: {response!.StateFilePath}");
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
                var result = await client.CallToolAsync("get_ignore_patterns");
                var response = JsonSerializer.Deserialize<IgnorePatternsResponse>(result.Content[0].Text);
                
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
                var result = await client.CallToolAsync("clear_ignore_patterns");
                var response = JsonSerializer.Deserialize<IgnorePatternsResponse>(result.Content[0].Text);
                
                await Console.Out.WriteLineAsync("Cleared all user-added patterns.");
                await Console.Out.WriteLineAsync("\nRemaining default patterns:");
                foreach (var pattern in response!.DefaultPatterns)
                {
                    await Console.Out.WriteLineAsync($"  {pattern}");
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        // Throw Exception command (for testing error handling)
        var throwExceptionCommand = new Command("throw-exception", "Throw an exception (for testing)");
        throwExceptionCommand.SetHandler(async () =>
        {
            try
            {
                var result = await client.CallToolAsync("throw_exception");
                await Console.Out.WriteLineAsync(result.Content[0].Text);
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
                var args = new Dictionary<string, object> { { "query", query } };
                if (top.HasValue)
                {
                    args["topK"] = top.Value;
                }
                
                var result = await client.CallToolAsync("semantic_search", args);
                var response = JsonSerializer.Deserialize<SemanticSearchResponse>(result.Content[0].Text);
                
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
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, queryOption, topKOption);

        // Analyze Packages command
        var analyzePackagesCommand = new Command("analyze-packages", "Analyze NuGet packages in a project");
        var analyzeProjectPathOption = new Option<string>("--project-path", "The project path to analyze") { IsRequired = true };
        analyzePackagesCommand.AddOption(analyzeProjectPathOption);
        analyzePackagesCommand.SetHandler(async (string projectPath) =>
        {
            try
            {
                var result = await client.CallToolAsync("analyze_packages", new Dictionary<string, object> { { "projectPath", projectPath } });
                var analyses = JsonSerializer.Deserialize<PackageAnalysis[]>(result.Content[0].Text);

                if (analyses == null || analyses.Length == 0)
                {
                    await Console.Out.WriteLineAsync("No packages found in the project.");
                    return;
                }

                await Console.Out.WriteLineAsync($"Found {analyses.Length} package(s):\n");
                foreach (var analysis in analyses)
                {
                    await Console.Out.WriteLineAsync($"Package: {analysis.PackageId}");
                    await Console.Out.WriteLineAsync($"Current Version: {analysis.Version}");
                    await Console.Out.WriteLineAsync($"Latest Version: {analysis.LatestVersion ?? "Unknown"}");
                    await Console.Out.WriteLineAsync($"Status: {(analysis.IsUsed ? "Used" : "Unused")}");
                    
                    if (analysis.UsageLocations.Length > 0)
                    {
                        await Console.Out.WriteLineAsync("Used in:");
                        foreach (var location in analysis.UsageLocations)
                        {
                            await Console.Out.WriteLineAsync($"  - {location}");
                        }
                    }

                    if (analysis.HasSecurityIssues)
                    {
                        await Console.Out.WriteLineAsync("⚠️ Has security issues!");
                    }

                    if (!string.IsNullOrEmpty(analysis.RecommendedAction))
                    {
                        await Console.Out.WriteLineAsync($"Recommendation: {analysis.RecommendedAction}");
                    }

                    await Console.Out.WriteLineAsync(new string('-', 80));
                    await Console.Out.WriteLineAsync();
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, analyzeProjectPathOption);

        rootCommand.AddCommand(helloCommand);
        rootCommand.AddCommand(setBaseDirCommand);
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
        rootCommand.AddCommand(throwExceptionCommand);
        rootCommand.AddCommand(semanticSearchCommand);
        rootCommand.AddCommand(analyzePackagesCommand);
        
        return await rootCommand.InvokeAsync(args);
    }

    private class IgnorePatternsResponse
    {
        public string[] DefaultPatterns { get; set; } = [];
        public string[] UserPatterns { get; set; } = [];
        public string[] AllPatterns { get; set; } = [];
    }

    private class AddIgnorePatternsResponse : IgnorePatternsResponse
    {
        public string[] ValidPatternsAdded { get; set; } = [];
        public string[] InvalidPatterns { get; set; } = [];
    }

    private class RemoveIgnorePatternsResponse : IgnorePatternsResponse
    {
        public string[] RemovedPatterns { get; set; } = [];
        public string[] NotFoundPatterns { get; set; } = [];
        public string[] DefaultPatternsSkipped { get; set; } = [];
    }

    private class StateFileLocationResponse
    {
        public string StateFilePath { get; set; } = "";
    }

    private class SemanticSearchResponse
    {
        public SemanticSearchResult[] Results { get; set; } = [];
    }

    private class SemanticSearchResult
    {
        public string FilePath { get; set; } = "";
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string Content { get; set; } = "";
        public float Score { get; set; }
        public string ParentScope { get; set; } = "";
    }

    private class PackageAnalysis
    {
        public string PackageId { get; set; } = "";
        public string Version { get; set; } = "";
        public bool IsUsed { get; set; }
        public string[] UsageLocations { get; set; } = [];
        public string? LatestVersion { get; set; }
        public bool HasSecurityIssues { get; set; }
        public string? RecommendedAction { get; set; }
    }
}