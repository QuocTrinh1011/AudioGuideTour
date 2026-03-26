using AudioGuideAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BootstrapController : ControllerBase
{
    private readonly AppDbContext _context;

    public BootstrapController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string language = "vi-VN")
    {
        var languages = await _context.LanguageOptions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var pois = await _context.Pois
            .AsNoTracking()
            .Include(x => x.Translations.Where(t => t.IsPublished))
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var tours = await _context.Tours
            .AsNoTracking()
            .Include(x => x.Stops.OrderBy(s => s.SortOrder))
            .ThenInclude(x => x.Poi)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var poiData = pois.Select(p =>
        {
            var translation = p.Translations.FirstOrDefault(t => t.Language == language)
                ?? p.Translations.FirstOrDefault(t => t.Language.StartsWith(language.Split('-')[0]))
                ?? p.Translations.FirstOrDefault();

            return new
            {
                p.Id,
                p.Name,
                title = translation?.Title ?? p.Name,
                summary = translation?.Summary ?? p.Summary,
                description = translation?.Description ?? p.Description,
                ttsScript = string.IsNullOrWhiteSpace(translation?.TtsScript) ? p.TtsScript : translation.TtsScript,
                voiceName = translation?.VoiceName ?? string.Empty,
                language = translation?.Language ?? p.DefaultLanguage,
                p.ImageUrl,
                p.MapUrl,
                p.Priority,
                p.Radius,
                p.CooldownSeconds,
                p.DebounceSeconds,
                p.Latitude,
                p.Longitude
            };
        });

        return Ok(new
        {
            languages,
            categories,
            pois = poiData,
            tours,
            requestedLanguage = language
        });
    }
}
