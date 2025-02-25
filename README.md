# NetContextServer

A .NET Codebase Context MCP Server for AI assistants. This tool helps AI assistants understand and navigate your .NET codebase by providing tools to list projects, files, search code, and retrieve documentation.

## Installation

### Global Tool

```bash
# Install from NuGet (once published)
dotnet tool install --global NetContextServer.Tool

# Or install from local build
dotnet pack src/NetContextServer.Tool --configuration Release
dotnet tool install --global --add-source ./src/NetContextServer.Tool/nupkg NetContextServer.Tool
```

## Usage

```bash
# Start the server for the current directory
net-context-server

# Start the server for a specific solution directory
net-context-server --solution "path/to/MySolution"

# Show help
net-context-server --help
```

## Available MCP Tools

The server provides the following tools:

### Project Tools

- **list_projects**: Lists all projects in the solution
- **list_files**: Lists all source files in a project
- **get_solution_path**: Gets the path to the solution file
- **get_solution_root**: Gets the root directory of the solution

### File Tools

- **open_file**: Opens a file and returns its contents
- **search_code**: Searches for a keyword in all source files

### Symbol Tools

- **get_doc_for_symbol**: Gets documentation for a symbol
- **list_symbols**: Lists all symbols in the solution

## Example MCP Requests

```json
// List all projects
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "mcp.call_function",
  "params": {
    "tool": "project_tools",
    "function": "list_projects",
    "params": {}
  }
}

// Search for code
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "mcp.call_function",
  "params": {
    "tool": "file_tools",
    "function": "search_code",
    "params": {
      "keyword": "MyClass"
    }
  }
}

// Get documentation for a symbol
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "mcp.call_function",
  "params": {
    "tool": "symbol_tools",
    "function": "get_doc_for_symbol",
    "params": {
      "symbolName": "MyClass"
    }
  }
}
```

## License

MIT 