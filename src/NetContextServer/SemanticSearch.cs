using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
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

public class SemanticSearch
{
    private const int CHUNK_SIZE = 1000;
    private const int OVERLAP = 100;
    private readonly Kernel _kernel;
    private readonly Dictionary<string, CodeSnippet> _cache = new();
    private readonly HashSet<string> _indexedFiles = new();
    private static readonly string[] _defaultIgnorePatterns = new[] 
    {
        "**/obj/**",
        "**/bin/**",
        "**/*.generated.cs",
        "**/*.designer.cs",
        "**/*.g.cs",
        "**/*.AssemblyInfo.cs"
    };

    public SemanticSearch()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("Azure OpenAI credentials not found in environment variables.");
        }

        var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010
        builder.AddAzureOpenAITextEmbeddingGeneration(
            "text-embedding-ada-002",
            endpoint!,
            key!
        );
#pragma warning restore SKEXP0010
        _kernel = builder.Build();
    }

    public async Task IndexFilesAsync(IEnumerable<string> filePaths)
    {
#pragma warning disable SKEXP0001
        var service = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
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
#pragma warning disable SKEXP0001
        var service = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
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

    private bool ShouldIgnoreFile(string filePath)
    {
        // Get user patterns from Program.cs
        var userPatterns = NetConextServer.GetIgnorePatterns();
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
        
        for (int i = 0; i < lines.Length; i += CHUNK_SIZE - OVERLAP)
        {
            var chunkLines = lines.Skip(i).Take(CHUNK_SIZE).ToList();
            chunks.Add(string.Join("\n", chunkLines));
        }
        
        return chunks;
    }

    private static int[] GetLineNumbers(string content)
    {
        var lineCount = content.Count(c => c == '\n') + 1;
        return Enumerable.Range(1, lineCount).ToArray();
    }

    private static bool IsMeaningfulCode(string chunk)
    {
        // Ignore chunks that are mostly whitespace, comments, or very short
        var meaningfulLines = chunk.Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !line.TrimStart().StartsWith("//"))
            .Where(line => !line.TrimStart().StartsWith("/*"))
            .Where(line => !line.TrimStart().StartsWith("*"))
            .Count();

        return meaningfulLines >= 3;
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