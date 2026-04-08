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

    public IActionResult Delete(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return RedirectToAction(nameof(Upload));
        }

        var path = Path.Combine(_imageStorageOptions.RootPath, Path.GetFileName(fileName));
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return RedirectToAction(nameof(Upload));
    }

    public record ImageFileItem(string FileName, string Url);
}
