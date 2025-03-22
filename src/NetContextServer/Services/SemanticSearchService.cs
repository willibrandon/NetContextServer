using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using NetContextServer.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NetContextServer.Services
{
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
        private bool _credentialsAvailable = false;

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

        private static bool ShouldIgnoreFile(string filePath)
        {
            // Get user patterns from IgnorePatternService
            var userPatternsJson = IgnorePatternService.GetIgnorePatterns();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            try
            {
                var response = JsonSerializer.Deserialize<IgnorePatternsResponse>(userPatternsJson, options);
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

        private static string WildcardToRegex(string pattern)
        {
            return "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
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
} 