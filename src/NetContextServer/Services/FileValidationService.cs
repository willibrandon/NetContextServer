namespace NetContextServer.Services;

internal static class FileValidationService
{
    public static string BaseDirectory { get; private set; } = Directory.GetCurrentDirectory();

    public static void SetBaseDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        BaseDirectory = directory;
    }

    public static bool IsPathSafe(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var fullPath = Path.GetFullPath(path);
        var basePath = Path.GetFullPath(BaseDirectory);
        return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(BaseDirectory, fullPath);
    }

    public static void EnsureBaseDirectorySet()
    {
        if (string.IsNullOrEmpty(BaseDirectory))
        {
            throw new InvalidOperationException("Base directory not set. Use set_base_directory first.");
        }
    }
}
