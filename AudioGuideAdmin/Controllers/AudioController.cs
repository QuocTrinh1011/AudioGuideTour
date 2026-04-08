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

    public IActionResult Delete(string fileName)
    {
        var path = Path.Combine(_audioStorageOptions.RootPath, fileName);

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return RedirectToAction(nameof(Upload));
    }

    public record AudioFileItem(string FileName, string Url);
}
