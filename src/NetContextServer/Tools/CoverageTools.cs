using ModelContextProtocol.Server;
using NetContextServer.Models;
using NetContextServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NetContextServer.Tools;

/// <summary>
/// Provides MCP tools for analyzing code coverage reports and providing coverage insights.
/// </summary>
[McpServerToolType]
public static class CoverageTools
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Analyzes a coverage report file and returns detailed coverage information.
    /// </summary>
    /// <remarks>
    /// This tool parses coverage data from common formats (Coverlet JSON, LCOV, Cobertura XML)
    /// and returns a structured analysis of code coverage, including:
    /// - Coverage percentage per file
    /// - Uncovered lines
    /// - Branch coverage (where available)
    /// - Recommendations for improving coverage
    /// </remarks>
    [McpServerTool("coverage_analysis")]
    [Description("Analyzes a coverage report file and returns detailed coverage information.")]
    public static async Task<string> AnalyzeCoverage(
        [Description("Path to the coverage report file. Must be within the base directory.")]
        string reportPath,
        
        [Description("Format of the coverage file: 'coverlet' (default), 'lcov', or 'cobertura'.")]
        string? coverageFormat = null)
    {
        try
        {
            FileValidationService.EnsureBaseDirectorySet();
            
            var format = coverageFormat?.ToLowerInvariant() switch
            {
                "lcov" => CoverageFormat.Lcov,
                "cobertura" => CoverageFormat.CoberturaXml,
                null or "coverlet" => CoverageFormat.CoverletJson,
                _ => throw new ArgumentException($"Unsupported coverage format: {coverageFormat}")
            };
            
            var service = new CoverageAnalysisService(FileValidationService.BaseDirectory);
            var reports = await service.AnalyzeCoverageAsync(reportPath, format);

            return JsonSerializer.Serialize(reports, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Generates a high-level summary of code coverage across all files.
    /// </summary>
    /// <remarks>
    /// This tool provides an overview of code coverage, including:
    /// - Total coverage percentage
    /// - Number of files with low coverage
    /// - Total uncovered lines
    /// - List of files with lowest coverage
    /// </remarks>
    [McpServerTool("coverage_summary")]
    [Description("Returns a high-level summary of code coverage statistics.")]
    public static async Task<string> CoverageSummary(
        [Description("Path to the coverage report file. Must be within the base directory.")]
        string reportPath,
        
        [Description("Format of the coverage file: 'coverlet' (default), 'lcov', or 'cobertura'.")]
        string? coverageFormat = null)
    {
        var format = coverageFormat?.ToLowerInvariant() switch
        {
            "lcov" => CoverageFormat.Lcov,
            "cobertura" => CoverageFormat.CoberturaXml,
            _ => CoverageFormat.CoverletJson
        };

        try
        {
            FileValidationService.EnsureBaseDirectorySet();
            
            var service = new CoverageAnalysisService(FileValidationService.BaseDirectory);
            var reports = await service.AnalyzeCoverageAsync(reportPath, format);
            var summary = service.GenerateSummary(reports);

            return JsonSerializer.Serialize(summary, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }
} 