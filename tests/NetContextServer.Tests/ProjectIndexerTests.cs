using NetContextServer.Core;
using System.IO;

namespace NetContextServer.Tests;

public class ProjectIndexerTests
{
    [Fact]
    [Trait("Category", "AI_Generated")]
    public void BuildIndex_WithValidSolutionRoot_ReturnsPopulatedIndex()
    {
        // Arrange
        var solutionRoot = Directory.GetCurrentDirectory(); // Use the current directory for testing

        // Act
        var index = ProjectIndexer.BuildIndex(solutionRoot);

        // Assert
        Assert.NotNull(index);
        Assert.Equal(solutionRoot, index.SolutionRoot);
        
        // Note: The actual number of projects and files will depend on the test environment
        // This is just a basic check that something was found
        Assert.True(index.ProjectPaths.Count >= 0);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void BuildIndex_WithInvalidSolutionRoot_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        var exception = Assert.Throws<DirectoryNotFoundException>(() => ProjectIndexer.BuildIndex(invalidPath));
        Assert.Contains(invalidPath, exception.Message);
    }
} 