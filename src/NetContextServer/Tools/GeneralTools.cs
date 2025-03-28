using ModelContextProtocol.Server;
using System.ComponentModel;

namespace NetContextServer.Tools;

/// <summary>
/// Provides general utility tools for the MCP server.
/// </summary>
[McpServerToolType]
public static class GeneralTools
{
    /// <summary>
    /// Simple health check endpoint that returns a greeting message.
    /// </summary>
    /// <returns>A greeting message indicating the server is operational.</returns>
    [McpServerTool("hello")]
    [Description("Simple health check endpoint that returns a greeting message.")]
    public static string Hello() => "hello, claude.";
}
