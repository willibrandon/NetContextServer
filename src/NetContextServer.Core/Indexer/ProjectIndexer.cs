using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetContextServer.Core.Models;

namespace NetContextServer.Core.Indexer;

/// <summary>
/// Provides functionality to scan and index .NET projects and their source files.
/// </summary>
public static class ProjectIndexer
{
    /// <summary>
    /// Builds an index of projects and source files from a solution directory.
    /// </summary>
    /// <param name="solutionRoot">The root directory containing the solution.</param>
    /// <returns>A <see cref="ProjectIndex"/> containing information about projects and their files.</returns>
    public static ProjectIndex BuildIndex(string solutionRoot)
    {
        if (string.IsNullOrWhiteSpace(solutionRoot))
            throw new ArgumentException("Solution root directory cannot be null or empty.", nameof(solutionRoot));

        if (!Directory.Exists(solutionRoot))
            throw new DirectoryNotFoundException($"Solution root directory not found: {solutionRoot}");

        var index = new ProjectIndex
        {
            SolutionRoot = solutionRoot
        };

        // Find the solution file
        var solutionFiles = Directory.EnumerateFiles(solutionRoot, "*.sln", SearchOption.TopDirectoryOnly).ToList();
        if (solutionFiles.Count > 0)
        {
            index.SolutionPath = solutionFiles.First();
        }

        // Find all project files
        var projectFiles = Directory.EnumerateFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories).ToList();
        index.ProjectPaths.AddRange(projectFiles);

        // For each project, find all .cs files
        foreach (var projectFile in projectFiles)
        {
            var projectDirectory = Path.GetDirectoryName(projectFile) ?? string.Empty;
            var sourceFiles = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories).ToList();
            index.FilesByProject[projectFile] = sourceFiles;
        }

        return index;
    }

    /// <summary>
    /// Gets a list of all source files across all projects.
    /// </summary>
    /// <param name="index">The project index.</param>
    /// <returns>A list of all source file paths.</returns>
    public static List<string> GetAllSourceFiles(ProjectIndex index)
    {
        return index.FilesByProject.Values.SelectMany(files => files).ToList();
    }

    /// <summary>
    /// Gets the relative path of a file from the solution root.
    /// </summary>
    /// <param name="index">The project index.</param>
    /// <param name="filePath">The absolute file path.</param>
    /// <returns>The relative path from the solution root.</returns>
    public static string GetRelativePath(ProjectIndex index, string filePath)
    {
        if (string.IsNullOrEmpty(index.SolutionRoot))
            return filePath;

        if (filePath.StartsWith(index.SolutionRoot, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = filePath.Substring(index.SolutionRoot.Length);
            return relativePath.TrimStart(Path.DirectorySeparatorChar);
        }

        return filePath;
    }
} 