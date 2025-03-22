using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace NetContextServer.Tools;

[McpToolType]
public static class GeneralTools
{
    [McpTool("hello")]
    [Description("Simple health check endpoint that returns a greeting message.")]
    public static string Hello() => "hello, claude.";
} 