using NetContextServer.Extensions;
using System.Text.Json;

namespace NetContextServer.Services;

/// <summary>
/// Provides functionality for searching code within the codebase using both text-based and semantic search capabilities.
/// </summary>
internal static class CodeSearchService
{
    private static SemanticSearchService _semanticSearch = new();
    private static bool _isIndexed = false;

    /// <summary>
    /// Ensures that the codebase is indexed for semantic search capabilities.
    /// </summary>
    /// <returns>A task representing the asynchronous indexing operation.</returns>
    private static async Task EnsureIndexedAsync()
    {
        if (!_isIndexed)
        {
            Console.WriteLine("Starting initial indexing...");
            _semanticSearch ??= new SemanticSearchService();
            
            var baseDir = FileValidationService.BaseDirectory;
            Console.WriteLine($"Searching for files in: {baseDir}");
            
            var files = Directory.GetFiles(baseDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs") || f.EndsWith(".fs") || f.EndsWith(".vb") || 
                           f.EndsWith(".fsx") || f.EndsWith(".fsi") || f.EndsWith(".cshtml") || 
                           f.EndsWith(".vbhtml") || f.EndsWith(".razor"))
                .Where(f => !IgnorePatternService.ShouldIgnoreFile(f))
                .ToList();
                
            Console.WriteLine($"Found {files.Count} files to index");
            await _semanticSearch.IndexFilesAsync(files);
            _isIndexed = true;
            Console.WriteLine("Initial indexing completed");
        }
    }

    /// <summary>
    /// Performs a text-based search across the codebase for exact matches of the search text.
    /// </summary>
    /// <param name="searchText">The text to search for in the codebase.</param>
    /// <returns>An array of strings containing the search results in the format "filepath:line: content".</returns>
    public static string[] SearchCode(string searchText)
    {
        var results = new List<string>();
        
        foreach (var pattern in FileService.DotNetFilePatterns)
        {
            var files = Directory.GetFiles(FileValidationService.BaseDirectory, pattern, SearchOption.AllDirectories)
                .Where(f => !IgnorePatternService.ShouldIgnoreFile(f));
            
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add($"{file}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
        }
        
        return [.. results];
    }

    /// <summary>
    /// Performs a semantic search across the indexed codebase using natural language understanding.
    /// </summary>
    /// <param name="query">The natural language query to search for.</param>
    /// <param name="topK">The maximum number of results to return.</param>
    /// <returns>A JSON string containing the search results with file paths, line numbers, and relevance scores.</returns>
    public static async Task<string> SemanticSearchAsync(string query, int topK)
    {
        await EnsureIndexedAsync();

        var results = await _semanticSearch.SearchAsync(query, topK);
        
        if (!results.Any())
        {
            // Return an empty results array instead of an error
            return JsonSerializer.Serialize(new
            {
                Results = Array.Empty<object>()
            });
        }

        return JsonSerializer.Serialize(new
        {
            Results = results.Select(r => new
            {
                FilePath = FileValidationService.GetRelativePath(r.Snippet.FilePath),
                r.Snippet.StartLine,
                r.Snippet.EndLine,
                Content = StringExtensions.FormatCodeContent(r.Snippet.Content),
                Score = Math.Round(r.Score * 100, 1), // Convert to percentage
                ParentScope = GetParentScope(r.Snippet.Content)
            })
        });
    }

    /// <summary>
    /// Extracts the parent scope (namespace, class, interface, etc.) from a code snippet.
    /// </summary>
    /// <param name="content">The code content to analyze.</param>
    /// <returns>A dot-separated string representing the hierarchical scope of the code.</returns>
    private static string GetParentScope(string content)
    {
        List<string> scopeParts = [];
        string[] lines = content.Split('\n');

        foreach (string line in lines)
        {
            string trimmed = line.TrimStart();
            
            // Detect namespace
            if (trimmed.StartsWith("namespace "))
            {
                string namespaceName = StringExtensions.ExtractName(trimmed, "namespace");
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    scopeParts.Add(namespaceName);
                }
            }
            // Detect class, interface, struct, enum
            else if (trimmed.Contains("class "))
            {
                string className = StringExtensions.ExtractName(trimmed, "class");
                if (!string.IsNullOrEmpty(className))
                {
                    scopeParts.Add(className);
                }
            }
            else if (trimmed.Contains("interface "))
            {
                string interfaceName = StringExtensions.ExtractName(trimmed, "interface");
                if (!string.IsNullOrEmpty(interfaceName))
                {
                    scopeParts.Add(interfaceName);
                }
            }
            else if (trimmed.Contains("struct "))
            {
                string structName = StringExtensions.ExtractName(trimmed, "struct");
                if (!string.IsNullOrEmpty(structName))
                {
                    scopeParts.Add(structName);
                }
            }
            else if (trimmed.Contains("enum "))
            {
                string enumName = StringExtensions.ExtractName(trimmed, "enum");
                if (!string.IsNullOrEmpty(enumName))
                {
                    scopeParts.Add(enumName);
                }
            }
            // Detect methods
            else if ((trimmed.Contains("void ") || 
                     trimmed.Contains("async ") || 
                     trimmed.Contains("Task ") ||
                     trimmed.Contains("public ") || 
                     trimmed.Contains("private ") || 
                     trimmed.Contains("protected ") ||
                     trimmed.Contains("internal ")) &&
                     trimmed.Contains('(') && 
                     !trimmed.StartsWith("//") && 
                     !trimmed.StartsWith("/*"))
            {
                string methodName = StringExtensions.ExtractMethodName(trimmed);
                if (!string.IsNullOrEmpty(methodName))
                {
                    scopeParts.Add(methodName);
                }
            }
        }

        return string.Join(".", scopeParts);
    }
}
