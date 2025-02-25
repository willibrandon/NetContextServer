using System;
using System.Collections.Generic;
using System.IO;
using NetContextServer.Core.MCP;
using NetContextServer.Core.Models;
using Xunit;

namespace NetContextServer.Tests.MCP;

[Trait("Category", "AI_Generated")]
public class SymbolToolsTests
{
    [Fact]
    public void GetDocForSymbol_WithValidSymbol_ReturnsDocumentation()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a test file with a documented class
            var filePath = Path.Combine(tempDir, "TestClass.cs");
            File.WriteAllLines(filePath, new[]
            {
                "using System;",
                "",
                "namespace Test",
                "{",
                "    /// <summary>",
                "    /// This is a test class.",
                "    /// </summary>",
                "    public class TestClass",
                "    {",
                "        /// <summary>",
                "        /// This is a test method.",
                "        /// </summary>",
                "        public void TestMethod()",
                "        {",
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
            
            SymbolTools.Initialize(index);

            // Act
            var result = SymbolTools.GetDocForSymbol("TestClass");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestClass", result!.SymbolName);
            Assert.Equal(filePath, result.FilePath);
            Assert.Equal(8, result.LineNumber); // 1-based line number
            Assert.Contains("This is a test class.", result.Documentation);
            Assert.Equal("type", result.SymbolType);
        }
        finally
        {
            // Clean up
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetDocForSymbol_WithValidMethod_ReturnsDocumentation()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a test file with a documented method
            var filePath = Path.Combine(tempDir, "TestClass.cs");
            File.WriteAllLines(filePath, new[]
            {
                "using System;",
                "",
                "namespace Test",
                "{",
                "    public class TestClass",
                "    {",
                "        /// <summary>",
                "        /// This is a test method.",
                "        /// </summary>",
                "        public void TestMethod()",
                "        {",
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
            
            SymbolTools.Initialize(index);

            // Act
            var result = SymbolTools.GetDocForSymbol("TestMethod");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestMethod", result!.SymbolName);
            Assert.Equal(filePath, result.FilePath);
            Assert.Equal(10, result.LineNumber); // 1-based line number
            Assert.Contains("This is a test method.", result.Documentation);
            Assert.Equal("method", result.SymbolType);
        }
        finally
        {
            // Clean up
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetDocForSymbol_WithNonExistentSymbol_ReturnsNull()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a test file
            var filePath = Path.Combine(tempDir, "TestClass.cs");
            File.WriteAllLines(filePath, new[]
            {
                "using System;",
                "",
                "namespace Test",
                "{",
                "    public class TestClass",
                "    {",
                "        public void TestMethod()",
                "        {",
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
            
            SymbolTools.Initialize(index);

            // Act
            var result = SymbolTools.GetDocForSymbol("NonExistentSymbol");

            // Assert
            Assert.Null(result);
        }
        finally
        {
            // Clean up
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetDocForSymbol_WithEmptySymbolName_ThrowsArgumentException()
    {
        // Arrange
        var index = new ProjectIndex();
        SymbolTools.Initialize(index);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SymbolTools.GetDocForSymbol(""));
    }

    [Fact]
    public void ListSymbols_ReturnsSymbolNames()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a test file with multiple symbols
            var filePath = Path.Combine(tempDir, "TestClass.cs");
            File.WriteAllLines(filePath, new[]
            {
                "using System;",
                "",
                "namespace Test",
                "{",
                "    public class TestClass",
                "    {",
                "        public void TestMethod1()",
                "        {",
                "            Console.WriteLine(\"Hello, World!\");",
                "        }",
                "",
                "        public void TestMethod2()",
                "        {",
                "            Console.WriteLine(\"Hello again!\");",
                "        }",
                "    }",
                "",
                "    public interface ITestInterface",
                "    {",
                "        void InterfaceMethod();",
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
            
            SymbolTools.Initialize(index);

            // Act
            var symbols = SymbolTools.ListSymbols();

            // Assert
            Assert.NotEmpty(symbols);
            Assert.Contains("TestClass", symbols);
            Assert.Contains("TestMethod1", symbols);
            Assert.Contains("TestMethod2", symbols);
            Assert.Contains("ITestInterface", symbols);
            Assert.Contains("InterfaceMethod", symbols);
        }
        finally
        {
            // Clean up
            Directory.Delete(tempDir, true);
        }
    }
} 