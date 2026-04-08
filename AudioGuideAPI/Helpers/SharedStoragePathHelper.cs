namespace AudioGuideAPI.Helpers;

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

        return Path.IsPathRooted(resolved)
            ? resolved
            : Path.GetFullPath(Path.Combine(contentRootPath, resolved));
    }
}
