using ModelContextProtocol;
using ModelContextProtocol.Server;
using NetContextServer.Services;
using System.ComponentModel;

namespace NetContextServer.Tools;

[McpToolType]
public static class IgnorePatternTools
{
    [McpTool("add_ignore_patterns")]
    [Description("Adds new patterns to the ignore list for file scanning operations.")]
    public static string AddIgnorePatterns(
        [Description("Array of glob patterns to ignore (e.g. ['*.generated.cs', 'bin/*'])")]
        string[] patterns) =>
        IgnorePatternService.AddIgnorePatterns(patterns);

    [McpTool("clear_ignore_patterns")]
    [Description("Removes all user-defined ignore patterns from both memory and the persistent state file.")]
    public static string ClearIgnorePatterns() => 
        IgnorePatternService.ClearIgnorePatterns();

    [McpTool("get_ignore_patterns")]
    [Description("Retrieves the current list of active ignore patterns from memory.")]
    public static string GetIgnorePatterns() => 
        IgnorePatternService.GetIgnorePatterns();

    [McpTool("get_state_file_location")]
    [Description("Returns the absolute path to the ignore_patterns.json state file.")]
    public static string GetStateFileLocation() => 
        IgnorePatternService.GetStateFileLocation();

    [McpTool("remove_ignore_patterns")]
    [Description("Removes specific patterns from the ignore list.")]
    public static string RemoveIgnorePatterns(
        [Description("Array of glob patterns to remove from the ignore list")]
        string[] patterns) => 
        IgnorePatternService.RemoveIgnorePatterns(patterns);
} 