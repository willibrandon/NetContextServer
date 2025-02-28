using NetContextServer.Extensions;
using System.Text.Json;

namespace NetContextServer.Services;

internal static class CodeSearchService
{
    private static SemanticSearchService _semanticSearch = new();
    private static bool _isIndexed = false;

    private static async Task EnsureIndexedAsync()
    {
        if (!_isIndexed)
        {
            _semanticSearch ??= new SemanticSearchService();
            var files = Directory.GetFiles(FileValidationService.BaseDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs") || f.EndsWith(".fs") || f.EndsWith(".vb"));
            await _semanticSearch.IndexFilesAsync(files);
            _isIndexed = true;
        }
    }

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

    public static async Task<string> SemanticSearchAsync(string query, int topK)
    {
        await EnsureIndexedAsync();

        var results = await _semanticSearch.SearchAsync(query, topK);
        
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
