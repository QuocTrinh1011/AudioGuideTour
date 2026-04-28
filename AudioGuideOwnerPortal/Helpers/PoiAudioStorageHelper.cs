namespace AudioGuideOwnerPortal.Helpers;

public static class PoiAudioStorageHelper
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".m4a",
        ".aac",
        ".ogg"
    };

    public static async Task<string> SaveAudioAsync(IFormFile? audioFile, string? currentUrl, AudioStorageOptions options)
    {
        if (audioFile == null || audioFile.Length <= 0)
        {
            return currentUrl?.Trim() ?? string.Empty;
        }

        Directory.CreateDirectory(options.RootPath);

        var extension = Path.GetExtension(audioFile.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return currentUrl?.Trim() ?? string.Empty;
        }

        var fileName = $"owner-audio-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(options.RootPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await audioFile.CopyToAsync(stream);

        return $"/audio/{fileName}";
    }
}
