using NetContextServer.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NetContextServer.Services;

internal static class IgnorePatternService
{
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

    private static HashSet<string> UserIgnorePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ignore_patterns.json");

    static IgnorePatternService()
    {
        LoadState();
    }

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

    public static string GetStateFileLocation()
    {
        return JsonSerializer.Serialize(new { StateFilePath = StateFile });
    }

    private static void SaveState()
    {
        var state = new { UserPatterns = UserIgnorePatterns.ToArray() };
        File.WriteAllText(StateFile, JsonSerializer.Serialize(state));
    }

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
        return JsonSerializer.Serialize(GetAllPatterns().ToArray());
    }

    public static string GetIgnorePatterns()
    {
        LoadState();
        return JsonSerializer.Serialize(GetAllPatterns().ToArray());
    }

    public static string ClearIgnorePatterns()
    {
        UserIgnorePatterns.Clear();
        SaveState();
        return JsonSerializer.Serialize(UserIgnorePatterns.ToArray());
    }

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

    public static IEnumerable<string> GetAllPatterns()
    {
        return DefaultIgnorePatterns.Concat(UserIgnorePatterns).Distinct(StringComparer.OrdinalIgnoreCase);
    }

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
}
