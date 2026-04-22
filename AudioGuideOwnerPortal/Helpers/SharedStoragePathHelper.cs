namespace AudioGuideOwnerPortal.Helpers;

public static class SharedStoragePathHelper
{
    public static string ResolveAudioRoot(IConfiguration configuration, string contentRootPath)
    {
        return ResolvePath(
            configuration["Storage:SharedAudioRoot"],
            contentRootPath,
            Path.Combine("..", "SharedStorage", "audio"));
    }

    public static string ResolveImageRoot(IConfiguration configuration, string contentRootPath)
    {
        return ResolvePath(
            configuration["Storage:SharedImageRoot"],
            contentRootPath,
            Path.Combine("..", "SharedStorage", "images"));
    }

    public static string ResolveDataFile(IConfiguration configuration, string contentRootPath)
    {
        return ResolvePath(
            configuration["Database:SqlitePath"],
            contentRootPath,
            Path.Combine("..", "SharedStorage", "data", "AudioGuide.db"));
    }

    public static string ResolveKeyRoot(IConfiguration configuration, string contentRootPath)
    {
        return ResolvePath(
            configuration["Storage:SharedKeyRoot"],
            contentRootPath,
            Path.Combine("..", "SharedStorage", "keys"));
    }

    private static string ResolvePath(string? configuredPath, string contentRootPath, string defaultRelativePath)
    {
        var resolved = string.IsNullOrWhiteSpace(configuredPath)
            ? defaultRelativePath
            : configuredPath;

        if (Path.IsPathRooted(resolved))
        {
            return Path.GetFullPath(resolved);
        }

        var workspaceResolved = TryResolveFromWorkspaceRoot(contentRootPath, resolved);
        if (!string.IsNullOrWhiteSpace(workspaceResolved))
        {
            return workspaceResolved;
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, resolved));
    }

    private static string? TryResolveFromWorkspaceRoot(string contentRootPath, string relativePath)
    {
        var normalizedRelative = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var marker = $"{Path.DirectorySeparatorChar}SharedStorage{Path.DirectorySeparatorChar}";
        var markerIndex = normalizedRelative.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var suffix = normalizedRelative[(markerIndex + marker.Length)..];
        for (var current = new DirectoryInfo(contentRootPath); current != null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "AudioGuideAPI")) &&
                Directory.Exists(Path.Combine(current.FullName, "AudioGuideAdmin")) &&
                Directory.Exists(Path.Combine(current.FullName, "SharedStorage")))
            {
                return Path.GetFullPath(Path.Combine(current.FullName, "SharedStorage", suffix));
            }
        }

        return null;
    }
}
