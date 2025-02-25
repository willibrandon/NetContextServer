# NetContextServer

A .NET Codebase Context MCP Server that provides AI assistants with access to your .NET codebase through the Model Context Protocol (MCP). This allows AI tools to understand and work with your specific codebase context.

## Features

- 📁 **Project & File Listing**: List all projects and source files in your solution
- 🔍 **Code Search**: Search through your codebase for specific patterns or text
- 🧠 **Semantic Search**: Find code based on meaning, not just exact text matches
- 📖 **File Content Access**: Read source files with safety checks and size limits
- 🛡️ **Security**: Built-in safeguards for sensitive files and directory access
- 🎯 **Pattern Management**: Flexible ignore patterns for controlling file access

## Installation

Install as a global .NET tool:

```bash
dotnet tool install --global NetContextServer.Tool
```

### Environment Setup

For semantic search functionality, you need to set up the following environment variables:
- `AZURE_OPENAI_ENDPOINT`: Your Azure OpenAI endpoint URL
- `AZURE_OPENAI_API_KEY`: Your Azure OpenAI API key

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

### Search Commands

1. **Text Search**:
```bash
NetContextClient.exe search-code --text "authentication"
```

2. **Semantic Search**:
```bash
# Search with default number of results (5)
NetContextClient.exe semantic-search --query "handle user authentication"

# Search with custom number of results
NetContextClient.exe semantic-search --query "database connection string" --top 10
```

The semantic search feature:
- Uses embeddings to find code based on meaning
- Returns code snippets ranked by relevance
- Shows line numbers and similarity scores
- Automatically indexes your code on first search

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

4. Search for authentication-related code:
```bash
NetContextClient.exe semantic-search --query "user authentication and authorization logic"
```

## Error Handling

The server provides clear error messages for common scenarios:
- Directory not found
- Access denied (outside base directory)
- Invalid patterns
- File size limits exceeded
- Restricted file types
- Missing environment variables for semantic search

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
- Uses Azure OpenAI for semantic embeddings
- Inspired by the Model Context Protocol 