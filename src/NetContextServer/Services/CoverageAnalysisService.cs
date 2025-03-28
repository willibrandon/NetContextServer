using NetContextServer.Models;
using System.Text.Json;
using System.Xml.Linq;

namespace NetContextServer.Services;

/// <summary>
/// Service for analyzing code coverage reports from various formats and providing structured coverage information.
/// </summary>
/// <remarks>
/// This service supports multiple coverage formats:
/// - Coverlet JSON (default)
/// - LCOV
/// - Cobertura XML
/// 
/// Coverage data is parsed and normalized into a consistent format for consumption by the MCP tools.
/// </remarks>
public class CoverageAnalysisService
{
    private const float LOW_COVERAGE_THRESHOLD = 70.0f;
    private readonly string _baseDirectory;

    /// <summary>
    /// Initializes a new instance of the CoverageAnalysisService class.
    /// </summary>
    /// <param name="baseDirectory">Optional base directory for resolving relative paths. If null, uses the current directory.</param>
    public CoverageAnalysisService(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Analyzes a coverage report file and returns detailed coverage information.
    /// </summary>
    /// <param name="coverageFilePath">Path to the coverage report file.</param>
    /// <param name="format">Format of the coverage report.</param>
    /// <returns>A list of coverage reports, one for each source file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the coverage file is outside the base directory.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the coverage file does not exist.</exception>
    public async Task<List<CoverageReport>> AnalyzeCoverageAsync(
        string coverageFilePath,
        CoverageFormat format)
    {
        // Validate coverage file path
        if (!FileValidationService.IsPathSafe(coverageFilePath))
        {
            throw new InvalidOperationException("Coverage file is outside the base directory or not allowed.");
        }
        if (!File.Exists(coverageFilePath))
        {
            throw new FileNotFoundException("Coverage file not found.", coverageFilePath);
        }

        return format switch
        {
            CoverageFormat.Lcov => await ParseLcovAsync(coverageFilePath),
            CoverageFormat.CoberturaXml => await ParseCoberturaXmlAsync(coverageFilePath),
            _ => await ParseCoverletJsonAsync(coverageFilePath),
        };
    }

    /// <summary>
    /// Generates a summary of coverage across all files.
    /// </summary>
    /// <param name="reports">List of individual file coverage reports.</param>
    /// <returns>A summary of overall coverage statistics.</returns>
    public CoverageSummary GenerateSummary(List<CoverageReport> reports)
    {
        if (reports.Count == 0)
        {
            return new CoverageSummary
            {
                TotalFiles = 0,
                TotalCoveragePercentage = 0,
                FilesWithLowCoverage = 0,
                TotalUncoveredLines = 0,
                LowestCoverageFiles = []
            };
        }

        var totalLines = reports.Sum(r => r.TotalLines);
        var totalUncoveredLines = reports.Sum(r => r.UncoveredLines.Count);
        var totalCoveredLines = totalLines - totalUncoveredLines;

        var summary = new CoverageSummary
        {
            TotalFiles = reports.Count,
            TotalCoveragePercentage = totalLines > 0 ? (float)totalCoveredLines / totalLines * 100 : 0,
            FilesWithLowCoverage = reports.Count(r => r.CoveragePercentage < LOW_COVERAGE_THRESHOLD),
            TotalUncoveredLines = totalUncoveredLines,
            LowestCoverageFiles = reports
                .OrderBy(r => r.CoveragePercentage)
                .Take(5)
                .ToList()
        };

        return summary;
    }

    private bool IsGeneratedCode(string filePath)
    {
        // Check for common generated code patterns
        return filePath.Contains("/obj/") ||        // Generated files in obj directory
               filePath.EndsWith(".g.cs") ||        // Standard generated code suffix
               filePath.EndsWith(".generated.cs");  // Alternative generated code suffix
    }

    private async Task<List<CoverageReport>> ParseCoverletJsonAsync(string filePath)
    {
        var text = await File.ReadAllTextAsync(filePath);
        using var doc = JsonDocument.Parse(text);

        var result = new List<CoverageReport>();
        var root = doc.RootElement;

        if (root.TryGetProperty("Modules", out var modulesEl))
        {
            foreach (var moduleProp in modulesEl.EnumerateObject())
            {
                if (moduleProp.Value.TryGetProperty("Classes", out var classesEl))
                {
                    foreach (var classProp in classesEl.EnumerateObject())
                    {
                        // Skip generated code files
                        if (IsGeneratedCode(classProp.Name))
                            continue;

                        var report = ExtractCoverageFromClass(classProp);
                        if (report != null)
                        {
                            result.Add(report);
                        }
                    }
                }
            }
        }

        return result;
    }

    private CoverageReport? ExtractCoverageFromClass(JsonProperty classProp)
    {
        if (!classProp.Value.TryGetProperty("Lines", out var linesEl))
        {
            return null;
        }

        var report = new CoverageReport
        {
            FilePath = NormalizePath(classProp.Name)
        };

        var totalLines = 0;
        var coveredLines = 0;
        var uncoveredLines = new List<int>();

        foreach (var lineProp in linesEl.EnumerateObject())
        {
            if (int.TryParse(lineProp.Name, out var lineNumber))
            {
                totalLines++;
                var hits = lineProp.Value.GetInt32();
                if (hits == 0)
                {
                    uncoveredLines.Add(lineNumber);
                }
                else
                {
                    coveredLines++;
                }
            }
        }

        report.UncoveredLines = uncoveredLines;
        report.TotalLines = totalLines;
        report.CoveragePercentage = totalLines > 0 
            ? (float)coveredLines / totalLines * 100 
            : 0;

        // Add branch coverage if available
        if (classProp.Value.TryGetProperty("Methods", out var methodsEl))
        {
            var branchCoverage = new Dictionary<string, float>();
            foreach (var methodProp in methodsEl.EnumerateObject())
            {
                if (methodProp.Value.TryGetProperty("CoveredBranches", out var coveredBranchesEl) &&
                    methodProp.Value.TryGetProperty("TotalBranches", out var totalBranchesEl))
                {
                    var coveredBranches = coveredBranchesEl.GetInt32();
                    var totalBranches = totalBranchesEl.GetInt32();

                    if (totalBranches > 0)
                    {
                        branchCoverage[methodProp.Name] = (float)coveredBranches / totalBranches * 100;
                    }
                }
            }
            report.BranchCoverage = branchCoverage;
        }

        // Add recommendations based on coverage
        if (report.CoveragePercentage < LOW_COVERAGE_THRESHOLD)
        {
            report.Recommendation = $"Consider adding tests to improve coverage (currently {report.CoveragePercentage:F1}%)";
            if (report.UncoveredLines.Count > 0)
            {
                report.Recommendation += $". Focus on lines: {string.Join(", ", report.UncoveredLines.Take(5))}";
                if (report.UncoveredLines.Count > 5)
                {
                    report.Recommendation += $" and {report.UncoveredLines.Count - 5} more";
                }
            }
        }

        return report;
    }

    private async Task<List<CoverageReport>> ParseLcovAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        var result = new List<CoverageReport>();
        CoverageReport? currentReport = null;
        int totalLines = 0;
        int coveredLines = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("SF:"))
            {
                // Skip generated code files
                var sourceFile = line[3..];
                if (IsGeneratedCode(sourceFile))
                {
                    currentReport = null;
                    continue;
                }

                // New file section
                if (currentReport != null)
                {
                    currentReport.TotalLines = totalLines;
                    currentReport.CoveragePercentage = totalLines > 0 
                        ? (float)coveredLines / totalLines * 100 
                        : 0;
                    result.Add(currentReport);
                }
                currentReport = new CoverageReport
                {
                    FilePath = NormalizePath(sourceFile),
                    UncoveredLines = []
                };
                totalLines = 0;
                coveredLines = 0;
            }
            else if (currentReport != null && line.StartsWith("DA:"))
            {
                // Line coverage data
                var parts = line[3..].Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var lineNum) &&
                    int.TryParse(parts[1], out var hits))
                {
                    totalLines++;
                    if (hits == 0)
                    {
                        currentReport.UncoveredLines.Add(lineNum);
                    }
                    else
                    {
                        coveredLines++;
                    }
                }
            }
            else if (line == "end_of_record" && currentReport != null)
            {
                currentReport.TotalLines = totalLines;
                currentReport.CoveragePercentage = totalLines > 0 
                    ? (float)coveredLines / totalLines * 100 
                    : 0;
                result.Add(currentReport);
                currentReport = null;
                totalLines = 0;
                coveredLines = 0;
            }
        }

        if (currentReport != null)
        {
            currentReport.TotalLines = totalLines;
            currentReport.CoveragePercentage = totalLines > 0 
                ? (float)coveredLines / totalLines * 100 
                : 0;
            result.Add(currentReport);
        }

        return result;
    }

    private async Task<List<CoverageReport>> ParseCoberturaXmlAsync(string filePath)
    {
        var doc = await Task.Run(() => XDocument.Load(filePath));
        var result = new List<CoverageReport>();

        var fileElements = doc.Descendants("class")
            .GroupBy(x => x.Attribute("filename")?.Value)
            .Where(g => g.Key != null && !IsGeneratedCode(g.Key));

        foreach (var fileGroup in fileElements)
        {
            var report = new CoverageReport
            {
                FilePath = NormalizePath(fileGroup.Key!),
                UncoveredLines = []
            };

            var allLines = new HashSet<int>();
            var coveredLines = new HashSet<int>();

            foreach (var classElement in fileGroup)
            {
                foreach (var lineElement in classElement.Descendants("line"))
                {
                    if (int.TryParse(lineElement.Attribute("number")?.Value, out var lineNum))
                    {
                        allLines.Add(lineNum);
                        if (int.TryParse(lineElement.Attribute("hits")?.Value, out var hits) && hits > 0)
                        {
                            coveredLines.Add(lineNum);
                        }
                        else
                        {
                            report.UncoveredLines.Add(lineNum);
                        }
                    }
                }
            }

            if (allLines.Count > 0)
            {
                report.CoveragePercentage = (float)coveredLines.Count / allLines.Count * 100;
            }

            result.Add(report);
        }

        return result;
    }

    private string NormalizePath(string path)
    {
        // Convert absolute paths to relative paths based on the base directory
        if (Path.IsPathRooted(path))
        {
            try
            {
                return Path.GetRelativePath(_baseDirectory, path);
            }
            catch
            {
                // If we can't get a relative path, return the original
                return path;
            }
        }
        return path;
    }
} 