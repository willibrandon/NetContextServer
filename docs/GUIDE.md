Below is a **step-by-step, chunked** implementation guide for **NetContextServer** (a .NET Codebase Context MCP Server) that builds on the design document. Each chunk is crafted to help you (and an AI collaborator) stay focused on a single coherent set of tasks. At the end of each chunk, there is a **Verification & Testing** step followed by a suggested **Git Commit** milestone. When finished, you can safely move on to the next chunk.

---

# Chunk 1: Project Setup & Environment

## 1.1 Objective
Set up the .NET 9 project and initialize a repository structure so that further development tasks have a clean foundation.

### 1.2 Tasks

1. **Install and verify .NET 9 SDK**  
   - Confirm that you have .NET 9 installed:  
     ```bash
     dotnet --list-sdks
     ```
   - If it’s missing, install the appropriate SDK from Microsoft’s official site.

2. **Create the solution and initial projects**  
   - Initialize a new solution named `NetContextServer`:  
     ```bash
     mkdir NetContextServer
     cd NetContextServer
     dotnet new sln --name NetContextServer
     ```
   - Create a **library** project for core functionalities (`NetContextServer.Core`):  
     ```bash
     dotnet new classlib --name NetContextServer.Core
     dotnet sln add NetContextServer.Core
     ```
   - (Optional) Create a **console** project for a quick test harness (`NetContextServer.ConsoleHarness`). This is separate from the CLI/global tool you’ll later build, but helps early testing:  
     ```bash
     dotnet new console --name NetContextServer.ConsoleHarness
     dotnet sln add NetContextServer.ConsoleHarness
     ```

3. **Add references**  
   - If needed, reference the core library from the console harness:  
     ```bash
     dotnet add NetContextServer.ConsoleHarness reference NetContextServer.Core
     ```

4. **Decide on an MCP .NET library**  
   - Check out existing libraries such as [MCPSharp](https://www.reddit.com/r/dotnet/comments/1irrfl2/mcpsharp_a_net_library_that_helps_you_build_model/) or any official/updated library.  
   - For now, choose and add it as a NuGet dependency in `NetContextServer.Core` (or keep it for later if you plan to bring it in after initial indexing code).

### 1.3 Questions to Answer
- **Do I need specialized Roslyn references right away, or will I keep the MVP scanning naive?**  
  - Likely wait until you define your MVP scope. If you plan to do naive file scanning first, skip Roslyn references.  

- **Where do I plan to store my test files or sample solutions for integration tests?**  
  - Decide on a `Tests` folder or a sample `.NET` solution under a `SampleSolutions` folder.

### 1.4 Verification & Testing
- **Build & Run**: In the root, run:
  ```bash
  dotnet build
  ```
  Confirm the solution builds successfully.
- **Sanity Check**: Run the console harness (if created) to verify it prints something like “Hello World” or a simple message:
  ```bash
  dotnet run --project NetContextServer.ConsoleHarness
  ```

### 1.5 Git Commit
- **Commit Message**: “Initialize solution structure with NetContextServer.Core and basic console harness.”

---

# Chunk 2: Basic Project Indexer (MVP)

## 2.1 Objective
Implement a minimal file-based indexer that scans a .sln directory to list projects and `.cs` files. This sets the stage for exposing “context” about the code.

### 2.2 Tasks

1. **Create Indexer Class**  
   - In `NetContextServer.Core`, create a `ProjectIndexer` (or similarly named) class.  
   - It should have methods like:
     ```csharp
     public class ProjectIndex
     {
         public List<string> ProjectPaths { get; set; } = new();
         public Dictionary<string, List<string>> FilesByProject { get; set; } = new();
     }
     
     public static class ProjectIndexer
     {
         public static ProjectIndex BuildIndex(string solutionRoot)
         {
             // 1. Locate .sln
             // 2. Parse .sln for .csproj references (or just do Directory search).
             // 3. For each .csproj, find all .cs files.
             // 4. Return a populated ProjectIndex
         }
     }
     ```
   - For an MVP, you can:
     - Use `Directory.EnumerateFiles(solutionRoot, "*.sln", SearchOption.AllDirectories)` to find the `.sln`.
     - Similarly, find `*.csproj` files.  
     - For each `.csproj`, recursively list `*.cs` files.  

2. **Naive Implementation**  
   - You can skip advanced `.csproj` parsing for now. A simpler approach:
     - For each folder containing a `.csproj`, list all `.cs` files in that folder (and subfolders).
     - This ensures you gather a rough idea of the code layout.

3. **Add Unit Test for Indexer**  
   - If you prefer TDD, create a test project (e.g., `NetContextServer.Tests`):
     ```bash
     dotnet new xunit -n NetContextServer.Tests
     dotnet sln add NetContextServer.Tests
     dotnet add NetContextServer.Tests reference NetContextServer.Core
     ```
   - Write a test such as:
     ```csharp
     [Fact]
     public void BuildIndex_ReturnsProjectsAndFiles()
     {
         var index = ProjectIndexer.BuildIndex("/path/to/SampleSolution");
         
         Assert.NotEmpty(index.ProjectPaths);
         Assert.True(index.FilesByProject.Count > 0);
         // etc.
     }
     ```

4. **Decide on Synchronous or Asynchronous**  
   - For now, synchronous file IO might be simpler. Asynchronous (`async/await`) can be introduced later if needed.

### 2.3 Questions to Answer
- **Do I need more robust parsing of .csproj**?  
  - Possibly later. The current approach should suffice for listing code files.  
- **Do I need to handle edge cases** (like multiple solutions, or solutions with F# / VB projects)?  
  - MVP can ignore those if desired.

### 2.4 Verification & Testing
- **Run Tests**:
  ```bash
  dotnet test
  ```
  Confirm that your new test passes and that the indexer correctly finds `.csproj` and `.cs` files in a sample solution.

### 2.5 Git Commit
- **Commit Message**: “Implement basic ProjectIndexer with test coverage for scanning .csproj and .cs files.”

---

# Chunk 3: MCP Tools & Library Integration

## 3.1 Objective
Integrate the MCP .NET library (e.g. MCPSharp) and expose your indexer data as simple “tools” (e.g., `ListProjects`, `ListFiles`).

### 3.2 Tasks

1. **Install MCP .NET Library**  
   - For example:
     ```bash
     cd NetContextServer.Core
     dotnet add package MCPSharp --version x.y.z
     ```
   - (Adjust for whichever library you choose.)

2. **Create a Tools Class**  
   - A typical pattern in these libraries is to annotate methods with `[McpTool("ToolName")]`.  
   - Example:
     ```csharp
     using MCPSharp;

     public class ProjectTools
     {
         private readonly ProjectIndex _index;

         public ProjectTools(ProjectIndex index)
         {
             _index = index;
         }

         [McpTool("ListProjects")]
         public List<string> ListProjects()
         {
             return _index.ProjectPaths;
         }

         [McpTool("ListFiles")]
         public List<string> ListFiles(string projectPath)
         {
             return _index.FilesByProject.TryGetValue(projectPath, out var files)
                 ? files
                 : new List<string>();
         }
     }
     ```
   - You may create multiple classes (e.g., `ProjectTools`, `FileTools`) or keep them in one class.  

3. **Server Skeleton**  
   - Add a `DotNetMcpServer` class that:
     - Builds the index: `var index = ProjectIndexer.BuildIndex(solutionPath);`
     - Creates tool objects with the index: `var tools = new ProjectTools(index);`
     - Registers them in an MCP server instance:
       ```csharp
       var server = new McpServer();
       server.RegisterTools(tools);
       server.RunStdio(); // or RunTcp(port)
       ```
   - The specifics depend on the chosen MCP library’s API.

4. **Local Test Harness**  
   - In `NetContextServer.ConsoleHarness`, you can do:
     ```csharp
     static void Main()
     {
         var server = new DotNetMcpServer("path/to/MySolution");
         server.Start(); // Blocks listening for requests on STDIO
     }
     ```
   - You can test by manually typing JSON requests in a console or by using a sample MCP client script if provided by the library.

### 3.3 Questions to Answer
- **How do I want the server to communicate**: STDIO vs. WebSocket?  
  - For local usage with AI tools like Claude Desktop, STDIO might suffice. If you want remote usage, implement WebSocket or HTTP.
- **Do I want to add more tools**?  
  - Possibly: `SearchCode`, `OpenFile`, etc. That’s up next.

### 3.4 Verification & Testing
- **Manual Test**: If the MCP library provides a test client, run the server, then from the client:
  ```json
  {
    "tool": "ListProjects",
    "args": []
  }
  ```
  Check if you receive a correct JSON response listing the projects.  

### 3.5 Git Commit
- **Commit Message**: “Integrate MCP library and add ProjectTools (ListProjects, ListFiles) with server skeleton.”

---

# Chunk 4: Additional Tools (SearchCode, OpenFile, GetDocForSymbol)

## 4.1 Objective
Implement more advanced (yet still MVP-friendly) functions so the AI can retrieve code content and doc comments.

### 4.2 Tasks

1. **SearchCode Tool**  
   - Input: A search keyword (string).  
   - Output: A list of `(filePath, lineNumber, lineText)` hits.  
   - Implementation detail:
     ```csharp
     [McpTool("SearchCode")]
     public List<CodeSearchResult> SearchCode(string keyword)
     {
         var results = new List<CodeSearchResult>();
         foreach (var kvp in _index.FilesByProject)
         {
             foreach (var file in kvp.Value)
             {
                 var lines = File.ReadAllLines(file);
                 for(int i=0; i<lines.Length; i++)
                 {
                     if(lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                     {
                         results.Add(new CodeSearchResult 
                         { 
                             FilePath = file, 
                             LineNumber = i+1, 
                             LineText = lines[i]
                         });
                     }
                 }
             }
         }
         return results;
     }
     ```

2. **OpenFile Tool**  
   - Input: `filePath`  
   - Output: File content (string). Possibly truncated if it’s huge.  
   ```csharp
   [McpTool("OpenFile")]
   public string OpenFile(string filePath)
   {
       // Basic safety check
       if(!File.Exists(filePath)) 
           throw new FileNotFoundException(filePath);
       
       // Optional: limit size
       var content = File.ReadAllText(filePath);
       if(content.Length > 100_000)
       {
           content = content.Substring(0,100_000) + "\n... [Truncated]";
       }
       return content;
   }
   ```

3. **GetDocForSymbol Tool** (Optional MVP)  
   - Input: `symbol` (e.g. “MyNamespace.AuthService”)  
   - Output: The doc comments or relevant summary.  
   - Naive approach: Search lines containing `///` above the class/method.  
   - Roslyn-based approach (more advanced): `SemanticModel` or `Symbol` APIs. You may skip for now or stub it.

### 4.3 Questions to Answer
- **Truncation**: How do you handle giant files?  
  - Decide a max size. Possibly provide an offset/limit approach for partial reading in advanced usage.
- **Case Sensitivity**: For searching code, use `StringComparison.OrdinalIgnoreCase` to be more user-friendly.

### 4.4 Verification & Testing
- **Unit Tests**: Test `SearchCode` with sample input and confirm you get correct line references.
- **Integration Tests**: Start the server, call the new tools via an MCP client or a small script.  
- **Check Edge Cases**: 
  - Searching for an empty string.  
  - Opening a non-existent file.  
  - Searching for a keyword not found in any file.

### 4.5 Git Commit
- **Commit Message**: “Add SearchCode, OpenFile, and optional GetDocForSymbol tools with associated tests.”

---

# Chunk 5: CLI & Global Tool Integration

## 5.1 Objective
Create the actual CLI tool that end users will install and run as `dotnet tool`.

### 5.2 Tasks

1. **New Project for CLI**  
   - In the solution root:
     ```bash
     dotnet new console --name NetContextServer.Tool
     dotnet sln add NetContextServer.Tool
     dotnet add NetContextServer.Tool reference NetContextServer.Core
     ```
2. **Implement CLI Argument Parsing**  
   - For example, in `Program.cs`:
     ```csharp
     static async Task Main(string[] args)
     {
         var (solutionPath, port) = ParseArgs(args);
         var server = new DotNetMcpServer(solutionPath);
         Console.WriteLine($"Starting NetContextServer for solution: {solutionPath}");
         await server.StartStdioAsync(); 
     }
     ```
   - A minimal `ParseArgs(args)` function can read flags like `--solution "MySolution.sln"` or `--port 5555`.  

3. **Package as a DotNet Tool**  
   - In `NetContextServer.Tool.csproj`, include:
     ```xml
     <PropertyGroup>
       <OutputType>Exe</OutputType>
       <PackAsTool>true</PackAsTool>
       <ToolCommandName>net-context-server</ToolCommandName>
       <PackageOutputPath>./nupkg</PackageOutputPath>
     </PropertyGroup>
     ```
4. **Build & Pack**  
   ```bash
   dotnet pack NetContextServer.Tool --configuration Release
   ```
   This produces a `.nupkg` file in the specified directory.

5. **Test Local Installation**  
   ```bash
   dotnet tool install --global --add-source ./nupkg NetContextServer.Tool
   net-context-server --solution "path/to/MySolution.sln"
   ```
   Confirm the server starts.

### 5.3 Questions to Answer
- **Should the default transport be STDIO or a local port**?  
  - Decide based on your AI client usage. STDIO is often enough for local integration with tools like Claude Desktop.

### 5.4 Verification & Testing
- **Run the installed tool**: Ensure it prints a startup message.  
- **Basic MCP calls**: Use a small script or client to verify you can call `ListProjects`.

### 5.5 Git Commit
- **Commit Message**: “Create NetContextServer.Tool, enable .NET global tool packaging, test local install.”

---

# Chunk 6: Security Defaults & Access Controls

## 6.1 Objective
Refine the server to avoid accidental exposure of sensitive data. Ensure the user can configure what files/folders the server can read.

### 6.2 Tasks

1. **Directory Whitelisting**  
   - Modify the indexing and file retrieval to only permit files under a specified root.  
   - For example, store a `_rootPath` in the server or tool classes. If the requested `filePath` is outside `_rootPath`, reject the call.

2. **Ignore Patterns**  
   - Add a config option or code snippet that ignores certain patterns: e.g. `appsettings.*.json`, `.env`, `*.pfx`.  
   - Users can override them in advanced config if they want.  

3. **Logging / Auditing (Optional)**  
   - Add minimal logging to the console or a log file: e.g., “Tool invoked: `OpenFile` with param=xyz.cs”.

### 6.3 Questions to Answer
- **How do I handle environment variables**?  
  - Typically, they shouldn’t be included in code retrieval. If environment secrets exist in code, consider disclaimers.
- **Is an authentication layer needed**?  
  - MVP might not require it if it’s strictly local, but for remote usage, you’d want some form of auth (e.g., token-based).

### 6.4 Verification & Testing
- **Attempt to open an off-limit file**: Confirm the server throws an error or denies access.  
- **Check logs**: If logging is implemented, verify request data is captured.

### 6.5 Git Commit
- **Commit Message**: “Add security defaults (directory whitelisting, ignore patterns) and test restricted file access.”

---

# Chunk 7: Testing & QA

## 7.1 Objective
Ensure the overall solution works reliably across typical usage scenarios and platforms.

### 7.2 Tasks

1. **Unit Test Coverage**  
   - Confirm each tool has at least minimal coverage (e.g., `SearchCode_EmptyResultIfNoMatch`).  
   - Add edge-case tests: extremely large files, missing solutions, invalid arguments, etc.

2. **Integration Tests**  
   - Create a small script or a test harness to:
     1. Launch the server.
     2. Send MCP messages for each tool.
     3. Validate the JSON responses (e.g., confirm “ListProjects” returns the expected set).

3. **Cross-Platform**  
   - Test on Windows, Linux, macOS if possible.  
   - Confirm file path separators, case sensitivity, and newline differences do not break anything.

4. **Performance Checks (Basic)**  
   - If you have a large sample solution, measure indexing time.  
   - Possibly set a warning if scanning takes too long or if the codebase is huge.

### 7.3 Questions to Answer
- **Are we satisfied with test coverage or do we want to adopt a code coverage tool** (like `coverlet`)?  
- **Are we missing any critical user scenario** (e.g., solution with multiple `.sln` files)?

### 7.4 Verification & Testing
- **Run all tests**:
  ```bash
  dotnet test
  ```
- **Manual End-to-End**: Start the CLI tool with a sample solution. From an AI client (e.g., Claude), see if you can do real queries.

### 7.5 Git Commit
- **Commit Message**: “Add robust test coverage and verified cross-platform integration.”

---

# Chunk 8: Packaging & Release Preparation

## 8.1 Objective
Finalize the solution’s distribution, versioning, and release artifacts.

### 8.2 Tasks

1. **Versioning**  
   - In the `.csproj` for the CLI tool, set a `<Version>` element. E.g. `<Version>1.0.0</Version>` for an MVP release.

2. **NuGet Packaging**  
   - Revisit the `.NET` tool packaging instructions. Confirm `PackAsTool` is set to `true`.  
   - Optionally publish to NuGet.org (requires you to register an account and get an API key).  
     ```bash
     dotnet pack --configuration Release
     dotnet nuget push <nupkgFile> --api-key <YourKey> --source https://api.nuget.org/v3/index.json
     ```

3. **Licensing**  
   - Add a license file (MIT or Apache 2.0, etc.) in the root of the repo.  
   - Update the `<PackageLicenseExpression>` in `.csproj` if you want the license recognized by NuGet directly.  

4. **Readme & Documentation**  
   - Provide a quickstart in `README.md`: 
     1. `dotnet tool install --global NetContextServer.Tool --version 1.0.0`  
     2. `net-context-server --solution "MySolution.sln"`  
   - Mention usage with a sample AI client or a script snippet.

### 8.3 Questions to Answer
- **Will I need a separate distribution for the library** (`NetContextServer.Core`)?  
  - If yes, also push that library as a NuGet package for advanced integration.

### 8.4 Verification & Testing
- **Local Install**: 
  ```bash
  dotnet tool install --global --add-source .\nupkg NetContextServer.Tool
  net-context-server --solution "MySolution.sln"
  ```
  Confirm it works after the final packaging.

### 8.5 Git Commit
- **Commit Message**: “Prepare v1.0.0 release: finalize NuGet packaging, license, documentation.”

---

# Chunk 9: Documentation & Usage Examples

## 9.1 Objective
Provide clear “how-to” docs and advanced usage examples to ensure new users quickly see the value.

### 9.2 Tasks

1. **Extended README**  
   - Document each MCP tool:
     - **ListProjects**: returns array of `.csproj` paths.  
     - **ListFiles**: returns array of `.cs` files for a given project.  
     - **OpenFile**: returns file contents as a string.  
     - **SearchCode**: returns matched lines.  
   - Show sample JSON requests/responses.

2. **Video/GIF Demos** (Optional)  
   - Record a quick CLI session or an AI conversation demonstrating code queries. Post it on the GitHub repo or a blog.

3. **FAQs**  
   - “Why is file content truncated?”  
   - “How do I ignore certain files?”  
   - “Can I connect this to Visual Studio Code / DevOps?”

### 9.3 Questions to Answer
- **Do I want to create a small website** (e.g., GitHub Pages) for the project?  
  - If so, consider that in your final promotional push.

### 9.4 Verification & Testing
- **Review Docs**: Are all tools clearly documented?  
- **Peer Review**: Ask a colleague or friend to follow the README from scratch to confirm clarity.

### 9.5 Git Commit
- **Commit Message**: “Add detailed documentation, usage examples, and FAQ to README.”

---

# Chunk 10: Final Verification & Launch

## 10.1 Objective
Perform a final review and decide if you’re ready to publish v1.0.0 to users.

### 10.2 Tasks

1. **Quality Gate**  
   - Ensure all tests pass.  
   - Confirm the code is properly licensed and the version is correct.  

2. **Publish**  
   - If everything is final, push the tool to NuGet.org (or your private feed).  
   - Optionally create a **GitHub Release** with a release tag `v1.0.0`.

3. **Announcement / Promotion**  
   - Post on `r/dotnet`, relevant developer Slack/Discord channels, or Hacker News.  
   - Include a short message describing the features and linking to your GitHub.

### 10.3 Questions to Answer
- **Monetization or open-core**: Confirm your initial approach. Possibly keep everything free for now and watch for adoption.  

### 10.4 Verification & Testing
- **Install from NuGet** on a fresh machine to confirm the process works smoothly:
  ```bash
  dotnet tool install --global NetContextServer.Tool --version 1.0.0
  net-context-server --solution "AnotherSolution.sln"
  ```

### 10.5 Git Commit
- **Commit Message**: “Finalize v1.0.0 – official release.”

---

## Conclusion

By following these **10 logical chunks**, you’ll have:
1. A **clean, incremental** path from initialization to a fully functional .NET MCP server.
2. Clear points at which to **verify functionality and commit** your work.
3. A well-documented, easily installable tool that other developers can adopt.

Each chunk isolates a set of changes and ends with **testing** and a **commit** step—ensuring stable, manageable progress throughout development. This approach helps prevent scope creep and “wandering” during collaboration with an AI or any other developer. 

**Next steps**: Proceed chunk by chunk, always verifying your success before moving on. Once you reach the final chunk, you’ll have a fully working NetContextServer MVP for .NET codebases—ready to connect to AI assistants through the Model Context Protocol.