using MCPSharp;

await MCPServer.StartAsync("TestServer", "1.0");


namespace MCPSharp.Example
{
    ///<summary>testing interface for custom .net mcp server</summary>
    [McpTool]
    public class MCPDev()
    {
        ///<summary>just returns a message for testing.</summary>
        [McpFunction("hello", "hello description")]
        public static string Hello() => "hello, claude.";

        ///<summary>returns ths input string back</summary>
        ///<param name="input">the string to echo</param>
        [McpFunction("echo", "echo description")]
        public static string Echo([McpParameter(true)] string input) => input;

        ///<summary>Add Two Numbers</summary>
        ///<param name="a">first number</param>
        ///<param name="b">second number</param>
        [McpFunction("add", "add description")]
        public static string Add(int a, int b) => (a + b).ToString();


        /// <summary>
        /// Adds a complex object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [McpFunction("add_complex", "add_complex description")]
        public static string AddComplex(ComplicatedObject obj) => $"Name: {obj.Name}, Age: {obj.Age}, Hobbies: {string.Join(", ", obj.Hobbies)}";

        /// <summary>
        /// throws an exception - for ensuring we handle them gracefully
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [McpFunction("throw_exception", "throw_exception description")]
        public static string Exception() => throw new Exception("This is an exception");
    }

    /// <summary>
    /// A complicated object
    /// </summary>
    public class ComplicatedObject()
    {
        /// <summary>The name of the object</summary>
        public string Name { get; set; } = "";
        /// <summary>The age of the object</summary>
        public int Age { get; set; } = 0;
        /// <summary>The hobbies of the object</summary>
        public string[] Hobbies { get; set; } = [];
    }
}