namespace NetContextServer.Extensions;

/// <summary>
/// Provides extension methods for string manipulation and code content formatting.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Extracts a name from a line of code following a specified keyword.
    /// </summary>
    /// <param name="line">The line of code to analyze.</param>
    /// <param name="keyword">The keyword that precedes the name to extract.</param>
    /// <returns>The extracted name, or an empty string if no name is found.</returns>
    /// <remarks>
    /// The method looks for the first occurrence of the keyword followed by a space,
    /// then extracts the text until it encounters a space, opening brace, colon, or parenthesis.
    /// </remarks>
    public static string ExtractName(string line, string keyword)
    {
        int keywordIndex = line.IndexOf(keyword + " ");
        if (keywordIndex < 0)
            return string.Empty;
            
        string afterKeyword = line[(keywordIndex + keyword.Length + 1)..].Trim();
        
        // Find the end of the name (at first space, opening brace, colon, or parenthesis)
        int endIndex = afterKeyword.Length;
        int spaceIndex = afterKeyword.IndexOf(' ');
        int braceIndex = afterKeyword.IndexOf('{');
        int colonIndex = afterKeyword.IndexOf(':');
        int parenIndex = afterKeyword.IndexOf('(');
        
        if (spaceIndex >= 0 && spaceIndex < endIndex) endIndex = spaceIndex;
        if (braceIndex >= 0 && braceIndex < endIndex) endIndex = braceIndex;
        if (colonIndex >= 0 && colonIndex < endIndex) endIndex = colonIndex;
        if (parenIndex >= 0 && parenIndex < endIndex) endIndex = parenIndex;
        
        if (endIndex > 0)
        {
            return afterKeyword[..endIndex].Trim();
        }
        
        return string.Empty;
    }

    /// <summary>
    /// Extracts a method name from a line of code.
    /// </summary>
    /// <param name="line">The line of code containing the method declaration.</param>
    /// <returns>The extracted method name, or an empty string if no method name is found.</returns>
    /// <remarks>
    /// The method extracts the text between the last space and the opening parenthesis
    /// in the line, which typically represents the method name in a method declaration.
    /// </remarks>
    public static string ExtractMethodName(string line)
    {
        // Extract the method name from the line (text before the opening parenthesis)
        int parenIndex = line.IndexOf('(');
        if (parenIndex <= 0)
            return string.Empty;
            
        string beforeParen = line[..parenIndex].Trim();
        
        // Find the last space before the parenthesis
        int lastSpaceIndex = beforeParen.LastIndexOf(' ');
        if (lastSpaceIndex >= 0 && lastSpaceIndex < beforeParen.Length - 1)
        {
            // Return everything after the last space
            return beforeParen[(lastSpaceIndex + 1)..].Trim();
        }
        
        return string.Empty;
    }

    /// <summary>
    /// Formats code content by managing blank lines and structural elements for improved readability.
    /// </summary>
    /// <param name="content">The code content to format.</param>
    /// <returns>The formatted code content with optimized blank line placement.</returns>
    /// <remarks>
    /// This method performs the following formatting operations:
    /// <list type="bullet">
    /// <item><description>Removes excessive blank lines</description></item>
    /// <item><description>Ensures proper structure with opening/closing braces</description></item>
    /// <item><description>Adds strategic blank lines before significant declarations</description></item>
    /// <item><description>Maintains a maximum ratio of 25% blank lines</description></item>
    /// </list>
    /// </remarks>
    public static string FormatCodeContent(string content)
    {
        // First, remove all blank lines completely
        var lines = content.Split('\n');
        var nonBlankLines = new List<string>();
        
        // Track if we've seen important structural elements
        bool hasOpeningBrace = content.Contains('{');
        
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                nonBlankLines.Add(line);
            }
        }
        
        // If there are no non-blank lines, return the original content
        if (nonBlankLines.Count == 0)
            return content.Trim();
            
        // If we don't have an opening brace but have a class/method declaration,
        // try to find and include the opening brace from the original content
        if (!hasOpeningBrace)
        {
            var firstLine = nonBlankLines[0].Trim();
            if (firstLine.Contains("class ") || 
                firstLine.Contains("interface ") ||
                firstLine.Contains("struct ") ||
                firstLine.Contains("enum ") ||
                (firstLine.Contains("void ") && firstLine.Contains('(')) ||
                (firstLine.Contains("public ") && firstLine.Contains('(')) ||
                (firstLine.Contains("private ") && firstLine.Contains('(')) ||
                (firstLine.Contains("protected ") && firstLine.Contains('(')) ||
                (firstLine.Contains("internal ") && firstLine.Contains('(')))
            {
                // Find the line with the opening brace in the original content
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains('{'))
                    {
                        nonBlankLines.Add(lines[i]);
                        hasOpeningBrace = true;
                        break;
                    }
                }
                
                // If we still don't have an opening brace, add one
                if (!hasOpeningBrace)
                {
                    nonBlankLines.Add("{");
                    nonBlankLines.Add("    // Content truncated");
                    nonBlankLines.Add("}");
                }
            }
        }
        
        // Now add strategic blank lines only where needed for readability
        var result = new List<string>();
        bool addedBlankLine = false;
        
        for (int i = 0; i < nonBlankLines.Count; i++)
        {
            var line = nonBlankLines[i];
            var trimmedLine = line.TrimStart();
            
            // Add a blank line before class, method, or control structure definitions
            // but only if we haven't just added a blank line
            bool isSignificantLine = 
                trimmedLine.Contains("class ") || 
                trimmedLine.Contains("interface ") ||
                trimmedLine.Contains("struct ") ||
                trimmedLine.Contains("enum ") ||
                (trimmedLine.Contains("void ") && trimmedLine.Contains('(')) ||
                (trimmedLine.Contains("public ") && trimmedLine.Contains('(')) ||
                (trimmedLine.Contains("private ") && trimmedLine.Contains('(')) ||
                (trimmedLine.Contains("protected ") && trimmedLine.Contains('(')) ||
                (trimmedLine.Contains("internal ") && trimmedLine.Contains('('));
            
            if (isSignificantLine && !addedBlankLine && i > 0)
            {
                result.Add("");
                addedBlankLine = true;
            }
            else
            {
                addedBlankLine = false;
            }
            
            result.Add(line);
        }
        
        // Ensure we don't have more than 1/4 blank lines
        int totalLines = result.Count;
        int blankLines = result.Count(l => string.IsNullOrWhiteSpace(l));
        int maxAllowedBlankLines = totalLines / 4; // 25% rule
        
        if (blankLines > maxAllowedBlankLines)
        {
            // Remove some blank lines to meet the threshold
            var finalResult = new List<string>();
            int blankLinesToKeep = maxAllowedBlankLines;
            
            foreach (var line in result)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (blankLinesToKeep > 0)
                    {
                        finalResult.Add(line);
                        blankLinesToKeep--;
                    }
                    // Skip this blank line if we've reached our quota
                }
                else
                {
                    finalResult.Add(line);
                }
            }
            
            return string.Join("\n", finalResult).Trim();
        }
        
        return string.Join("\n", result).Trim();
    }
}
