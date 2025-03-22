# NetContextServer

A .NET Codebase Context MCP Server that provides AI assistants with access to your .NET codebase through the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/introduction). This allows AI tools to understand and work with your specific codebase context.

## Features

- 📁 **Project & File Listing**: List all projects and source files in your solution
- 🔍 **Code Search**: Search through your codebase for specific patterns or text
- 🧠 **Semantic Search**: Find code based on meaning, not just exact text matches
- 📖 **File Content Access**: Read source files with safety checks and size limits
- 🛡️ **Security**: Built-in safeguards for sensitive files and directory access
- 🎯 **Pattern Management**: Flexible ignore patterns for controlling file access

## Building from Source

1. Clone the repository:
```bash
git clone https://github.com/willibrandon/NetContextServer.git
cd NetContextServer
```

2. Build the solution:
```bash
dotnet build
```

3. Run the tests (optional):
```bash
dotnet test
```

## Running Commands

Use the client to interact with your codebase:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- <command> [options]
```

### Environment Setup

For semantic search functionality, you need to set up the following environment variables:
- `AZURE_OPENAI_ENDPOINT`: Your Azure OpenAI endpoint URL
- `AZURE_OPENAI_API_KEY`: Your Azure OpenAI API key

## Usage

### Basic Commands

1. **Set Base Directory**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- set-base-dir --directory "D:\YourProject"
```

2. **Get Base Directory**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-base-dir
```

3. **List Projects**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-projects-in-dir --directory "D:\YourProject\src"
```

4. **List Source Files**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-source-files --project-dir "D:\YourProject\src\YourProject"
```

5. **Analyze Packages**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- analyze-packages
```

### Search Commands

1. **Text Search**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- search-code --text "authentication"
```

2. **Semantic Search**:
```bash
# Search with default number of results (5)
dotnet run --project src/NetContextClient/NetContextClient.csproj -- semantic-search --query "handle user authentication"

# Search with custom number of results
dotnet run --project src/NetContextClient/NetContextClient.csproj -- semantic-search --query "database connection string" --top 10
```

The semantic search feature:
- Uses embeddings to find code based on meaning
- Returns code snippets ranked by relevance
- Shows line numbers and similarity scores
- Automatically indexes your code on first search

### Pattern Management

1. **Add Ignore Patterns**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- add-ignore-patterns --patterns "*.txt" "*.log"
```

2. **View Current Patterns**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-ignore-patterns
```

3. **Remove Specific Patterns**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- remove-ignore-patterns --patterns "*.txt"
```

4. **Clear User Patterns**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- clear-ignore-patterns
```

5. **View State File Location**:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-state-file-location
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

1. Set the base directory for your project:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- set-base-dir --directory "D:\Projects\MyApp"
```

2. Set up custom ignore patterns:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- add-ignore-patterns --patterns "*.generated.cs" "*.designer.cs"
```

3. List all projects:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-projects-in-dir --directory "D:\Projects\MyApp\src"
```

4. Search for authentication-related code:
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- semantic-search --query "user authentication and authorization logic"
```

## Integration with AI Coding Tools

NetContextServer implements the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/introduction), allowing seamless integration with AI coding assistants that support this protocol, such as:

- **Cursor AI**: Provides your AI assistant with full context of your codebase for more accurate code generation and assistance
- **Other MCP-compatible tools**: Any tool that implements the Model Context Protocol can connect to NetContextServer

To use with Cursor AI:
1. Configure Cursor AI to use the NetContextServer as its MCP provider
2. Enjoy enhanced code assistance with full codebase context

The Model Context Protocol enables AI tools to request specific information about your codebase, making the AI's suggestions more relevant and accurate to your specific project structure and coding patterns.

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

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.
