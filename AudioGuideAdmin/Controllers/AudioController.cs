using AudioGuideAdmin.Helpers;
using Microsoft.AspNetCore.Mvc;

public class AudioController : Controller
{
    private readonly AudioStorageOptions _audioStorageOptions;

    public AudioController(AudioStorageOptions audioStorageOptions)
    {
        _audioStorageOptions = audioStorageOptions;
    }

    public IActionResult Upload()
    {
        var path = _audioStorageOptions.RootPath;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var files = Directory.GetFiles(path)
            .Select(f => new AudioFileItem(Path.GetFileName(f)!, $"/audio/{Path.GetFileName(f)}"))
            .ToList();

        return View(files);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            var path = _audioStorageOptions.RootPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(path, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        return RedirectToAction(nameof(Upload));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string fileName)
    {
        var path = ResolveSafePath(fileName, _audioStorageOptions.RootPath);

        if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return RedirectToAction(nameof(Upload));
    }

    private static string? ResolveSafePath(string? fileName, string rootPath)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var safeFileName = Path.GetFileName(fileName);
        var combinedPath = Path.GetFullPath(Path.Combine(rootPath, safeFileName));
        var normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return combinedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            ? combinedPath
            : null;
    }

    public record AudioFileItem(string FileName, string Url);
}
