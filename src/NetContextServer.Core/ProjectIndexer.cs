using System.IO;

namespace NetContextServer.Core;

/// <summary>
/// Provides functionality to scan and index .NET solutions and projects.
/// </summary>
public static class ProjectIndexer
{
    /// <summary>
    /// Builds an index of projects and files from a .NET solution.
    /// </summary>
    /// <param name="solutionRoot">The root directory of the solution to index.</param>
    /// <returns>A <see cref="ProjectIndex"/> containing information about the projects and files.</returns>
    public static ProjectIndex BuildIndex(string solutionRoot)
    {
        if (!Directory.Exists(solutionRoot))
        {
            throw new DirectoryNotFoundException($"Solution directory not found: {solutionRoot}");
        }

        var index = new ProjectIndex(solutionRoot);

        // Find all .csproj files in the solution directory
        var projectFiles = Directory.EnumerateFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories);
        
        foreach (var projectFile in projectFiles)
        {
            index.ProjectPaths.Add(projectFile);
            
            // Get the directory containing the project file
            var projectDir = Path.GetDirectoryName(projectFile) ?? string.Empty;
            
            // Find all .cs files in the project directory
            var sourceFiles = Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories).ToList();
            
            // Add the source files to the index
            index.FilesByProject[projectFile] = sourceFiles;
        }

        return index;
    }
} 