namespace NetContextServer.Models
{
    public class CodeSnippet
    {
        public required string FilePath { get; init; }
        public required string Content { get; init; }
        public required int StartLine { get; init; }
        public required int EndLine { get; init; }
        public required ReadOnlyMemory<float> Embedding { get; init; }
    }
} 