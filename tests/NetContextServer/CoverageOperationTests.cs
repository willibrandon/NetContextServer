using ModelContextProtocol.Client;
using NetContextServer.Models;
using System.Text.Json;

namespace NetContextServer.Tests;

[Collection("NetContextServer Collection")]
public class CoverageOperationTests : IAsyncLifetime
{
    private readonly string _testDir;
    private readonly string _testCoverageFile;
    private readonly IMcpClient _client;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CoverageOperationTests(NetContextServerFixture fixture)
    {
        _client = fixture.Client;
        _testDir = Path.Combine(Path.GetTempPath(), "NetContextServerTests_" + Guid.NewGuid());
        _testCoverageFile = Path.Combine(_testDir, "coverage.json");
    }

    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(_testDir);
            // Create a sample coverage file
            File.WriteAllText(_testCoverageFile, @"{
                ""Modules"": {
                    ""TestModule"": {
                        ""Classes"": {
                            ""TestNamespace.TestClass"": {
                                ""Lines"": {
                                    ""10"": 1,
                                    ""11"": 0,
                                    ""12"": 1
                                }
                            }
                        }
                    }
                }
            }");
        });
    }

    public async Task DisposeAsync()
    {
        try
        {
            // Reset the base directory
            await _client.CallToolAsync("set_base_directory", 
                new Dictionary<string, object?> { ["directory"] = Directory.GetCurrentDirectory() });
        }
        catch
        {
            // Ignore errors when resetting base directory
        }

        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task CoverageAnalysis_WithValidFile_ReturnsCoverageData()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("coverage_analysis", 
            new Dictionary<string, object?> { 
                ["reportPath"] = _testCoverageFile,
                ["coverageFormat"] = "coverlet"
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var reports = JsonSerializer.Deserialize<List<CoverageReport>>(content.Text, DefaultJsonOptions);
        Assert.NotNull(reports);
        Assert.NotEmpty(reports);
        
        var report = reports[0];
        Assert.Equal("TestNamespace.TestClass", report.FilePath);
        Assert.Equal(66.67f, report.CoveragePercentage, 2); // 2/3 lines covered
        Assert.Contains(11, report.UncoveredLines);
    }

    [Fact]
    public async Task CoverageAnalysis_WithInvalidPath_ReturnsError()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });
        var invalidPath = Path.Combine(_testDir, "nonexistent.json");

        // Act
        var result = await _client.CallToolAsync("coverage_analysis", 
            new Dictionary<string, object?> { 
                ["reportPath"] = invalidPath
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonDocument.Parse(content.Text);
        Assert.True(response.RootElement.TryGetProperty("error", out var errorElement));
        Assert.Contains("not found", errorElement.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CoverageAnalysis_OutsideBaseDirectory_ReturnsError()
    {
        // Arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "OutsideDir_" + Guid.NewGuid());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "coverage.json");
        File.WriteAllText(outsideFile, "{}");

        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });

        try
        {
            // Act
            var result = await _client.CallToolAsync("coverage_analysis", 
                new Dictionary<string, object?> { 
                    ["reportPath"] = outsideFile
                });

            // Assert
            Assert.NotNull(result);
            var content = result.Content.FirstOrDefault(c => c.Type == "text");
            Assert.NotNull(content);
            Assert.NotNull(content.Text);

            var response = JsonDocument.Parse(content.Text);
            Assert.True(response.RootElement.TryGetProperty("error", out var errorElement));
            Assert.Contains("outside", errorElement.GetString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(outsideDir))
                {
                    Directory.Delete(outsideDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task CoverageSummary_WithValidFile_ReturnsSummaryData()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("coverage_summary", 
            new Dictionary<string, object?> { 
                ["reportPath"] = _testCoverageFile,
                ["coverageFormat"] = "coverlet"
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var summary = JsonSerializer.Deserialize<CoverageSummary>(content.Text, DefaultJsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalFiles);
        Assert.Equal(66.67f, summary.TotalCoveragePercentage, 2);
        Assert.Equal(1, summary.FilesWithLowCoverage);
        Assert.Equal(1, summary.TotalUncoveredLines);
    }

    [Fact]
    public async Task CoverageAnalysis_WithLcovFormat_ReturnsCoverageData()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });
        
        var lcovContent = @"SF:src/MyProject/Service.cs
DA:1,1
DA:2,0
DA:3,1
end_of_record";
        var lcovFile = Path.Combine(_testDir, "coverage.lcov");
        await File.WriteAllTextAsync(lcovFile, lcovContent);

        // Act
        var result = await _client.CallToolAsync("coverage_analysis", 
            new Dictionary<string, object?> { 
                ["reportPath"] = lcovFile,
                ["coverageFormat"] = "lcov"
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var reports = JsonSerializer.Deserialize<List<CoverageReport>>(content.Text, DefaultJsonOptions);
        Assert.NotNull(reports);
        Assert.NotEmpty(reports);
        
        var report = reports[0];
        Assert.Equal("src/MyProject/Service.cs", report.FilePath);
        Assert.Equal(66.67f, report.CoveragePercentage, 2); // 2/3 lines covered
        Assert.Contains(2, report.UncoveredLines);
    }

    [Fact]
    public async Task CoverageAnalysis_WithCoberturaFormat_ReturnsCoverageData()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });
        
        var coberturaContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<coverage line-rate=""0.6667"">
  <packages>
    <package>
      <classes>
        <class filename=""src/MyProject/Service.cs"" line-rate=""0.6667"">
          <lines>
            <line number=""1"" hits=""1""/>
            <line number=""2"" hits=""0""/>
            <line number=""3"" hits=""1""/>
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>";
        var coberturaFile = Path.Combine(_testDir, "coverage.xml");
        await File.WriteAllTextAsync(coberturaFile, coberturaContent);

        // Act
        var result = await _client.CallToolAsync("coverage_analysis", 
            new Dictionary<string, object?> { 
                ["reportPath"] = coberturaFile,
                ["coverageFormat"] = "cobertura"
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var reports = JsonSerializer.Deserialize<List<CoverageReport>>(content.Text, DefaultJsonOptions);
        Assert.NotNull(reports);
        Assert.NotEmpty(reports);
        
        var report = reports[0];
        Assert.Equal("src/MyProject/Service.cs", report.FilePath);
        Assert.Equal(66.67f, report.CoveragePercentage, 2);
        Assert.Contains(2, report.UncoveredLines);
    }

    [Fact]
    public async Task CoverageAnalysis_WithBranchCoverage_ReturnsBranchData()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });
        
        var coverageContent = @"{
            ""Modules"": {
                ""TestModule"": {
                    ""Classes"": {
                        ""TestNamespace.TestClass"": {
                            ""Lines"": {
                                ""10"": 1,
                                ""11"": 0,
                                ""12"": 1
                            },
                            ""Branches"": {
                                ""10"": {
                                    ""0"": 1,
                                    ""1"": 0
                                }
                            },
                            ""Methods"": {
                                ""ProcessData"": {
                                    ""CoveredBranches"": 1,
                                    ""TotalBranches"": 2
                                }
                            }
                        }
                    }
                }
            }
        }";
        await File.WriteAllTextAsync(_testCoverageFile, coverageContent);

        // Act
        var result = await _client.CallToolAsync("coverage_analysis", 
            new Dictionary<string, object?> { 
                ["reportPath"] = _testCoverageFile,
                ["coverageFormat"] = "coverlet"
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var reports = JsonSerializer.Deserialize<List<CoverageReport>>(content.Text, DefaultJsonOptions);
        Assert.NotNull(reports);
        Assert.NotEmpty(reports);
        
        var report = reports[0];
        Assert.Equal("TestNamespace.TestClass", report.FilePath);
        Assert.Equal(66.67f, report.CoveragePercentage, 2);
        Assert.Contains(11, report.UncoveredLines);
        Assert.Contains("ProcessData", report.BranchCoverage.Keys);
        Assert.Equal(50.0f, report.BranchCoverage["ProcessData"], 1);
    }

    [Fact]
    public async Task CoverageSummary_WithMultipleFiles_AggregatesCorrectly()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });
        
        var coverageContent = @"{
            ""Modules"": {
                ""TestModule"": {
                    ""Classes"": {
                        ""TestNamespace.Class1"": {
                            ""Lines"": {
                                ""10"": 1,
                                ""11"": 0
                            }
                        },
                        ""TestNamespace.Class2"": {
                            ""Lines"": {
                                ""15"": 1,
                                ""16"": 1,
                                ""17"": 0
                            }
                        }
                    }
                }
            }
        }";
        await File.WriteAllTextAsync(_testCoverageFile, coverageContent);

        // Act
        var result = await _client.CallToolAsync("coverage_summary", 
            new Dictionary<string, object?> { 
                ["reportPath"] = _testCoverageFile,
                ["coverageFormat"] = "coverlet"
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var summary = JsonSerializer.Deserialize<CoverageSummary>(content.Text, DefaultJsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalFiles);
        Assert.Equal(60.0f, summary.TotalCoveragePercentage, 1); // 3/5 lines covered
        Assert.Equal(2, summary.FilesWithLowCoverage); // Both files < 70%
        Assert.Equal(2, summary.TotalUncoveredLines);
        Assert.Equal(2, summary.LowestCoverageFiles.Count);
    }

    [Fact]
    public async Task CoverageAnalysis_WithEmptyCoverageFile_ReturnsEmptyResults()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });
        
        await File.WriteAllTextAsync(_testCoverageFile, "{}");

        // Act
        var result = await _client.CallToolAsync("coverage_analysis", 
            new Dictionary<string, object?> { 
                ["reportPath"] = _testCoverageFile
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var reports = JsonSerializer.Deserialize<List<CoverageReport>>(content.Text, DefaultJsonOptions);
        Assert.NotNull(reports);
        Assert.Empty(reports);
    }

    [Fact]
    public async Task CoverageAnalysis_WithInvalidFormat_ReturnsError()
    {
        // Arrange
        await _client.CallToolAsync("set_base_directory", 
            new Dictionary<string, object?> { ["directory"] = _testDir });

        // Act
        var result = await _client.CallToolAsync("coverage_analysis", 
            new Dictionary<string, object?> { 
                ["reportPath"] = _testCoverageFile,
                ["coverageFormat"] = "invalid"
            });

        // Assert
        Assert.NotNull(result);
        var content = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(content);
        Assert.NotNull(content.Text);

        var response = JsonDocument.Parse(content.Text);
        Assert.True(response.RootElement.TryGetProperty("error", out var errorElement));
        Assert.Contains("format", errorElement.GetString(), StringComparison.OrdinalIgnoreCase);
    }
} 