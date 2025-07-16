# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NetContextServer is a .NET 9.0 Model Context Protocol (MCP) server that provides AI coding assistants with deep understanding of .NET codebases. It consists of three main projects:
- `NetContextServer`: MCP server implementation
- `NetContextClient`: CLI interface for server interaction  
- `NetContextServer.Tests`: Comprehensive test suite

## Essential Commands

### Development
```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the server
dotnet run --project src/NetContextServer/NetContextServer.csproj

# Run the client with a command
dotnet run --project src/NetContextClient/NetContextClient.csproj -- [command]

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestName"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Publishing
```bash
# Publish for Windows
dotnet publish -c Release -r win-x64 --self-contained

# Publish for Linux
dotnet publish -c Release -r linux-x64 --self-contained

# Publish for macOS
dotnet publish -c Release -r osx-x64 --self-contained
```

## Architecture Overview

### Service Layer Pattern
The codebase follows a clean service-based architecture where business logic is encapsulated in services under `src/NetContextServer/Services/`. Each service has a corresponding interface and handles a specific domain concern.

### MCP Tool Implementation
Tools are organized under `src/NetContextServer/Tools/` and grouped by functionality:
- **FileTools**: Project/file operations (`ListProjects`, `ListSourceFiles`, `ReadFile`)
- **SearchTools**: Text and semantic search (`SearchCode`, `SemanticSearch`)
- **PackageTools**: NuGet package analysis (`AnalyzePackages`)
- **CoverageTools**: Test coverage analysis (`GetCoverage`)
- **ThinkTools**: Structured reasoning (`Think`)

Each tool class inherits from `ModelContextTool<TArgs, TResult>` and implements the MCP protocol.

### Key Services and Their Responsibilities
- **FileService**: File system operations with security validation
- **CodeSearchService**: Text-based code search using regex
- **SemanticSearchService**: AI-powered semantic search with Azure OpenAI embeddings
- **PackageAnalyzerService**: NuGet dependency analysis and update recommendations
- **CoverageAnalysisService**: Multi-format coverage report parsing (Coverlet, LCOV, Cobertura)

### State Management
User patterns and preferences are persisted in `%LocalAppData%/NetContextServer/state.json` through the `StateService`.

### Security Model
- Path validation ensures operations stay within allowed directories
- File size limits prevent resource exhaustion
- Sensitive files (.env, secrets) are protected from access
- All file operations validate against these security constraints

## Testing Approach

Tests are organized using xUnit with fixture-based integration testing:
- **Unit Tests**: Test individual services in isolation
- **Integration Tests**: Use `NetContextServerFixture` for full tool testing
- **Test Utilities**: `TestOutputLogger` for debugging test execution

When adding new features:
1. Add unit tests for service logic
2. Add integration tests for MCP tool implementations
3. Use the existing fixture pattern for consistency

## Configuration and Environment

The server supports various configuration options through:
- Environment variables for Azure OpenAI integration
- Command-line arguments for server initialization
- Local state file for user preferences

When working with semantic search features, ensure Azure OpenAI credentials are configured in environment variables.