# Getting Started with NetContextServer

Welcome to NetContextServer! This guide will help you get up and running in just a few minutes. NetContextServer is a powerful tool that enhances AI coding assistants by giving them deep understanding of your .NET codebase.

## Why NetContextServer?

When you're using AI coding assistants like Cursor AI or Claude, they work best when they understand your codebase. NetContextServer bridges this gap by:
- 🔍 Enabling AI to search and understand your code semantically
- 📁 Providing secure access to your project structure and files
- 🛡️ Maintaining security with built-in safeguards for sensitive data
- 🚀 Making AI suggestions more relevant to your specific project

## Quick Start (5 Minutes)

### Prerequisites
- .NET 9.0 or later
- For semantic search: Azure OpenAI account (optional but recommended)
- Git

### 1. Install NetContextServer

```bash
# Clone the repository
git clone https://github.com/willibrandon/NetContextServer.git
cd NetContextServer

# Build the solution
dotnet build
```

### 2. Set Up Environment (for Semantic Search)

Create a `.env` file in the root directory:
```env
AZURE_OPENAI_ENDPOINT=your_azure_openai_endpoint
AZURE_OPENAI_API_KEY=your_azure_openai_key
```

> 💡 **Note**: NetContextServer works without Azure OpenAI, but semantic search will be disabled.

### 3. Test the Installation

```bash
# Run a simple hello test
dotnet run --project src/NetContextClient/NetContextClient.csproj -- hello

# Set your project directory
dotnet run --project src/NetContextClient/NetContextClient.csproj -- set-base-dir --directory "path/to/your/project"

# Try a semantic search
dotnet run --project src/NetContextClient/NetContextClient.csproj -- semantic-search --query "find authentication logic"
```

## Integration with Cursor AI

Cursor AI is one of the primary ways to use NetContextServer. Here's how to set it up:

1. Open Cursor AI
2. Press `Cmd/Ctrl + Shift + P` to open the command palette
3. Type "Configure MCP Server" and select it
4. Enter the following configuration:
   ```json
   {
     "command": "dotnet",
     "args": ["run", "--project", "path/to/NetContextServer/src/NetContextServer/NetContextServer.csproj"]
   }
   ```
5. Start coding! Try asking Cursor AI questions about your codebase.

Example prompts to try:
- "Find all authentication-related code in the project"
- "Show me how error handling is implemented"
- "List all API endpoints in the codebase"

## Common Operations

### Project Navigation
```bash
# List all projects in a directory
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-projects-in-dir --directory "path/to/dir"

# List source files in a project
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-source-files --project-dir "path/to/project"
```

### Code Search
```bash
# Text-based search
dotnet run --project src/NetContextClient/NetContextClient.csproj -- search-code --text "authentication"

# Semantic search (requires Azure OpenAI)
dotnet run --project src/NetContextClient/NetContextClient.csproj -- semantic-search --query "how is user data validated"
```

### Security Management
```bash
# Add files to ignore
dotnet run --project src/NetContextClient/NetContextClient.csproj -- add-ignore-patterns --patterns "*.secret" "*.key"

# View current ignore patterns
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-ignore-patterns
```

## Troubleshooting

### Common Issues

1. **"Directory not found" error**
   - Ensure the path exists and is accessible
   - Use absolute paths if relative paths aren't working

2. **Semantic search not working**
   - Check your Azure OpenAI environment variables
   - Verify your Azure OpenAI service is running
   - Ensure you have embedding model access

3. **Access denied errors**
   - Verify the file/directory is within your set base directory
   - Check file permissions
   - Review ignore patterns that might be blocking access

### Getting Help

- Check the [GitHub Issues](https://github.com/willibrandon/NetContextServer/issues) for known problems
- Use [CursorMCPMonitor](https://github.com/willibrandon/CursorMCPMonitor) to debug MCP communication
- Create a new issue if you find a bug or have a feature request

## Next Steps

- Explore the [full tool reference](./tool-reference.md)
- Learn about [advanced configuration options](./configuration.md)
- See [integration examples](./integrations.md) with other AI tools
- Check out our [contribution guidelines](../CONTRIBUTING.md) if you want to help improve NetContextServer

## Tips for Best Results

1. **Set a Specific Base Directory**
   - Always set the base directory to your project root
   - This ensures AI tools have access to all relevant files

2. **Use Semantic Search Effectively**
   - Write natural language queries
   - Be specific about what you're looking for
   - Use domain terminology from your codebase

3. **Manage Ignore Patterns**
   - Add patterns for generated files to reduce noise
   - Don't forget to ignore sensitive files
   - Review patterns periodically

4. **Keep the Server Running**
   - Start NetContextServer before beginning your coding session
   - It will maintain an index of your code for faster searches 