using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NetContextServer;

public class CodeSnippet
{
    public required string FilePath { get; init; }
    public required string Content { get; init; }
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
    public required ReadOnlyMemory<float> Embedding { get; init; }
}

public class SearchResult
{
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public string ParentScope { get; set; } = string.Empty;
}

public class SemanticSearch
{
    private const int CHUNK_SIZE = 200;
    private const int OVERLAP = 20;
    private const int CONTEXT_LINES = 3;
    private readonly Kernel? _kernel;
    private readonly Dictionary<string, CodeSnippet> _cache = [];
    private readonly HashSet<string> _indexedFiles = [];
    private static readonly string[] _defaultIgnorePatterns =
    [
        "**/obj/**",
        "**/bin/**",
        "**/*.generated.cs",
        "**/*.designer.cs",
        "**/*.g.cs",
        "**/*.AssemblyInfo.cs"
    ];
    private bool _credentialsAvailable = false;

    public SemanticSearch()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
        {
            Console.WriteLine("Warning: Azure OpenAI credentials not found in environment variables. Semantic search will be unavailable.");
            _credentialsAvailable = false;
            return;
        }

        try
        {
            var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010
            builder.AddAzureOpenAITextEmbeddingGeneration(
                "text-embedding-ada-002",
                endpoint!,
                key!
            );
#pragma warning restore SKEXP0010
            _kernel = builder.Build();
            _credentialsAvailable = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to initialize Azure OpenAI client: {ex.Message}");
            _credentialsAvailable = false;
        }
    }

    public async Task IndexFilesAsync(IEnumerable<string> filePaths)
    {
        if (!_credentialsAvailable)
        {
            // Skip indexing if credentials aren't available
            return;
        }

#pragma warning disable SKEXP0001
        var service = _kernel!.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001

        foreach (var filePath in filePaths)
        {
            if (_indexedFiles.Contains(filePath) || ShouldIgnoreFile(filePath))
                continue;

            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var chunks = ChunkCode(content);
                var lineNumbers = GetLineNumbers(content);

                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    if (!IsMeaningfulCode(chunk))
                        continue;

                    var startLine = lineNumbers[i * CHUNK_SIZE];
                    var endLine = lineNumbers[Math.Min((i + 1) * CHUNK_SIZE - 1, lineNumbers.Length - 1)];
                    
                    var embedding = await service.GenerateEmbeddingAsync(chunk);
                    var key = $"{filePath}:{startLine}:{endLine}";
                    
                    _cache[key] = new CodeSnippet
                    {
                        FilePath = filePath,
                        Content = chunk,
                        StartLine = startLine,
                        EndLine = endLine,
                        Embedding = embedding
                    };
                }
                _indexedFiles.Add(filePath);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error indexing {filePath}: {ex.Message}");
            }
        }
    }

    public async Task<IEnumerable<(CodeSnippet Snippet, double Score)>> SearchAsync(string query, int topK = 5)
    {
        if (!_credentialsAvailable)
        {
            // Return an empty result if credentials aren't available
            return [];
        }

        if (_cache.Count == 0)
        {
            return Enumerable.Empty<(CodeSnippet, double)>();
        }

#pragma warning disable SKEXP0001
        var service = _kernel!.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001

        var queryEmbedding = await service.GenerateEmbeddingAsync(query);

        return _cache.Values
            .Select(snippet => (
                Snippet: snippet,
                Score: CosineSimilarity(queryEmbedding, snippet.Embedding)
            ))
            .OrderByDescending(x => x.Score)
            .Take(topK);
    }

    public async Task<List<SearchResult>> SemanticSearchAsync(string query, int topK = 5)
    {
        if (!_credentialsAvailable)
        {
            // Return an empty result with a message if credentials aren't available
            return
            [
                new SearchResult
                {
                    FilePath = "semantic_search_unavailable",
                    StartLine = 0,
                    EndLine = 0,
                    Content = "Semantic search is unavailable because Azure OpenAI credentials are not configured.",
                    Score = 0,
                    ParentScope = "N/A"
                }
            ];
        }

        var results = await SearchAsync(query, topK);
        
        return results
            .Select(x => new SearchResult
            {
                FilePath = x.Snippet.FilePath,
                StartLine = x.Snippet.StartLine,
                EndLine = x.Snippet.EndLine,
                Content = x.Snippet.Content,
                Score = x.Score,
                ParentScope = GetParentScope(x.Snippet.FilePath, x.Snippet.StartLine)
            })
            .ToList();
    }

    private static string GetParentScope(string filePath, int lineNumber)
    {
        try
        {
            if (!File.Exists(filePath))
                return "Unknown";
            
            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');
            
            // Get the lines before the target line to find the parent scope
            var relevantLines = lines.Take(lineNumber).ToArray();
            
            List<string> scopeParts = [];
            
            // Process lines in reverse to find the closest parent scopes
            for (int i = relevantLines.Length - 1; i >= 0; i--)
            {
                string line = relevantLines[i].TrimStart();
                
                // Detect namespace
                if (line.StartsWith("namespace "))
                {
                    string namespaceName = ExtractName(line, "namespace");
                    if (!string.IsNullOrEmpty(namespaceName))
                    {
                        scopeParts.Insert(0, namespaceName);
                    }
                }
                // Detect class, interface, struct, enum
                else if (line.Contains("class "))
                {
                    string className = ExtractName(line, "class");
                    if (!string.IsNullOrEmpty(className))
                    {
                        scopeParts.Insert(0, className);
                    }
                }
                else if (line.Contains("interface "))
                {
                    string interfaceName = ExtractName(line, "interface");
                    if (!string.IsNullOrEmpty(interfaceName))
                    {
                        scopeParts.Insert(0, interfaceName);
                    }
                }
                else if (line.Contains("struct "))
                {
                    string structName = ExtractName(line, "struct");
                    if (!string.IsNullOrEmpty(structName))
                    {
                        scopeParts.Insert(0, structName);
                    }
                }
                else if (line.Contains("enum "))
                {
                    string enumName = ExtractName(line, "enum");
                    if (!string.IsNullOrEmpty(enumName))
                    {
                        scopeParts.Insert(0, enumName);
                    }
                }
                // Detect methods
                else if ((line.Contains("void ") || 
                         line.Contains("async ") || 
                         line.Contains("Task ") ||
                         line.Contains("public ") || 
                         line.Contains("private ") || 
                         line.Contains("protected ") ||
                         line.Contains("internal ")) &&
                         line.Contains('(') && 
                         !line.StartsWith("//") && 
                         !line.StartsWith("/*"))
                {
                    string methodName = ExtractMethodName(line);
                    if (!string.IsNullOrEmpty(methodName))
                    {
                        scopeParts.Insert(0, methodName);
                        break; // We found the method, no need to look further
                    }
                }
            }
            
            return string.Join(".", scopeParts);
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string ExtractName(string line, string keyword)
    {
        int keywordIndex = line.IndexOf(keyword + " ");
        if (keywordIndex < 0)
            return string.Empty;
        
        string afterKeyword = line[(keywordIndex + keyword.Length + 1)..].Trim();
        
        // Find the end of the name (at first space, opening brace, colon, or parenthesis)
        int endIndex = afterKeyword.Length;
        int spaceIndex = afterKeyword.IndexOf(' ');
        int braceIndex = afterKeyword.IndexOf('{');
        int colonIndex = afterKeyword.IndexOf(':');
        int parenIndex = afterKeyword.IndexOf('(');
        
        if (spaceIndex >= 0 && spaceIndex < endIndex) endIndex = spaceIndex;
        if (braceIndex >= 0 && braceIndex < endIndex) endIndex = braceIndex;
        if (colonIndex >= 0 && colonIndex < endIndex) endIndex = colonIndex;
        if (parenIndex >= 0 && parenIndex < endIndex) endIndex = parenIndex;
        
        if (endIndex > 0)
        {
            return afterKeyword[..endIndex].Trim();
        }
        
        return string.Empty;
    }

    private static string ExtractMethodName(string line)
    {
        // Extract the method name from the line (text before the opening parenthesis)
        int parenIndex = line.IndexOf('(');
        if (parenIndex <= 0)
            return string.Empty;
        
        string beforeParen = line[..parenIndex].Trim();
        
        // Find the last space before the parenthesis
        int lastSpaceIndex = beforeParen.LastIndexOf(' ');
        if (lastSpaceIndex >= 0 && lastSpaceIndex < beforeParen.Length - 1)
        {
            // Return everything after the last space
            return beforeParen[(lastSpaceIndex + 1)..].Trim();
        }
        
        return string.Empty;
    }

    private static bool ShouldIgnoreFile(string filePath)
    {
        // Get user patterns from Program.cs
        var userPatternsJson = NetConextServer.GetIgnorePatterns();
        var userPatterns = JsonSerializer.Deserialize<string[]>(userPatternsJson) ?? [];
        var allPatterns = _defaultIgnorePatterns.Concat(userPatterns);

        foreach (var pattern in allPatterns)
        {
            var regex = WildcardToRegex(pattern);
            if (Regex.IsMatch(filePath, regex, RegexOptions.IgnoreCase))
                return true;
        }
        return false;
    }

    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/\\\\]*")
            .Replace("\\?", ".") + "$";
    }

    private static List<string> ChunkCode(string content)
    {
        var chunks = new List<string>();
        var lines = content.Split('\n');
        var currentChunk = new List<string>();
        var currentDepth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            currentChunk.Add(line);
            
            // Track code block depth
            currentDepth += line.Count(c => c == '{') - line.Count(c => c == '}');

            // Create a new chunk when we:
            // 1. Hit the chunk size limit AND we're at depth 0 (not in middle of a block)
            // 2. OR we have a complete code block
            if ((currentChunk.Count >= CHUNK_SIZE && currentDepth == 0) || 
                (currentDepth == 0 && line.Trim().Length > 0))
            {
                chunks.Add(string.Join("\n", currentChunk));
                // Keep overlap lines for context
                currentChunk = [.. lines.Skip(i - OVERLAP + 1).Take(OVERLAP)];
            }
        }

        // Add any remaining lines
        if (currentChunk.Count != 0)
        {
            chunks.Add(string.Join("\n", currentChunk));
        }

        return chunks;
    }

    private static int[] GetLineNumbers(string content)
    {
        var lineCount = content.Count(c => c == '\n') + 1;
        return [.. Enumerable.Range(1, lineCount)];
    }

    private static bool IsMeaningfulCode(string chunk)
    {
        var lines = chunk.Split('\n');
        
        // Count meaningful lines
        var meaningfulLines = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !line.TrimStart().StartsWith("//"))
            .Where(line => !line.TrimStart().StartsWith("/*"))
            .Where(line => !line.TrimStart().StartsWith('*'))
            .Count();

        // Check if chunk contains important code structures
        var hasCodeStructure = lines.Any(line => 
            line.Contains("class ") ||
            line.Contains("interface ") ||
            line.Contains("struct ") ||
            line.Contains("enum ") ||
            line.Contains("void ") ||
            line.Contains("async ") ||
            line.Contains("return ") ||
            line.Contains("public ") ||
            line.Contains("private ") ||
            line.Contains("protected "));

        return meaningfulLines >= 3 || hasCodeStructure;
    }

    private static double CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var aSpan = a.Span;
        var bSpan = b.Span;
        var dotProduct = 0.0;
        var normA = 0.0;
        var normB = 0.0;

        for (int i = 0; i < aSpan.Length; i++)
        {
            dotProduct += aSpan[i] * bSpan[i];
            normA += aSpan[i] * aSpan[i];
            normB += bSpan[i] * bSpan[i];
        }

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
} 