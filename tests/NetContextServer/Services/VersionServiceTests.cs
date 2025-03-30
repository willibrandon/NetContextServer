using NetContextServer.Models;
using NetContextServer.Services;
using System.Reflection;
using Xunit;

namespace NetContextServer.Tests.Services;

public class VersionServiceTests
{
    [Fact]
    public void GetVersionInfo_ReturnsCorrectInformation()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null);
        Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);

        // Act
        var versionInfo = VersionService.GetVersionInfo();

        // Assert
        Assert.NotNull(versionInfo);
        Assert.NotNull(versionInfo.Version);
        Assert.Equal(Environment.Version.ToString(), versionInfo.RuntimeVersion);
        Assert.Equal(Environment.OSVersion.ToString(), versionInfo.OperatingSystem);
        Assert.False(versionInfo.SemanticSearchEnabled);
        Assert.NotNull(versionInfo.ActiveConfiguration);
        Assert.Contains("BaseDirectory", versionInfo.ActiveConfiguration.Keys);
        Assert.Contains("HasIgnorePatterns", versionInfo.ActiveConfiguration.Keys);
        Assert.Contains("StateFileLocation", versionInfo.ActiveConfiguration.Keys);
    }

    [Fact]
    public void GetVersionInfo_DetectsSemanticSearchConfiguration()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://test.openai.azure.com");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", "test-key");

        try
        {
            // Act
            var versionInfo = VersionService.GetVersionInfo();

            // Assert
            Assert.True(versionInfo.SemanticSearchEnabled);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public void VersionInfo_InitializesWithCorrectDefaults()
    {
        // Act
        var versionInfo = new VersionInfo();

        // Assert
        Assert.NotNull(versionInfo.Version);
        Assert.NotEqual("unknown", versionInfo.Version);
        Assert.NotEmpty(versionInfo.RuntimeVersion);
        Assert.NotEmpty(versionInfo.OperatingSystem);
        Assert.NotNull(versionInfo.ActiveConfiguration);
        Assert.Empty(versionInfo.ActiveConfiguration);
    }
} 