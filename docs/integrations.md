# Integration Guide

## Cursor AI Integration

### Quick Setup
1. Open Cursor AI
2. Press `Cmd/Ctrl + Shift + P`
3. Type "Configure MCP Server"
4. Enter configuration:
```json
{
  "command": "dotnet",
  "args": ["run", "--project", "path/to/NetContextServer/src/NetContextServer/NetContextServer.csproj"]
}
```

### Verification
Test the integration directly in Cursor AI's chat interface by asking questions like:

1. Basic file operations:
   - "What's the current base directory?"
   - "List all projects in this solution"
   - "Show me the source files in this project"

2. Search capabilities:
   - "Search for files containing 'Program'"
   - "Find code related to authentication"
   - "Look for error handling implementations"

3. Package analysis:
   - "Analyze the NuGet packages in this project"
   - "Show me all project dependencies"

The AI assistant will use NetContextServer's capabilities to answer these questions directly in the chat.

## MCP Protocol Support

NetContextServer implements the [Model Context Protocol](https://modelcontextprotocol.io/introduction) for AI tool integration.

### Supported Operations
- File system navigation
- Code search (text and semantic)
- Project structure analysis
- Package analysis
- Pattern management

### Security
- All operations respect base directory restrictions
- Sensitive file patterns are automatically ignored
- File size limits are enforced

## Debugging Integration

### Using CursorMCPMonitor
[CursorMCPMonitor](https://github.com/willibrandon/CursorMCPMonitor) provides real-time monitoring of MCP communications.

1. Installation:
```bash
git clone https://github.com/willibrandon/CursorMCPMonitor.git
cd CursorMCPMonitor
dotnet build
```

2. Usage:
```bash
dotnet run --project CursorMCPMonitor.csproj
```

### Common Integration Issues

1. **Connection Failed**
   - Verify NetContextServer is running
   - Check port availability
   - Confirm path configuration

2. **Permission Errors**
   - Verify base directory setting
   - Check file access permissions
   - Review ignore patterns

3. **Search Not Working**
   - Confirm Azure OpenAI setup (if using semantic search)
   - Verify file indexing status

## Other AI Tools

NetContextServer can integrate with any tool supporting the MCP protocol. Key requirements:

1. **Tool Requirements**
   - MCP protocol support
   - .NET file system compatibility
   - Base directory respect

2. **Configuration Steps**
   - Point tool to NetContextServer endpoint
   - Configure authentication (if required)
   - Set up base directory 