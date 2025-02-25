using System;
using System.Collections.Generic;
using System.IO;
using NetContextServer.Core.MCP;
using NetContextServer.Core.Models;
using Xunit;

namespace NetContextServer.Tests.MCP;

[Trait("Category", "AI_Generated")]
public class ProjectToolsTests
{
    [Fact]
    public void ListProjects_ReturnsProjectPaths()
    {
        // Arrange
        var index = new ProjectIndex
        {
            ProjectPaths = new List<string> { "Project1.csproj", "Project2.csproj" }
        };
        ProjectTools.Initialize(index);

        // Act
        var result = ProjectTools.ListProjects();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Project1.csproj", result);
        Assert.Contains("Project2.csproj", result);
    }

    [Fact]
    public void ListFiles_WithValidProject_ReturnsFiles()
    {
        // Arrange
        var projectPath = "Project1.csproj";
        var files = new List<string> { "File1.cs", "File2.cs" };
        var index = new ProjectIndex
        {
            FilesByProject = new Dictionary<string, List<string>>
            {
                { projectPath, files }
            }
        };
        ProjectTools.Initialize(index);

        // Act
        var result = ProjectTools.ListFiles(projectPath);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("File1.cs", result);
        Assert.Contains("File2.cs", result);
    }

    [Fact]
    public void ListFiles_WithInvalidProject_ReturnsEmptyList()
    {
        // Arrange
        var index = new ProjectIndex
        {
            FilesByProject = new Dictionary<string, List<string>>
            {
                { "Project1.csproj", new List<string> { "File1.cs" } }
            }
        };
        ProjectTools.Initialize(index);

        // Act
        var result = ProjectTools.ListFiles("NonExistentProject.csproj");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetSolutionPath_ReturnsSolutionPath()
    {
        // Arrange
        var solutionPath = "Solution.sln";
        var index = new ProjectIndex
        {
            SolutionPath = solutionPath
        };
        ProjectTools.Initialize(index);

        // Act
        var result = ProjectTools.GetSolutionPath();

        // Assert
        Assert.Equal(solutionPath, result);
    }

    [Fact]
    public void GetSolutionRoot_ReturnsSolutionRoot()
    {
        // Arrange
        var solutionRoot = "C:\\Projects\\MySolution";
        var index = new ProjectIndex
        {
            SolutionRoot = solutionRoot
        };
        ProjectTools.Initialize(index);

        // Act
        var result = ProjectTools.GetSolutionRoot();

        // Assert
        Assert.Equal(solutionRoot, result);
    }
} 