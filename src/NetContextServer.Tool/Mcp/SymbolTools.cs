using MCPSharp;
using NetContextServer.Core.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace NetContextServer.Tool.Mcp;

/// <summary>
/// Provides MCP tools for working with code symbols and documentation.
/// </summary>
[McpTool("symbol_tools", "Tools for working with code symbols and documentation")]
public static class SymbolTools
{
    private static ProjectIndex? _index;
    private static readonly Regex SymbolRegex = new(@"(public|internal|private|protected)\s+(static\s+)?(class|interface|struct|enum|record|delegate|abstract\s+class)\s+(?<n>\w+)", RegexOptions.Compiled);
    private static readonly Regex MethodRegex = new(@"(public|internal|private|protected)?\s*(static\s+)?(async\s+)?([\w<>[\],\s]+)\s+(?<n>\w+)\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Initializes the SymbolTools with the specified project index.
    /// </summary>
    /// <param name="index">The project index containing information about projects and their files.</param>
    public static void Initialize(ProjectIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    /// <summary>
    /// Represents documentation for a symbol.
    /// </summary>
    public class SymbolDocumentation
    {
        /// <summary>
        /// Gets or sets the name of the symbol.
        /// </summary>
        public string SymbolName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path where the symbol is defined.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the line number where the symbol is defined (1-based).
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the documentation comments for the symbol.
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the symbol (class, method, etc.).
        /// </summary>
        public string SymbolType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gets documentation for a symbol.
    /// </summary>
    /// <param name="symbolName">The name of the symbol to find documentation for.</param>
    /// <returns>Documentation for the symbol, or null if the symbol is not found.</returns>
    [McpFunction("get_doc_for_symbol", "Gets documentation for a symbol")]
    public static SymbolDocumentation? GetDocForSymbol([McpParameter(true, "The name of the symbol to find documentation for")] string symbolName)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(symbolName))
            throw new ArgumentException("Symbol name cannot be empty", nameof(symbolName));

        var allSourceFiles = _index!.FilesByProject.Values.SelectMany(files => files).ToList();

        foreach (var file in allSourceFiles)
        {
            try
            {
                var lines = File.ReadAllLines(file);
                var docBuilder = new StringBuilder();
                var inDocComment = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();

                    // Check for XML doc comments
                    if (line.StartsWith("///"))
                    {
                        inDocComment = true;
                        docBuilder.AppendLine(line.TrimStart('/').Trim());
                        continue;
                    }

                    if (inDocComment)
                    {
                        // Check if this line contains the symbol definition
                        string symbolType = string.Empty;
                        if (IsSymbolDefinition(line, symbolName, out symbolType))
                        {
                            return new SymbolDocumentation
                            {
                                SymbolName = symbolName,
                                FilePath = file,
                                LineNumber = i + 1,
                                Documentation = docBuilder.ToString().Trim(),
                                SymbolType = symbolType
                            };
                        }

                        // If we hit a non-doc line without finding the symbol, reset and continue searching
                        inDocComment = false;
                        docBuilder.Clear();
                    }
                    else
                    {
                        // Check for symbol without doc comments
                        string symbolType = string.Empty;
                        if (IsSymbolDefinition(line, symbolName, out symbolType))
                        {
                            return new SymbolDocumentation
                            {
                                SymbolName = symbolName,
                                FilePath = file,
                                LineNumber = i + 1,
                                Documentation = string.Empty,
                                SymbolType = symbolType
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with other files
                Console.Error.WriteLine($"Error searching file {file}: {ex.Message}");
            }
        }

        // Symbol not found
        return null;
    }

    /// <summary>
    /// Lists all symbols in the solution.
    /// </summary>
    /// <returns>A list of symbol names found in the solution.</returns>
    [McpFunction("list_symbols", "Lists all symbols in the solution")]
    public static List<string> ListSymbols()
    {
        EnsureInitialized();
        
        var symbols = new HashSet<string>();
        var allSourceFiles = _index!.FilesByProject.Values.SelectMany(files => files).ToList();
        
        foreach (var file in allSourceFiles)
        {
            try
            {
                var lines = File.ReadAllLines(file);
                
                foreach (var line in lines)
                {
                    // Extract class/interface/struct/enum names
                    var match = SymbolRegex.Match(line);
                    if (match.Success)
                    {
                        symbols.Add(match.Groups["n"].Value);
                    }
                    
                    // Extract method names
                    match = MethodRegex.Match(line);
                    if (match.Success)
                    {
                        symbols.Add(match.Groups["n"].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with other files
                Console.Error.WriteLine($"Error processing file {file}: {ex.Message}");
            }
        }
        
        return symbols.OrderBy(s => s).ToList();
    }

    private static void EnsureInitialized()
    {
        if (_index == null)
        {
            throw new InvalidOperationException("SymbolTools has not been initialized. Call Initialize() first.");
        }
    }

    private static bool IsSymbolDefinition(string line, string symbolName, out string symbolType)
    {
        symbolType = string.Empty;
        
        // Check for class, interface, struct, enum, record definitions
        var match = SymbolRegex.Match(line);
        if (match.Success && match.Groups["n"].Value == symbolName)
        {
            symbolType = "type";
            return true;
        }
        
        // Check for method definitions
        match = MethodRegex.Match(line);
        if (match.Success && match.Groups["n"].Value == symbolName)
        {
            symbolType = "method";
            return true;
        }
        
        // Simple fallback: check if the symbol name is in the line
        // This is less accurate but might catch some cases
        return line.Contains($" {symbolName} ") || line.Contains($" {symbolName}(");
    }
} 