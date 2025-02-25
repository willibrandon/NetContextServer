using NetContextServer.Core.Indexer;

namespace NetContextServer.Tests.Indexer;

public class ProjectIndexerTests
{
    [Fact]
    [Trait("Category", "AI_Generated")]
    public void BuildIndex_WithValidSolutionRoot_ReturnsProjectsAndFiles()
    {
        // Arrange
        var solutionRoot = GetSolutionRootPath();

        // Act
        var index = ProjectIndexer.BuildIndex(solutionRoot);

        // Assert
        Assert.NotNull(index);
        Assert.Equal(solutionRoot, index.SolutionRoot);
        Assert.NotNull(index.SolutionPath);
        Assert.NotEmpty(index.ProjectPaths);
        Assert.True(index.FilesByProject.Count > 0);
        
        // Verify that each project has at least one source file
        foreach (var projectFiles in index.FilesByProject.Values)
        {
            Assert.NotEmpty(projectFiles);
        }
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void BuildIndex_WithNullSolutionRoot_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ProjectIndexer.BuildIndex(null!));
        Assert.Contains("Solution root directory cannot be null or empty", exception.Message);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void BuildIndex_WithNonExistentSolutionRoot_ThrowsDirectoryNotFoundException()
    {
        // Act & Assert
        var exception = Assert.Throws<DirectoryNotFoundException>(() => 
            ProjectIndexer.BuildIndex(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
        Assert.Contains("Solution root directory not found", exception.Message);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void GetAllSourceFiles_ReturnsAllSourceFiles()
    {
        // Arrange
        var solutionRoot = GetSolutionRootPath();
        var index = ProjectIndexer.BuildIndex(solutionRoot);

        // Act
        var allSourceFiles = ProjectIndexer.GetAllSourceFiles(index);

        // Assert
        Assert.NotEmpty(allSourceFiles);
        Assert.True(allSourceFiles.Count >= index.FilesByProject.Count);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void GetRelativePath_ReturnsCorrectRelativePath()
    {
        // Arrange
        var solutionRoot = GetSolutionRootPath();
        var index = ProjectIndexer.BuildIndex(solutionRoot);
        var filePath = Path.Combine(solutionRoot, "src", "NetContextServer.Core", "Class1.cs");

        // Act
        var relativePath = ProjectIndexer.GetRelativePath(index, filePath);

        // Assert
        Assert.Equal(Path.Combine("src", "NetContextServer.Core", "Class1.cs"), relativePath);
    }

    private string GetSolutionRootPath()
    {
        // Navigate up from the test assembly location to find the solution root
        var currentDirectory = Directory.GetCurrentDirectory();
        
        // Keep going up until we find the .sln file or hit the root
        while (!string.IsNullOrEmpty(currentDirectory))
        {
            if (Directory.GetFiles(currentDirectory, "*.sln").Length > 0)
            {
                return currentDirectory;
            }

            var parent = Directory.GetParent(currentDirectory);
            if (parent == null)
            {
                break;
            }

            currentDirectory = parent.FullName;
        }

        // If we can't find the solution, use the current directory as a fallback
        return Directory.GetCurrentDirectory();
    }
} 