# NetContextServer Tool Reference

This document provides detailed documentation for all available tools in NetContextServer. Each tool is documented with its description, parameters, and usage examples.

## File Operations

### `list_files`
Lists all .NET source files in the specified project directory.

**Parameters:**
- `--project-path` (required): Absolute path to the project directory containing the .cs files

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-files --project-path "D:\Projects\MyApp\src\MyProject"
```

### `list_projects`
Scans the current solution and returns all .csproj files found.

**Parameters:** None

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-projects
```

### `list_projects_in_dir`
Searches a specific directory for .csproj files.

**Parameters:**
- `--directory` (required): Absolute path to the directory to search for .csproj files

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-projects-in-dir --directory "D:\Projects\MyApp\src"
```

### `list_solutions`
Returns all .sln files found in the base directory.

**Parameters:** None

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-solutions
```

### `list_source_files`
Lists all source files in a project directory.

**Parameters:**
- `--project-dir` (required): Absolute path to the project directory to scan for source files

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- list-source-files --project-dir "D:\Projects\MyApp\src\MyProject"
```

### `open_file`
Reads and returns the contents of a specified file.

**Parameters:**
- `--file-path` (required): Absolute path to the file to read

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- open-file --file-path "D:\Projects\MyApp\src\MyProject\Program.cs"
```

### `set_base_directory`
Sets the base directory for all file operations.

**Parameters:**
- `--directory` (required): Absolute path to set as the new base directory. Must be a valid, existing directory

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- set-base-dir --directory "D:\Projects\MyApp"
```

### `get_base_directory`
Returns the current base directory used for all file operations.

**Parameters:** None

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-base-dir
```

## Search Operations

### `search_code`
Performs a text-based search across all code files for the specified text.

**Parameters:**
- `--text` (required): The exact text string to search for in the codebase

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- search-code --text "public class Program"
```

### `semantic_search`
Performs a semantic similarity search across the codebase using AI.

**Parameters:**
- `--query` (required): Natural language description of the code you're looking for
- `--top` (optional): Number of results to return (default: 5)

**Example:**
```bash
# With default number of results
dotnet run --project src/NetContextClient/NetContextClient.csproj -- semantic-search --query "file handling operations"

# With custom number of results
dotnet run --project src/NetContextClient/NetContextClient.csproj -- semantic-search --query "error handling" --top 10
```

**Note:** Requires Azure OpenAI credentials to be set in environment variables:
- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_API_KEY`

## Package Analysis

### `analyze_packages`
Analyzes NuGet packages in all projects found in the base directory.

**Parameters:** 
- `--include-preview` (optional): Whether to include preview/prerelease versions in update recommendations. Default is false (only stable versions).

**Example:**
```bash
# Default - show only stable version updates
dotnet run --project src/NetContextClient/NetContextClient.csproj -- analyze-packages

# Include preview versions in update recommendations
dotnet run --project src/NetContextClient/NetContextClient.csproj -- analyze-packages --include-preview
```

**Output includes:**
- Package versions and available updates
- Usage analysis and detection of unused packages
- Security vulnerability warnings
- Recommendations for updates or removal
- Deep transitive dependency analysis
- Visual dependency graph representation with smart grouping
- Preview/prerelease version information (when requested or available)

**Status Indicators:**
- ✅ Up to date: Current version is the latest stable version
- 🔄 Update available: A newer stable version exists
- 🔆 Preview update available: When preview versions are included
- 🔅 Preview available: Indicates preview updates are available when not showing previews
- ⚠️ Unused: Package is not referenced in code

**Dependency Graph Features:**
- Hierarchical tree visualization in ASCII-art format
- Automatic grouping of related dependencies by namespace
- Color-coding of dependencies in the console:
  - Cyan: Leaf dependencies (end nodes)
  - Green: Intermediate dependencies
  - Yellow: Grouped namespaces
- Clear visual separation between dependency groups
- Configurable depth of transitive dependency resolution

**Example Output:**
```
Project: MyProject.csproj
  Found 3 package(s):
  - ✅ Newtonsoft.Json (13.0.1)
    Used in 5 location(s)

    Dependencies:
    └─ Newtonsoft.Json
       ├─ Microsoft.*
       │  └─ Microsoft.CSharp
       └─ System.*
          └─ System.ComponentModel

  - 🔄 Microsoft.Extensions.DependencyInjection (5.0.2 → 6.0.1)
    Update available: 6.0.1
    Used in 3 location(s)

    Dependencies:
    └─ Microsoft.Extensions.DependencyInjection
       └─ Microsoft.*
          └─ Microsoft.Extensions.DependencyInjection.Abstractions

  - ⚠️ Unused.Package (1.0.0)
    Consider removing this unused package
```

## Ignore Pattern Management

### `add_ignore_patterns`
Adds new patterns to the ignore list for file scanning operations.

**Parameters:**
- `--patterns` (required): Array of glob patterns to ignore (e.g. '*.generated.cs', 'bin/*')

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- add-ignore-patterns --patterns "*.generated.cs" "bin/*" "obj/*"
```

### `clear_ignore_patterns`
Removes all user-defined ignore patterns from both memory and the persistent state file.

**Parameters:** None

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- clear-ignore-patterns
```

### `get_ignore_patterns`
Retrieves the current list of active ignore patterns from memory.

**Parameters:** None

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-ignore-patterns
```

### `get_state_file_location`
Returns the absolute path to the ignore_patterns.json state file.

**Parameters:** None

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- get-state-file-location
```

### `remove_ignore_patterns`
Removes specific patterns from the ignore list.

**Parameters:**
- `--patterns` (required): Array of glob patterns to remove from the ignore list

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- remove-ignore-patterns --patterns "*.generated.cs" "bin/*"
```

## General Tools

### `hello`
Simple health check endpoint that returns a greeting message.

**Parameters:** None

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- hello
```

### `think`
Provides a space for structured thinking during complex operations, allowing AI models to reason about actions, verify compliance with rules, and plan next steps without making any state changes.

**Parameters:**
- `--thought` (required): The thought or reasoning to process

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- think --thought "Planning to refactor the authentication module to use JWT tokens"
```

**Output Example:**
```json
{
  "thought": "Planning to refactor the authentication module to use JWT tokens",
  "message": "Thought processed successfully",
  "category": "Refactoring",
  "timestamp": "2024-03-21T14:30:00.000Z",
  "characterCount": 58
}
```

**When to Use:**
The think tool is particularly valuable when you want Claude to:
1. Break down complex problems into manageable steps
2. Analyze tool outputs before taking further actions
3. Verify compliance with project policies and rules
4. Plan multi-step operations that require careful consideration
5. Document reasoning about architectural decisions

> 📚 **Learn More**: This implementation is based on Anthropic's research on improving Claude's performance with a dedicated thinking space. [Read their detailed blog post](https://www.anthropic.com/engineering/claude-think-tool) for more insights.

**Best Practice Examples:**

1. **Multi-step Task Planning**
```
Before implementing a new feature, use the think tool to:
- List all required functionality
- Identify affected components and files
- Plan the implementation sequence
- Consider potential edge cases
- Outline test scenarios

Example: When adding authentication, think through: user flow, security requirements, error states, and affected API endpoints.
```

2. **Policy Compliance Verification**
```
When evaluating solutions against project guidelines, use the think tool to:
- List all relevant policies
- Check each policy requirement against the solution
- Identify any compliance gaps
- Document justifications for approach
- Flag areas needing further review

Example: "Before implementing this database change, let me verify it meets our data security policies..."
```

3. **Tool Output Analysis**
```
After receiving complex tool outputs (like search results or code analysis), use the think tool to:
- Summarize key findings
- Identify patterns across results
- Connect information from different sources
- Determine next investigation steps
- Validate assumptions based on collected data

Example: "After searching the codebase for auth-related files, I've found these patterns..."
```

4. **Architectural Decision Documentation**
```
When making architectural choices, use the think tool to:
- Document decision criteria
- Compare alternative approaches
- List pros and cons of each option
- Justify the chosen solution
- Note implications for future development

Example: "Considering three approaches for the caching layer: in-memory, Redis, or database..."
```

**Features:**
- Automatic thought categorization:
  - Refactoring: Code restructuring and improvements
  - Security: Security-related considerations
  - Performance: Optimization and performance improvements
  - Testing: Testing and debugging thoughts
  - Architecture: Design and architectural decisions
  - General: Other uncategorized thoughts
- Detailed metadata including timestamps and character counts
- Content validation for potentially harmful patterns
- Support for Unicode characters and emoji
- Automatic log rotation for debugging logs

**Limitations and Considerations:**
- The tool is stateless - it doesn't persist thoughts between invocations
- No state changes are made to the codebase
- Maximum thought length is 32KB (32,768 characters)
- Potentially harmful content is automatically rejected
- Timestamps are in ISO 8601 format (UTC)
- Log files are automatically rotated at 5MB

**Logging Configuration:**
Enable thought logging by setting the environment variable:
```bash
# PowerShell
$env:NETCONTEXT_LOG_THOUGHTS="true"

# Bash
export NETCONTEXT_LOG_THOUGHTS="true"
```

Logs are stored in:
- Location: `[AppDirectory]/logs/thoughts.log`
- Format: `[Timestamp] JSON-formatted-thought-data`
- Rotation: Automatic at 5MB with timestamp-based archiving

**Integration with AI Workflows:**
When working with Claude, the think tool can be used to:
- Document decision-making processes
- Create structured plans for complex refactoring
- Validate approaches against project guidelines
- Break down large tasks into smaller, manageable steps
- Maintain a clear record of reasoning in the conversation history
- Track thought patterns through categorization

**Error Handling:**
The tool returns error responses in the following cases:
```json
{
  "error": "Missing required parameter 'thought'"
}
```
```json
{
  "error": "Error: Thought exceeds maximum length of 32768 characters"
}
```
```json
{
  "error": "Error: Thought contains invalid content"
}
```

## Default Ignore Patterns

The following patterns are ignored by default to protect sensitive information:
- `*.env` - Environment files
- `appsettings.*.json` - Application settings
- `*.pfx` - Certificate files
- `*.key` - Key files
- `*.pem` - PEM files
- `*password*` - Files containing "password" in the name
- `*secret*` - Files containing "secret" in the name

## Error Handling

The server provides clear error messages for common scenarios:
- Directory not found
- Access denied (outside base directory)
- Invalid patterns
- File size limits exceeded
- Restricted file types
- Missing environment variables for semantic search

## Coverage Analysis Tools

### `coverage-analysis`

Analyzes test coverage data from various formats and provides detailed insights.

```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- coverage-analysis --report-path <path> [--format <format>]
```

**Parameters:**
- `--report-path`: Path to the coverage report file
- `--format` (optional): Coverage report format. Supported values:
  - `coverlet-json` (default): Coverlet JSON format
  - `lcov`: LCOV format
  - `cobertura`: Cobertura XML format

**Output:**
Returns a list of coverage reports for each file, including:
- File path
- Coverage percentage
- List of uncovered lines
- Branch coverage data (where available)
- Recommendations for improving coverage

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- coverage-analysis --report-path "TestResults/coverage.json"
```

### `coverage-summary`

Generates a summary of test coverage across all files.

```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- coverage-summary --report-path <path> [--format <format>]
```

**Parameters:**
- `--report-path`: Path to the coverage report file
- `--format` (optional): Coverage report format (same as coverage-analysis)

**Output:**
Returns a summary object containing:
- Total number of files
- Overall coverage percentage
- Total number of uncovered lines
- List of files with coverage below threshold
- List of files with lowest coverage

**Example:**
```bash
dotnet run --project src/NetContextClient/NetContextClient.csproj -- coverage-summary --report-path "TestResults/coverage.json"
``` 