---
description: .NET Codebase Context MCP Server Development Rules
globs: *.cs, *.csproj, *.sln
---
# .NET Codebase Context MCP Server Development Rules

This rule applies to all .NET code files in the project and provides guidance for implementing the MCP server with specific optimizations for Claude 3.7 Sonnet integration.

## Code Documentation for AI Assistance
- Include comprehensive XML documentation for all public and protected members
- Add code examples within XML comments for complex functionality
- Document "why" behind implementation decisions, not just "what" the code does
- Use consistent terminology across all documentation
- Include parameter validation logic in method documentation
- Specify explicit return value descriptions and possible exceptions
- Add class/interface relationship diagrams in code comments for complex hierarchies

## Code Organization
- Keep files under 300 lines when possible for better AI comprehension
- Use partial classes to logically organize larger components
- Follow consistent naming conventions for related patterns
- Group related functionality into clearly named namespaces
- Create dedicated classes for cross-cutting concerns
- Use explicit rather than implicit patterns to improve AI code understanding
- Prefer composition over inheritance for better AI reasoning

## Implementation Guidelines
- Prioritize readability over clever or overly compact code
- Implement one complete feature at a time
- Use strongly typed models for all data exchanges
- Create dedicated DTOs for MCP protocol interactions
- Add comprehensive input validation with descriptive error messages
- Implement self-explanatory function and variable names
- Avoid magic numbers and strings; use constants with descriptive names

## Testing Guidelines
- Write unit tests for all public APIs with descriptive test names
- Create integration tests for MCP protocol interactions
- Add performance tests for critical path operations
- Include contract tests for MCP protocol compliance
- Cover edge cases and failure scenarios in tests
- Document test coverage goals and gaps

## Semantic Search Optimization
- Document embedding generation strategy and parameters
- Implement configurable chunking strategies for different document types
- Add explicit tokenization examples for code files
- Create dedicated indexes for different code artifacts (methods, classes, etc.)
- Implement relevance scoring tuned for code understanding
- Include heuristics for determining code boundaries
- Add context preservation mechanisms between code chunks

## Performance Considerations
- Implement efficient large codebase scanning with incremental updates
- Use caching with clear invalidation strategies
- Add benchmarks for critical operations
- Implement background processing for long-running operations
- Add memory usage monitoring and adaptive throttling
- Include performance characteristics documentation
- Support incremental updates to minimize reprocessing

## Claude 3.7 Sonnet-Specific Guidelines
- Structure code to optimize for Claude's context window (200K tokens)
- Include focused example outputs for all tools
- Add explicit relationship maps between components
- Implement consistent state management with clear transitions
- Document all assumptions about AI capabilities and limitations
- Include failure recovery mechanisms for interrupted AI interactions
- Structure code to facilitate accurate citation by Claude