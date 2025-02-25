using NetContextServer.Core.Indexer;
using System;
using System.IO;
using System.Linq;

namespace NetContextServer.Tool;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("NetContextServer - .NET Codebase Context MCP Server");
        Console.WriteLine("----------------------------------------------------");

        try
        {
            // Get the solution directory (current directory for demo purposes)
            var solutionRoot = Directory.GetCurrentDirectory();
            Console.WriteLine($"Scanning solution at: {solutionRoot}");

            // Build the project index
            var index = ProjectIndexer.BuildIndex(solutionRoot);
            
            // Display solution information
            Console.WriteLine($"\nSolution file: {Path.GetFileName(index.SolutionPath ?? "Not found")}");
            
            // Display projects
            Console.WriteLine($"\nFound {index.ProjectPaths.Count} projects:");
            foreach (var projectPath in index.ProjectPaths)
            {
                Console.WriteLine($"  - {Path.GetFileName(projectPath)}");
                
                // Display source files for this project
                var sourceFiles = index.FilesByProject[projectPath];
                Console.WriteLine($"    Contains {sourceFiles.Count} source files");
                
                // Display up to 5 source files as examples
                foreach (var sourceFile in sourceFiles.Take(5))
                {
                    Console.WriteLine($"      * {ProjectIndexer.GetRelativePath(index, sourceFile)}");
                }
                
                if (sourceFiles.Count > 5)
                {
                    Console.WriteLine($"      * ... and {sourceFiles.Count - 5} more");
                }
                
                Console.WriteLine();
            }
            
            // Display total source files
            var allSourceFiles = ProjectIndexer.GetAllSourceFiles(index);
            Console.WriteLine($"Total source files: {allSourceFiles.Count}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }
}
