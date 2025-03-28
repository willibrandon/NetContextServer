namespace NetContextServer.Models;

/// <summary>
/// Represents a summary of code coverage across multiple files.
/// </summary>
public class CoverageSummary
{
    /// <summary>
    /// Gets or sets the overall coverage percentage across all files.
    /// </summary>
    public float TotalCoveragePercentage { get; set; }

    /// <summary>
    /// Gets or sets the total number of files analyzed.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files with coverage below a warning threshold.
    /// </summary>
    public int FilesWithLowCoverage { get; set; }

    /// <summary>
    /// Gets or sets the total number of uncovered lines across all files.
    /// </summary>
    public int TotalUncoveredLines { get; set; }

    /// <summary>
    /// Gets or sets a list of files with the lowest coverage percentages.
    /// </summary>
    public List<CoverageReport> LowestCoverageFiles { get; set; } = [];
} 