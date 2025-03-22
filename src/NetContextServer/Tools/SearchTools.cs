using ModelContextProtocol;
using ModelContextProtocol.Server;
using NetContextServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NetContextServer.Tools;

[McpToolType]
public static class SearchTools
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true
    };

    [McpTool]
    [Description("Performs a text-based search across all code files for the specified text.")]
    public static string SearchCode(
        [Description("The exact text string to search for in the codebase")]
        string searchText) => 
        JsonSerializer.Serialize(CodeSearchService.SearchCode(searchText), DefaultJsonOptions);

    [McpTool]
    [Description("Performs a semantic similarity search across the codebase using AI.")]
    public static async Task<string> SemanticSearchAsync(
        [Description("Natural language description of the code you're looking for")]
        string query,
        [Description("Optional: Number of results to return (default: 5)")]
        int? topK = 5)
    {
        try
        {
            FileValidationService.EnsureBaseDirectorySet();
            return await CodeSearchService.SemanticSearchAsync(query, topK ?? 5);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Error = ex.Message }, DefaultJsonOptions);
        }
    }
} 