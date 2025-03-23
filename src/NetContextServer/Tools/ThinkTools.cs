using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NetContextServer.Tools;

/// <summary>
/// Provides a tool for structured thinking during complex operations, allowing the model
/// to reason about its actions without making state changes.
/// </summary>
[McpToolType]
public static class ThinkTools
{
    /// <summary>
    /// Default JSON serializer options used for think tool output.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Maximum allowed length for thought parameter to prevent memory issues.
    /// </summary>
    private const int MaxThoughtLength = 32768; // 32KB

    /// <summary>
    /// Maximum log file size before rotation (5MB).
    /// </summary>
    private const int MaxLogFileSize = 5 * 1024 * 1024;

    /// <summary>
    /// Standard error messages.
    /// </summary>
    private static readonly string ErrorMissingThought = "Error: Missing required parameter 'thought'";
    private static readonly string ErrorThoughtTooLarge = $"Error: Thought exceeds maximum length of {MaxThoughtLength} characters";
    private static readonly string ErrorInvalidContent = "Error: Thought contains invalid content";

    /// <summary>
    /// Pattern to detect potentially harmful content.
    /// </summary>
    private static readonly Regex InvalidContentPattern = new(
        @"(?i)(exec\s+\{|system\s*\(|eval\s*\(|<script|javascript:)",
        RegexOptions.Compiled);

    /// <summary>
    /// Provides a space for structured thinking during complex operations.
    /// This tool allows the model to reason about its actions, verify compliance with rules,
    /// and plan next steps without making any state changes.
    /// </summary>
    /// <param name="thought">The thought or reasoning to process.</param>
    /// <returns>A JSON string containing the processed thought and a confirmation message.</returns>
    /// <remarks>
    /// This tool is particularly useful for:
    /// - Analyzing complex tool outputs before taking action
    /// - Verifying compliance with policies and rules
    /// - Planning multi-step operations
    /// - Breaking down complex problems into manageable steps
    /// 
    /// Performance considerations:
    /// - Thoughts are limited to 32KB to prevent memory issues
    /// - JSON serialization is optimized for indented output
    /// - Optional logging can be enabled for debugging
    /// - Log files are automatically rotated at 5MB
    /// 
    /// Security considerations:
    /// - Content is validated against potentially harmful patterns
    /// - Unicode and special characters are preserved
    /// - Timestamps are in UTC for consistent logging
    /// </remarks>
    [McpTool("think")]
    [Description("Provides a space for structured thinking during complex operations, without making any state changes.")]
    public static string Think(
        [Description("The thought or reasoning to process")]
        string thought)
    {
        // Validate input
        if (string.IsNullOrEmpty(thought))
        {
            return JsonSerializer.Serialize(new { Error = ErrorMissingThought }, DefaultJsonOptions);
        }

        // Check size limit
        if (thought.Length > MaxThoughtLength)
        {
            return JsonSerializer.Serialize(new { Error = ErrorThoughtTooLarge }, DefaultJsonOptions);
        }

        // Validate content
        if (InvalidContentPattern.IsMatch(thought))
        {
            return JsonSerializer.Serialize(new { Error = ErrorInvalidContent }, DefaultJsonOptions);
        }

        try
        {
            var timestamp = DateTime.UtcNow;
            var category = DetermineCategory(thought);
            
            var response = new
            {
                Thought = thought,
                Message = "Thought processed successfully",
                Category = category,
                Timestamp = timestamp.ToString("o"), // ISO 8601 format
                CharacterCount = thought.Length
            };

            // Log the thought if logging is enabled
            if (bool.TryParse(Environment.GetEnvironmentVariable("NETCONTEXT_LOG_THOUGHTS"), out bool shouldLog) && shouldLog)
            {
                LogThought(response);
            }

            return JsonSerializer.Serialize(response, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Error = $"Error processing thought: {ex.Message}" }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Determines the category of a thought based on its content.
    /// </summary>
    private static string DetermineCategory(string thought)
    {
        var lowerThought = thought.ToLowerInvariant();
        
        if (lowerThought.Contains("refactor") || lowerThought.Contains("restructure"))
            return "Refactoring";
        if (lowerThought.Contains("security") || lowerThought.Contains("vulnerability"))
            return "Security";
        if (lowerThought.Contains("performance") || lowerThought.Contains("optimization"))
            return "Performance";
        if (lowerThought.Contains("test") || lowerThought.Contains("debug"))
            return "Testing";
        if (lowerThought.Contains("architecture") || lowerThought.Contains("design"))
            return "Architecture";
        
        return "General";
    }

    /// <summary>
    /// Logs the thought for debugging purposes if enabled.
    /// </summary>
    private static void LogThought(object thoughtResponse)
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);

            var logPath = Path.Combine(logDir, "thoughts.log");
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {JsonSerializer.Serialize(thoughtResponse)}{Environment.NewLine}";
            
            // Check if we need to rotate the log file
            if (File.Exists(logPath) && new FileInfo(logPath).Length > MaxLogFileSize)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var archivePath = Path.Combine(logDir, $"thoughts_{timestamp}.log");
                File.Move(logPath, archivePath);
            }

            File.AppendAllText(logPath, logEntry);
        }
        catch
        {
            // Silently fail logging - shouldn't impact the main functionality
        }
    }
}
