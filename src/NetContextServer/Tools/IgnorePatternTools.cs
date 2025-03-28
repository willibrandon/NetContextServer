using ModelContextProtocol.Server;
using NetContextServer.Services;
using System.ComponentModel;

namespace NetContextServer.Tools;

/// <summary>
/// Provides MCP tools for managing file ignore patterns used during file operations.
/// </summary>
[McpServerToolType]
public static class IgnorePatternTools
{
    /// <summary>
    /// Adds new patterns to the ignore list for file scanning operations.
    /// </summary>
    /// <param name="patterns">Array of glob patterns to ignore (e.g. ['*.generated.cs', 'bin/*']).</param>
    /// <returns>A JSON string containing the results of the operation, including any invalid patterns.</returns>
    [McpServerTool("add_ignore_patterns")]
    [Description("Adds new patterns to the ignore list for file scanning operations.")]
    public static string AddIgnorePatterns(
        [Description("Array of glob patterns to ignore (e.g. ['*.generated.cs', 'bin/*'])")]
        string[] patterns) =>
        IgnorePatternService.AddIgnorePatterns(patterns);

    /// <summary>
    /// Removes all user-defined ignore patterns from both memory and the persistent state file.
    /// </summary>
    /// <returns>A JSON string containing the updated pattern lists after clearing.</returns>
    [McpServerTool("clear_ignore_patterns")]
    [Description("Removes all user-defined ignore patterns from both memory and the persistent state file.")]
    public static string ClearIgnorePatterns() => 
        IgnorePatternService.ClearIgnorePatterns();

    /// <summary>
    /// Retrieves the current list of active ignore patterns from memory.
    /// </summary>
    /// <returns>A JSON string containing all active ignore patterns, including both default and user-defined patterns.</returns>
    [McpServerTool("get_ignore_patterns")]
    [Description("Retrieves the current list of active ignore patterns from memory.")]
    public static string GetIgnorePatterns() => 
        IgnorePatternService.GetIgnorePatterns();

    /// <summary>
    /// Returns the absolute path to the ignore_patterns.json state file.
    /// </summary>
    /// <returns>A JSON string containing the path to the state file.</returns>
    [McpServerTool("get_state_file_location")]
    [Description("Returns the absolute path to the ignore_patterns.json state file.")]
    public static string GetStateFileLocation() => 
        IgnorePatternService.GetStateFileLocation();

    /// <summary>
    /// Removes specific patterns from the ignore list.
    /// </summary>
    /// <param name="patterns">Array of glob patterns to remove from the ignore list.</param>
    /// <returns>A JSON string containing the results of the operation and updated pattern lists.</returns>
    [McpServerTool("remove_ignore_patterns")]
    [Description("Removes specific patterns from the ignore list.")]
    public static string RemoveIgnorePatterns(
        [Description("Array of glob patterns to remove from the ignore list")]
        string[] patterns) => 
        IgnorePatternService.RemoveIgnorePatterns(patterns);
}
