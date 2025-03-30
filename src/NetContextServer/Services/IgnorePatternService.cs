using NetContextServer.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NetContextServer.Services;

/// <summary>
/// Provides functionality for managing file patterns that should be ignored during file operations.
/// Supports both default and user-defined ignore patterns using glob syntax.
/// </summary>
internal static class IgnorePatternService
{
    /// <summary>
    /// Default patterns that are always ignored during file operations.
    /// </summary>
    private static readonly HashSet<string> DefaultIgnorePatterns = new(StringComparer.OrdinalIgnoreCase) 
    { 
        "*.env",
        "appsettings.*.json",
        "*.pfx",
        "*.key",
        "*.pem",
        "*password*",
        "*secret*"
    };

    /// <summary>
    /// User-defined patterns that should be ignored during file operations.
    /// </summary>
    private static HashSet<string> UserIgnorePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Path to the file where ignore patterns are persisted.
    /// </summary>
    private static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ignore_patterns.json");

    static IgnorePatternService()
    {
        LoadState();
    }

    /// <summary>
    /// Validates whether a given pattern is a valid glob pattern.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    /// <returns>True if the pattern is valid; otherwise, false.</returns>
    public static bool IsValidGlobPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        // Basic glob pattern validation
        // Disallow patterns that start with / or \
        if (pattern.StartsWith('/') || pattern.StartsWith('\\'))
            return false;

        // Check for invalid characters
        var invalidChars = Path.GetInvalidFileNameChars()
            .Where(c => c != '*' && c != '?' && c != '[' && c != ']')
            .ToArray();
        
        if (pattern.Any(c => invalidChars.Contains(c)))
            return false;

        // Ensure balanced square brackets if used
        var openBrackets = pattern.Count(c => c == '[');
        var closeBrackets = pattern.Count(c => c == ']');
        if (openBrackets != closeBrackets)
            return false;

        return true;
    }

    /// <summary>
    /// Gets the location of the state file where ignore patterns are stored.
    /// </summary>
    /// <returns>A JSON string containing the state file path.</returns>
    public static string GetStateFileLocation()
    {
        return JsonSerializer.Serialize(new { StateFilePath = StateFile });
    }

    /// <summary>
    /// Saves the current state of user-defined ignore patterns to the state file.
    /// </summary>
    private static void SaveState()
    {
        var state = new { UserPatterns = UserIgnorePatterns.ToArray() };
        File.WriteAllText(StateFile, JsonSerializer.Serialize(state));
    }

    /// <summary>
    /// Loads user-defined ignore patterns from the state file.
    /// </summary>
    private static void LoadState()
    {
        try
        {
            if (File.Exists(StateFile))
            {
                var state = JsonSerializer.Deserialize<UserPatternsState>(File.ReadAllText(StateFile));
                UserIgnorePatterns = new HashSet<string>(state!.UserPatterns, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            UserIgnorePatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Adds new ignore patterns to the user-defined patterns list.
    /// </summary>
    /// <param name="patterns">Array of patterns to add.</param>
    /// <returns>A JSON string containing the results of the operation, including invalid patterns and the updated pattern list.</returns>
    public static string AddIgnorePatterns(string[] patterns)
    {
        var invalidPatterns = new List<string>();
        var validPatterns = new List<string>();

        foreach (var pattern in patterns)
        {
            if (IsValidGlobPattern(pattern))
            {
                UserIgnorePatterns.Add(pattern);
                validPatterns.Add(pattern);
            }
            else
            {
                invalidPatterns.Add(pattern);
            }
        }

        SaveState();
        return JsonSerializer.Serialize(new
        {
            InvalidPatterns = invalidPatterns.ToArray(),
            ValidPatternsAdded = validPatterns.ToArray(),
            AllPatterns = GetAllPatterns().ToArray()
        });
    }

    /// <summary>
    /// Gets all current ignore patterns, including both default and user-defined patterns.
    /// </summary>
    /// <returns>A JSON string containing all active ignore patterns.</returns>
    public static string GetIgnorePatterns()
    {
        LoadState();
        return JsonSerializer.Serialize(new
        {
            DefaultPatterns = DefaultIgnorePatterns.ToArray(),
            UserPatterns = UserIgnorePatterns.ToArray(),
            AllPatterns = GetAllPatterns().ToArray()
        });
    }

    /// <summary>
    /// Clears all user-defined ignore patterns while preserving default patterns.
    /// </summary>
    /// <returns>A JSON string containing the updated pattern lists.</returns>
    public static string ClearIgnorePatterns()
    {
        UserIgnorePatterns.Clear();
        SaveState();
        return JsonSerializer.Serialize(new
        {
            DefaultPatterns = DefaultIgnorePatterns.ToArray(),
            UserPatterns = UserIgnorePatterns.ToArray(),
            AllPatterns = GetAllPatterns().ToArray()
        });
    }

    /// <summary>
    /// Removes specified patterns from the user-defined ignore patterns list.
    /// </summary>
    /// <param name="patterns">Array of patterns to remove.</param>
    /// <returns>A JSON string containing the results of the operation and updated pattern lists.</returns>
    /// <remarks>
    /// Default patterns cannot be removed. Any attempt to remove default patterns will be skipped.
    /// </remarks>
    public static string RemoveIgnorePatterns(string[] patterns)
    {
        var removedPatterns = new List<string>();
        var notFoundPatterns = new List<string>();
        var defaultPatterns = new List<string>();

        foreach (var pattern in patterns)
        {
            if (DefaultIgnorePatterns.Contains(pattern))
            {
                defaultPatterns.Add(pattern);
            }
            else if (UserIgnorePatterns.Remove(pattern))
            {
                removedPatterns.Add(pattern);
            }
            else
            {
                notFoundPatterns.Add(pattern);
            }
        }

        SaveState();
        var allPatterns = GetAllPatterns().ToArray();
        return JsonSerializer.Serialize(new
        {
            DefaultPatterns = DefaultIgnorePatterns.ToArray(),
            UserPatterns = UserIgnorePatterns.ToArray(),
            AllPatterns = allPatterns,
            RemovedPatterns = removedPatterns.ToArray(),
            NotFoundPatterns = notFoundPatterns.ToArray(),
            DefaultPatternsSkipped = defaultPatterns.ToArray()
        });
    }

    /// <summary>
    /// Gets all active ignore patterns, combining both default and user-defined patterns.
    /// </summary>
    /// <returns>An enumerable collection of all active ignore patterns.</returns>
    public static IEnumerable<string> GetAllPatterns()
    {
        return DefaultIgnorePatterns.Concat(UserIgnorePatterns).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a given file should be ignored based on the active ignore patterns.
    /// </summary>
    /// <param name="filePath">The path of the file to check.</param>
    /// <returns>True if the file should be ignored; otherwise, false.</returns>
    public static bool ShouldIgnoreFile(string filePath)
    {
        return GetAllPatterns().Any(pattern =>
        {
            if (pattern.Contains('*'))
            {
                var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                return Regex.IsMatch(Path.GetFileName(filePath), regex, RegexOptions.IgnoreCase);
            }

            return Path.GetFileName(filePath).Equals(pattern, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Checks if there are any user-defined ignore patterns.
    /// </summary>
    /// <returns>True if there are user-defined patterns, false otherwise.</returns>
    public static bool HasUserDefinedPatterns()
    {
        return UserIgnorePatterns.Count > 0;
    }
}
