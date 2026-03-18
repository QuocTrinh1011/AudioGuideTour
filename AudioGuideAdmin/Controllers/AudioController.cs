using Microsoft.AspNetCore.Mvc;

public class AudioController : Controller
{
    private readonly IWebHostEnvironment _env;

    public AudioController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Upload()
    {
        var path = Path.Combine(_env.WebRootPath, "audio");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var files = Directory.GetFiles(path)
            .Select(f => Path.GetFileName(f))
            .ToList();

        return View(files);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            var path = Path.Combine(_env.WebRootPath, "audio");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filePath = Path.Combine(path, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }

        return RedirectToAction("Upload");
    }

    public IActionResult Delete(string fileName)
    {
        var path = Path.Combine(_env.WebRootPath, "audio", fileName);

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return RedirectToAction("Upload");
    }
}