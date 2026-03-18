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

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var folder = Path.Combine(_env.ContentRootPath, "uploads");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, file.FileName);

        using var stream = new FileStream(path, FileMode.Create);

        await file.CopyToAsync(stream);

        return Ok(new
        {
            file = file.FileName
        });
    }
}