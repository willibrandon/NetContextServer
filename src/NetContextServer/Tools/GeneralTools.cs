using ModelContextProtocol.Server;
using NetContextServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace NetContextServer.Tools;

/// <summary>
/// Provides general utility tools for the MCP server.
/// </summary>
[McpServerToolType]
public static class GeneralTools
{
    /// <summary>
    /// Default JSON serializer options used for version information output.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Simple health check endpoint that returns a greeting message.
    /// </summary>
    /// <returns>A greeting message indicating the server is operational.</returns>
    [McpServerTool("hello")]
    [Description("Simple health check endpoint that returns a greeting message.")]
    public static string Hello() => "hello, claude.";

    /// <summary>
    /// Gets version and configuration information about the NetContextServer.
    /// </summary>
    /// <returns>A JSON string containing version information and current configuration.</returns>
    [McpServerTool("version")]
    [Description("Gets version and configuration information about the NetContextServer.")]
    public static string GetVersion() => 
        JsonSerializer.Serialize(VersionService.GetVersionInfo(), DefaultJsonOptions);
}
