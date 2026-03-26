using Microsoft.AspNetCore.Mvc;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudioController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public AudioController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("library")]
    public IActionResult Library()
    {
        var folder = Path.Combine(_env.WebRootPath, "audio");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var files = Directory.GetFiles(folder)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => new
            {
                fileName = name!,
                url = $"/audio/{name}"
            })
            .ToList();

        return Ok(files);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        var folder = Path.Combine(_env.WebRootPath, "audio");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(folder, safeName);

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        return Ok(new
        {
            file = safeName,
            url = $"/audio/{safeName}"
        });
    }
}
