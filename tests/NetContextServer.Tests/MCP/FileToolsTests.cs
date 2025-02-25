using System;
using System.Collections.Generic;
using System.IO;
using NetContextServer.Core.MCP;
using NetContextServer.Core.Models;
using Xunit;

namespace NetContextServer.Tests.MCP;

[Trait("Category", "AI_Generated")]
public class FileToolsTests
{
    [Fact]
    public void SearchCode_WithValidKeyword_ReturnsResults()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a test file with content
            var filePath = Path.Combine(tempDir, "TestFile.cs");
            File.WriteAllLines(filePath, new[]
            {
                "using System;",
                "namespace Test {",
                "    public class TestClass {",
                "        public void TestMethod() {",
                "            Console.WriteLine(\"Hello, World!\");",
                "        }",
                "    }",
                "}"
            });

            var index = new ProjectIndex
            {
                SolutionRoot = tempDir,
                FilesByProject = new Dictionary<string, List<string>>
                {
                    { "Project1.csproj", new List<string> { filePath } }
                }
            };
            
            FileTools.Initialize(index);

            // Act
            var results = FileTools.SearchCode("TestMethod");

            // Assert
            Assert.Single(results);
            Assert.Equal(filePath, results[0].FilePath);
            Assert.Equal(4, results[0].LineNumber);
            Assert.Contains("TestMethod", results[0].LineText);
        }
        finally
        {
            // Clean up
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SearchCode_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a test file with content
            var filePath = Path.Combine(tempDir, "TestFile.cs");
            File.WriteAllLines(filePath, new[]
            {
                "using System;",
                "namespace Test {",
                "    public class TestClass {",
                "        public void TestMethod() {",
                "            Console.WriteLine(\"Hello, World!\");",
                "        }",
                "    }",
                "}"
            });

            var index = new ProjectIndex
            {
                SolutionRoot = tempDir,
                FilesByProject = new Dictionary<string, List<string>>
                {
                    { "Project1.csproj", new List<string> { filePath } }
                }
            };
            
            FileTools.Initialize(index);

            // Act
            var results = FileTools.SearchCode("NonExistentKeyword");

            // Assert
            Assert.Empty(results);
        }
        finally
        {
            // Clean up
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SearchCode_WithEmptyKeyword_ThrowsArgumentException()
    {
        // Arrange
        var index = new ProjectIndex();
        FileTools.Initialize(index);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => FileTools.SearchCode(""));
    }

    [Fact]
    public void OpenFile_WithValidFile_ReturnsContent()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a test file with content
            var filePath = Path.Combine(tempDir, "TestFile.cs");
            var fileContent = "using System;\npublic class Test {}";
            File.WriteAllText(filePath, fileContent);

            var index = new ProjectIndex
            {
                SolutionRoot = tempDir
            };
            
            FileTools.Initialize(index);

            // Act
            var result = FileTools.OpenFile(filePath);

            // Assert
            Assert.Equal(fileContent, result);
        }
        finally
        {
            // Clean up
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void OpenFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var index = new ProjectIndex
        {
            SolutionRoot = Path.GetTempPath()
        };
        FileTools.Initialize(index);
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "NonExistentFile.cs");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => FileTools.OpenFile(nonExistentFile));
    }
} 