using ModelContextProtocol.Server;
using NetContextServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NetContextServer.Tools;

/// <summary>
/// Provides MCP tools for searching code within the codebase using both text-based and semantic search capabilities.
/// </summary>
[McpToolType]
public static class SearchTools
{
    /// <summary>
    /// Default JSON serializer options used for search results output.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Performs a text-based search across all code files for the specified text.
    /// </summary>
    /// <param name="searchText">The exact text string to search for in the codebase.</param>
    /// <returns>A JSON string containing an array of matches with file paths and line numbers.</returns>
    [McpTool("search_code")]
    [Description("Performs a text-based search across all code files for the specified text.")]
    public static string SearchCode(
        [Description("The exact text string to search for in the codebase")]
        string searchText) => 
        JsonSerializer.Serialize(CodeSearchService.SearchCode(searchText), DefaultJsonOptions);

    /// <summary>
    /// Performs a semantic similarity search across the codebase using AI.
    /// </summary>
    /// <param name="query">Natural language description of the code you're looking for.</param>
    /// <param name="topK">Optional: Number of results to return (default: 5).</param>
    /// <returns>A JSON string containing semantically relevant code snippets ranked by relevance.</returns>
    /// <remarks>
    /// This method uses AI embeddings to understand the semantic meaning of the query and find relevant code,
    /// even if it doesn't contain the exact words used in the query.
    /// </remarks>
    [McpTool("semantic_search")]
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
