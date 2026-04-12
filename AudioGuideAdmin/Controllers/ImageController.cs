using AudioGuideAdmin.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuideAdmin.Controllers;

public class ImageController : Controller
{
    private readonly ImageStorageOptions _imageStorageOptions;

    public ImageController(ImageStorageOptions imageStorageOptions)
    {
        _imageStorageOptions = imageStorageOptions;
    }

    public IActionResult Upload()
    {
        var path = _imageStorageOptions.RootPath;
        Directory.CreateDirectory(path);

        var files = Directory.GetFiles(path)
            .Select(f => new ImageFileItem(Path.GetFileName(f)!, $"/images/{Path.GetFileName(f)}"))
            .OrderBy(x => x.FileName)
            .ToList();

        return View(files);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            Directory.CreateDirectory(_imageStorageOptions.RootPath);
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_imageStorageOptions.RootPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        return RedirectToAction(nameof(Upload));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string fileName)
    {
        var path = ResolveSafePath(fileName, _imageStorageOptions.RootPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            return RedirectToAction(nameof(Upload));
        }

        if (System.IO.File.Exists(path))
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

    public record ImageFileItem(string FileName, string Url);
}
