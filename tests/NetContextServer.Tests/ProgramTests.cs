using NetContextServer.Tool;
using System.Reflection;

namespace NetContextServer.Tests;

public class ProgramTests
{
    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ParseArgs_WithSolutionPath_ReturnsSolutionPath()
    {
        // Arrange
        var args = new[] { "--solution", "C:\\Path\\To\\Solution" };

        // Act
        var solutionPath = InvokeParseArgs(args);

        // Assert
        Assert.Equal("C:\\Path\\To\\Solution", solutionPath);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ParseArgs_WithShortSolutionPath_ReturnsSolutionPath()
    {
        // Arrange
        var args = new[] { "-s", "C:\\Path\\To\\Solution" };

        // Act
        var solutionPath = InvokeParseArgs(args);

        // Assert
        Assert.Equal("C:\\Path\\To\\Solution", solutionPath);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ParseArgs_WithNoArgs_ThrowsArgumentException()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => InvokeParseArgs(args));
        Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Equal("No arguments provided.", exception.InnerException?.Message);
    }

    [Fact]
    [Trait("Category", "AI_Generated")]
    public void ParseArgs_WithMissingSolutionValue_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--solution" };

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => InvokeParseArgs(args));
        Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Equal("Missing value for --solution parameter.", exception.InnerException?.Message);
    }

    private static string InvokeParseArgs(string[] args)
    {
        // Use reflection to invoke the private ParseArgs method
        var method = typeof(Program).GetMethod("ParseArgs", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        if (method == null)
        {
            throw new InvalidOperationException("ParseArgs method not found");
        }

        return (string)method.Invoke(null, new object[] { args })!;
    }
} 