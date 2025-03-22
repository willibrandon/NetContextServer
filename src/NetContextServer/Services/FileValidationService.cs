namespace NetContextServer.Services;

/// <summary>
/// Provides validation and path manipulation services for file system operations.
/// Ensures file access is restricted to the designated base directory for security.
/// </summary>
internal static class FileValidationService
{
    /// <summary>
    /// Gets or sets the base directory for all file operations.
    /// All file access must be within this directory for security.
    /// </summary>
    public static string BaseDirectory { get; private set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Sets the base directory for file operations.
    /// </summary>
    /// <param name="directory">The directory path to set as the base directory.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    public static void SetBaseDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        BaseDirectory = directory;
    }

    /// <summary>
    /// Determines whether a given path is safe to access by verifying it is within the base directory.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is within the base directory; otherwise, false.</returns>
    /// <remarks>
    /// A path is considered safe if:
    /// - It is not null or empty
    /// - Its full path starts with the base directory path
    /// - Case-insensitive comparison is used for Windows compatibility
    /// </remarks>
    public static bool IsPathSafe(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var fullPath = Path.GetFullPath(path);
        var basePath = Path.GetFullPath(BaseDirectory);
        return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the relative path of a file with respect to the base directory.
    /// </summary>
    /// <param name="fullPath">The full path to convert to a relative path.</param>
    /// <returns>The path relative to the base directory.</returns>
    public static string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(BaseDirectory, fullPath);
    }

    /// <summary>
    /// Ensures that the base directory has been set before performing file operations.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the base directory is not set.</exception>
    /// <remarks>
    /// This method should be called before any file operations that depend on the base directory.
    /// </remarks>
    public static void EnsureBaseDirectorySet()
    {
        if (string.IsNullOrEmpty(BaseDirectory))
        {
            throw new InvalidOperationException("Base directory not set. Use set_base_directory first.");
        }
    }
}
