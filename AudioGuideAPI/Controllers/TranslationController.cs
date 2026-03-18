using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Data;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslationController : ControllerBase
{
    private readonly AppDbContext _context;

    public TranslationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{poiId}/{lang}")]
    public async Task<IActionResult> Get(int poiId, string lang)
    {
        var result = await _context.PoiTranslations
            .FirstOrDefaultAsync(x => x.PoiId == poiId && x.Language == lang);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}