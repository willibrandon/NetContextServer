namespace NetContextServer.Models;

/// <summary>
/// Represents a NuGet package reference in a .NET project.
/// </summary>
public class PackageReference
{
    /// <summary>
    /// Gets or sets the package identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the package reference.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the project file containing this package reference.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;
}