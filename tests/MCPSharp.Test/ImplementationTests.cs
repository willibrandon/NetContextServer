namespace MCPSharp.Test;

public class ImplementationTests
{
    private readonly static MCPClient client = new("Test Client", "1.0.0", "MCPSharp.Example.exe");

    [Fact]
    public async Task Test_ListTools()
    {
        var tools = await client.GetToolsAsync();

        Assert.NotEmpty(tools);
        Assert.True(tools.Count > 0);

        tools.ForEach(tool =>
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
        });
    }
}
