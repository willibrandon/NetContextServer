using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.RegularExpressions;

namespace NetContextServer;

public class CodeSnippet
{
    public string FilePath { get; set; } = "";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Content { get; set; } = "";
    public float[] Embedding { get; set; } = [];
}

public class SemanticSearchResult
{
    public string FilePath { get; set; } = "";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Content { get; set; } = "";
    public float Score { get; set; }
}

public class SemanticSearch
{
    private readonly Kernel _kernel;
    private readonly List<CodeSnippet> _snippets = [];
    private const int CHUNK_SIZE = 10; // Number of lines per chunk
    private const int OVERLAP = 2; // Number of lines to overlap between chunks

    public SemanticSearch()
    {
        var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        builder.AddAzureOpenAITextEmbeddingGeneration(
            "text-embedding-ada-002",
            Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "",
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? ""
        );
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        _kernel = builder.Build();
    }

    public async Task IndexFilesAsync(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath)) continue;

            var lines = await File.ReadAllLinesAsync(filePath);
            var chunks = ChunkCode(lines);

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var embedding = await GetEmbeddingAsync(chunk.Content);
                
                _snippets.Add(new CodeSnippet
                {
                    FilePath = filePath,
                    StartLine = chunk.StartLine,
                    EndLine = chunk.EndLine,
                    Content = chunk.Content,
                    Embedding = embedding
                });
            }
        }
    }

    public async Task<List<SemanticSearchResult>> SearchAsync(string query, int topK = 5)
    {
        var queryEmbedding = await GetEmbeddingAsync(query);
        
        var results = _snippets
            .Select(snippet => new SemanticSearchResult
            {
                FilePath = snippet.FilePath,
                StartLine = snippet.StartLine,
                EndLine = snippet.EndLine,
                Content = snippet.Content,
                Score = CosineSimilarity(queryEmbedding, snippet.Embedding)
            })
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return results;
    }

    private async Task<float[]> GetEmbeddingAsync(string text)
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var embedding = await _kernel.GetRequiredService<ITextEmbeddingGenerationService>()
            .GenerateEmbeddingAsync(text);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return embedding.ToArray();
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        float dotProduct = 0;
        float normA = 0;
        float normB = 0;
        
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        
        return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private List<(int StartLine, int EndLine, string Content)> ChunkCode(string[] lines)
    {
        var chunks = new List<(int StartLine, int EndLine, string Content)>();
        
        for (int i = 0; i < lines.Length; i += CHUNK_SIZE - OVERLAP)
        {
            var endLine = Math.Min(i + CHUNK_SIZE, lines.Length);
            var chunk = string.Join("\n", lines[i..endLine]);
            
            // Skip chunks that are just whitespace or comments
            if (string.IsNullOrWhiteSpace(chunk) || IsOnlyComments(chunk))
                continue;
                
            chunks.Add((i + 1, endLine, chunk));
        }
        
        return chunks;
    }

    private bool IsOnlyComments(string text)
    {
        // Remove C# comments and check if anything meaningful remains
        var withoutComments = Regex.Replace(text, @"(//.*|/\*[\s\S]*?\*/)", "");
        return string.IsNullOrWhiteSpace(withoutComments);
    }
} 