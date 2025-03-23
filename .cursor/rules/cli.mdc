---
description: CLI Tool Implementation Rules
globs: **/Program.cs, **/CLI/**/*.cs, **/CommandLine/**/*.cs
---
# CLI Tool Implementation Rules

This rule provides guidance for implementing the Command Line Interface (CLI) for the .NET Codebase Context Server, with specific optimizations for Claude 3.7 Sonnet integration.

## AI-Assisted Development
- Use descriptive XML comments that explain the "why" behind implementation choices
- Include example usage for each command in code comments
- Structure commands into logical groups with consistent naming patterns
- Ensure each command has a clear, single responsibility
- Add CLI output examples in comments to help Claude understand expected behavior

## Command Structure
- Use verb-noun pattern for all commands (e.g., `list-projects`, `search-code`)
- Implement consistent parameter naming across similar commands
- Support both short and long option formats for all parameters
- Include help text that explains parameter relationships and constraints
- Group related commands into command sets with shared context

## Error Handling
- Return structured JSON responses with error codes and descriptive messages
- Include targeted troubleshooting guidance in error messages
- Implement detailed logging with contextual information
- Add correlation IDs to associate related operations for troubleshooting
- Handle graceful shutdown for CTRL+C and other interrupts

## User Experience
- Provide colorized, formatted output with clear visual hierarchy
- Implement progress indicators for long-running operations
- Support interactive mode for complex operations
- Add "did you mean?" suggestions for common command typos
- Include completion scripts for bash/zsh/PowerShell

## Documentation 
- Create example-driven documentation with realistic scenarios
- Document each command with purpose, parameters, examples, and limitations
- Include troubleshooting sections for common issues
- Add visual guides showing command relationships and workflows
- Provide explicit mapping between CLI commands and MCP protocol capabilities

## AI Integration Features
- Add machine-readable JSON output option for all commands
- Support template-based rendering of results for custom output formats
- Include metadata in command responses to aid in AI understanding
- Add descriptive response schemas for Claude to better understand outputs

## Security
- Validate all user inputs with clear error messages
- Implement proper access control for file operations
- Use least-privilege principles for all operations
- Never expose sensitive information in logs or output
- Support credential management with secure storage