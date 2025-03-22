using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using NetContextServer.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NetContextServer.Services;

/// <summary>
/// Provides semantic search capabilities for code files using Azure OpenAI embeddings.
/// Enables natural language search across codebases by understanding code context and meaning.
/// </summary>
internal class SemanticSearchService
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
    private readonly bool _credentialsAvailable = false;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the SemanticSearchService class.
    /// Attempts to initialize Azure OpenAI client using environment variables.
    /// </summary>
    public SemanticSearchService()
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

    /// <summary>
    /// Indexes the provided files for semantic search by generating embeddings for code chunks.
    /// </summary>
    /// <param name="filePaths">Collection of file paths to index.</param>
    /// <returns>A task representing the asynchronous indexing operation.</returns>
    /// <remarks>
    /// Files are chunked into smaller segments with overlap for better context preservation.
    /// Only meaningful code chunks are indexed, skipping generated files and empty sections.
    /// </remarks>
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
            if (_indexedFiles.Contains(filePath))
            {
                continue;
            }
            
            if (ShouldIgnoreFile(filePath))
            {
                continue;
            }

            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var chunks = ChunkCode(content);
                var lineNumbers = GetLineNumbers(content);

                int meaningfulChunks = 0;

                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    if (!IsMeaningfulCode(chunk))
                    {
                        continue;
                    }

                    meaningfulChunks++;
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

    /// <summary>
    /// Performs a semantic search across indexed code files using natural language queries.
    /// </summary>
    /// <param name="query">The natural language query to search for.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <returns>Collection of code snippets ranked by relevance to the query.</returns>
    public async Task<IEnumerable<(CodeSnippet Snippet, double Score)>> SearchAsync(string query, int topK = 5)
    {
        if (!_credentialsAvailable)
        {
            // Return an empty result if credentials aren't available
            return [];
        }

        if (_cache.Count == 0)
        {
            Console.WriteLine("Warning: Cannot search because no files have been indexed");
            return [];
        }

#pragma warning disable SKEXP0001
        var service = _kernel!.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001

        var queryEmbedding = await service.GenerateEmbeddingAsync(query);
        var results = _cache.Values
            .Select(snippet => (
                Snippet: snippet,
                Score: CosineSimilarity(queryEmbedding, snippet.Embedding)
            ))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        return results;
    }

    /// <summary>
    /// Determines whether a file should be ignored based on ignore patterns.
    /// </summary>
    /// <param name="filePath">The path of the file to check.</param>
    /// <returns>True if the file should be ignored; otherwise, false.</returns>
    private static bool ShouldIgnoreFile(string filePath)
    {
        // Get user patterns from IgnorePatternService
        var userPatternsJson = IgnorePatternService.GetIgnorePatterns();
        
        try
        {
            var response = JsonSerializer.Deserialize<IgnorePatternsResponse>(userPatternsJson, DefaultJsonOptions);
            var allPatterns = _defaultIgnorePatterns.Concat(response?.UserPatterns ?? []);

            foreach (var pattern in allPatterns)
            {
                var regex = WildcardToRegex(pattern);
                if (Regex.IsMatch(filePath, regex, RegexOptions.IgnoreCase))
                    return true;
            }
        }
        catch (Exception)
        {
            // If we can't parse the patterns, just use the defaults
            foreach (var pattern in _defaultIgnorePatterns)
            {
                var regex = WildcardToRegex(pattern);
                if (Regex.IsMatch(filePath, regex, RegexOptions.IgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private class IgnorePatternsResponse
    {
        public string[] DefaultPatterns { get; set; } = [];
        public string[] UserPatterns { get; set; } = [];
        public string[] AllPatterns { get; set; } = [];
    }

    /// <summary>
    /// Converts a wildcard pattern to a regular expression pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to convert.</param>
    /// <returns>A regular expression pattern equivalent to the wildcard pattern.</returns>
    private static string WildcardToRegex(string pattern)
    {
        return "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/\\\\]*")
            .Replace("\\?", ".") + "$";
    }

    /// <summary>
    /// Chunks code content into smaller segments for semantic analysis.
    /// </summary>
    /// <param name="content">The code content to chunk.</param>
    /// <returns>A list of code chunks with context preservation.</returns>
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

    /// <summary>
    /// Gets an array of line numbers for the content.
    /// </summary>
    /// <param name="content">The content to get line numbers for.</param>
    /// <returns>An array of line numbers starting from 1.</returns>
    private static int[] GetLineNumbers(string content)
    {
        var lineCount = content.Count(c => c == '\n') + 1;
        return [.. Enumerable.Range(1, lineCount)];
    }

    /// <summary>
    /// Determines whether a code chunk contains meaningful code worth indexing.
    /// </summary>
    /// <param name="chunk">The code chunk to analyze.</param>
    /// <returns>True if the chunk contains meaningful code; otherwise, false.</returns>
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

    /// <summary>
    /// Calculates the cosine similarity between two embedding vectors.
    /// </summary>
    /// <param name="a">First embedding vector.</param>
    /// <param name="b">Second embedding vector.</param>
    /// <returns>The cosine similarity score between the vectors.</returns>
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
