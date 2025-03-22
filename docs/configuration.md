# Advanced Configuration Guide

This guide covers advanced configuration options for NetContextServer to help you customize and optimize its behavior for your specific needs.

## Environment Variables

### Azure OpenAI Configuration
Required for semantic search functionality:
```bash
# Azure OpenAI Settings
AZURE_OPENAI_ENDPOINT=your_azure_openai_endpoint
AZURE_OPENAI_API_KEY=your_azure_openai_key
```

## Base Directory Configuration

The base directory is a critical security feature that defines the root of your workspace. All file operations are restricted to this directory and its subdirectories.

### Setting the Base Directory

You can set the base directory in several ways:

1. **Command Line**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- set-base-dir --directory "D:\YourProject"
```

2. **Environment Variable** (optional):
```bash
NETCONTEXT_BASE_DIR=D:\YourProject
```

### Base Directory Best Practices

1. Set to your solution or project root
2. Ensure it contains all necessary source files
3. Avoid setting to system directories or root paths
4. Use absolute paths for reliability

## Security Configuration

### Ignore Patterns

NetContextServer uses ignore patterns to protect sensitive files and optimize performance.

#### Default Ignore Patterns
These patterns are always active and cannot be disabled:
```
*.env
appsettings.*.json
*.pfx
*.key
*.pem
*password*
*secret*
```

#### Custom Ignore Patterns
Add your own patterns for project-specific needs:
```bash
# Add patterns
dotnet run --project src/NetContextClient/NetContextClient.csproj -- add-ignore-patterns --patterns "*.generated.cs" "bin/*" "obj/*"

# View current patterns
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-ignore-patterns
```

#### Persistent Ignore Patterns
Patterns are stored in `ignore_patterns.json`. Location can be found using:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-state-file-location
```

### File Access Controls

1. **Size Limits**
   - Large files are truncated to prevent memory issues
   - Default limit: 1MB per file
   - Binary files are blocked

2. **Path Validation**
   - All file paths must be within base directory
   - Symbolic links are resolved and validated
   - Parent directory traversal (`..`) is blocked

## Search Configuration

### Text Search

Text search is configured for:
- Case-sensitive matching
- Regular expression support
- Line number reporting
- Context lines around matches

### Semantic Search

Semantic search can be customized with:

1. **Results Count**
```bash
# Customize number of results
dotnet run --project src/NetContextClient/NetContextClient.csproj -- semantic-search --query "error handling" --top 10
```

2. **Index Management**
- Files are automatically indexed on first search
- Index is maintained in memory for performance
- Reindexing occurs when files change

## Performance Optimization

### File Scanning

1. **Excluded Directories**
   - `bin` and `obj` directories are skipped by default
   - Add custom directories to ignore for faster scanning

2. **File Type Filtering**
   - Focus on relevant file types
   - Add ignore patterns for non-code files

### Memory Management

1. **File Content Caching**
   - Recently accessed files are cached
   - Large files are read in chunks

2. **Search Index**
   - Semantic search index is optimized for speed
   - Index is updated incrementally

## Integration Configuration

### Cursor AI Setup

1. **MCP Server Configuration**
```json
{
  "command": "dotnet",
  "args": ["run", "--project", "path/to/NetContextServer/src/NetContextServer/NetContextServer.csproj"]
}
```

2. **Recommended Settings**
   - Keep server running for better performance
   - Configure in workspace settings for project-specific setup

### CursorMCPMonitor Integration

For debugging MCP communication:
1. Install CursorMCPMonitor
2. Configure logging level
3. Monitor real-time interactions

## Logging and Diagnostics

### Log Levels
Available log levels:
- ERROR: Critical issues
- WARN: Important warnings
- INFO: General operation info
- DEBUG: Detailed debugging info

### Diagnostic Information
- Server status
- Connection details
- Operation timing
- Error traces

## Best Practices

1. **Project Structure**
   - Set base directory at solution root
   - Use consistent file organization
   - Follow standard .NET project layout

2. **Security**
   - Review ignore patterns regularly
   - Keep sensitive files outside source tree
   - Use environment variables for secrets

3. **Performance**
   - Optimize ignore patterns
   - Clean up unused files
   - Monitor memory usage

4. **Integration**
   - Test configuration changes
   - Verify file access patterns
   - Monitor error logs

## Troubleshooting

### Common Issues

1. **Access Denied**
   - Verify base directory setting
   - Check file permissions
   - Review ignore patterns

2. **Search Not Working**
   - Verify Azure OpenAI credentials
   - Check file indexing status
   - Review search patterns

3. **Performance Issues**
   - Optimize ignore patterns
   - Clean up temporary files
   - Review large file handling
