# NetContextServer

A .NET Codebase Context MCP Server that provides AI assistants with access to your .NET codebase through the Model Context Protocol (MCP). This allows AI tools to understand and work with your specific codebase context.

## Features

- 📁 **Project & File Listing**: List all projects and source files in your solution
- 🔍 **Code Search**: Search through your codebase for specific patterns or text
- 📖 **File Content Access**: Read source files with safety checks and size limits
- 🛡️ **Security**: Built-in safeguards for sensitive files and directory access
- 🎯 **Pattern Management**: Flexible ignore patterns for controlling file access

## Installation

Install as a global .NET tool:

```bash
dotnet tool install --global NetContextServer.Tool
```

## Usage

### Basic Commands

1. **Start the Server**:
```bash
net-context-server --solution "path/to/your/Solution.sln"
```

2. **Set Base Directory**:
```bash
NetContextClient.exe set-base-dir --directory "D:\YourProject"
```

3. **List Projects**:
```bash
NetContextClient.exe list-projects-in-dir --directory "D:\YourProject\src"
```

4. **List Source Files**:
```bash
NetContextClient.exe list-source-files --project-dir "D:\YourProject\src\YourProject"
```

### Pattern Management

1. **Add Ignore Patterns**:
```bash
NetContextClient.exe add-ignore-patterns --patterns "*.txt" "*.log"
```

2. **View Current Patterns**:
```bash
NetContextClient.exe get-ignore-patterns
```

3. **Remove Specific Patterns**:
```bash
NetContextClient.exe remove-ignore-patterns --patterns "*.txt"
```

4. **Clear User Patterns**:
```bash
NetContextClient.exe clear-ignore-patterns
```

5. **View State File Location**:
```bash
NetContextClient.exe get-state-file-location
```

### Default Ignore Patterns

The following patterns are ignored by default to protect sensitive information:
- `*.env` - Environment files
- `appsettings.*.json` - Application settings
- `*.pfx` - Certificate files
- `*.key` - Key files
- `*.pem` - PEM files
- `*password*` - Files containing "password" in the name
- `*secret*` - Files containing "secret" in the name

## Security Features

1. **Path Safety**: Files can only be accessed within the specified base directory
2. **Pattern Validation**: Ignore patterns are validated for proper syntax
3. **Size Limits**: Large file contents are truncated to prevent memory issues
4. **Sensitive File Protection**: Built-in patterns protect common sensitive files

## Example Workflow

1. Start the server pointing to your solution:
```bash
net-context-server --solution "D:\Projects\MyApp\MyApp.sln"
```

2. Set up custom ignore patterns:
```bash
NetContextClient.exe add-ignore-patterns --patterns "*.generated.cs" "*.designer.cs"
```

3. List all projects:
```bash
NetContextClient.exe list-projects-in-dir --directory "D:\Projects\MyApp\src"
```

4. List source files in a specific project:
```bash
NetContextClient.exe list-source-files --project-dir "D:\Projects\MyApp\src\MyApp.Core"
```

## Error Handling

The server provides clear error messages for common scenarios:
- Directory not found
- Access denied (outside base directory)
- Invalid patterns
- File size limits exceeded
- Restricted file types

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built with [MCPSharp](https://github.com/your-reference-here/MCPSharp)
- Inspired by the Model Context Protocol 