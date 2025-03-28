namespace NetContextClient.Models;

/// <summary>
/// Represents a code coverage report for a single file or class.
/// </summary>
public class CoverageReport
{
    /// <summary>
    /// Gets or sets the path to the source file, relative to the project root.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overall coverage percentage for the file (0-100).
    /// </summary>
    public float CoveragePercentage { get; set; }

    /// <summary>
    /// Gets or sets the list of line numbers that are not covered by tests.
    /// </summary>
    public List<int> UncoveredLines { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of executable lines in the file.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Gets or sets the branch coverage information, mapping method names to their coverage percentage.
    /// </summary>
    public Dictionary<string, float> BranchCoverage { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of test files that provide coverage for this file.
    /// </summary>
    public List<string> TestFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets a suggested action to improve coverage, if applicable.
    /// </summary>
    public string? Recommendation { get; set; }

    /// <summary>
    /// The type of file being analyzed (Production, Test, Generated, or Unknown)
    /// </summary>
    public CoverageFileType FileType { get; set; } = CoverageFileType.Unknown;
}

/// <summary>
/// Represents the type of file being analyzed for code coverage
/// </summary>
public enum CoverageFileType
{
    /// <summary>
    /// Production code file
    /// </summary>
    Production,

    /// <summary>
    /// Test code file
    /// </summary>
    Test,

    /// <summary>
    /// Generated code file
    /// </summary>
    Generated,

    /// <summary>
    /// Unknown file type
    /// </summary>
    Unknown
}
