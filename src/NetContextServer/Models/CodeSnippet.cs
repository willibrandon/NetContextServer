namespace NetContextServer.Models;

/// <summary>
/// Represents a snippet of code from a source file with its associated metadata and embedding.
/// </summary>
public class CodeSnippet
{
    /// <summary>
    /// Gets the full file path of the source code file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the actual content/text of the code snippet.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the starting line number of the snippet in the source file (1-based).
    /// </summary>
    public required int StartLine { get; init; }

    /// <summary>
    /// Gets the ending line number of the snippet in the source file (1-based).
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    /// Gets the vector embedding representation of the code snippet used for semantic search.
    /// </summary>
    public required ReadOnlyMemory<float> Embedding { get; init; }
}
