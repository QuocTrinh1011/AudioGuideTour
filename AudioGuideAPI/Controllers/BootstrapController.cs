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
            .ThenInclude(x => x!.Translations.Where(t => t.IsPublished))
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
                p.Category,
                title = translation?.Title ?? p.Name,
                summary = translation?.Summary ?? p.Summary,
                description = translation?.Description ?? p.Description,
                p.Address,
                ttsScript = string.IsNullOrWhiteSpace(translation?.TtsScript) ? p.TtsScript : translation.TtsScript,
                audioUrl = string.IsNullOrWhiteSpace(translation?.AudioUrl) ? p.AudioUrl : translation.AudioUrl,
                p.AudioMode,
                voiceName = translation?.VoiceName ?? string.Empty,
                language = translation?.Language ?? p.DefaultLanguage,
                p.ImageUrl,
                p.MapUrl,
                p.TriggerMode,
                p.Priority,
                p.Radius,
                p.ApproachRadiusMeters,
                p.CooldownSeconds,
                p.DebounceSeconds,
                p.EstimatedDurationSeconds,
                p.Latitude,
                p.Longitude
            };
        });

        var tourData = tours.Select(tour => new
        {
            tour.Id,
            tour.Name,
            tour.Description,
            tour.Language,
            tour.CoverImageUrl,
            tour.EstimatedDurationMinutes,
            stops = tour.Stops
                .OrderBy(s => s.SortOrder)
                .Select(stop =>
                {
                    var poi = stop.Poi;
                    object? mappedPoi = null;

                    if (poi != null)
                    {
                        var translation = poi.Translations.FirstOrDefault(t => t.Language == language)
                            ?? poi.Translations.FirstOrDefault(t => t.Language.StartsWith(language.Split('-')[0]))
                            ?? poi.Translations.FirstOrDefault();

                        mappedPoi = new
                        {
                            poi.Id,
                            poi.Name,
                            poi.Category,
                            title = translation?.Title ?? poi.Name,
                            summary = translation?.Summary ?? poi.Summary,
                            description = translation?.Description ?? poi.Description,
                            poi.Address,
                            ttsScript = string.IsNullOrWhiteSpace(translation?.TtsScript) ? poi.TtsScript : translation.TtsScript,
                            audioUrl = string.IsNullOrWhiteSpace(translation?.AudioUrl) ? poi.AudioUrl : translation.AudioUrl,
                            poi.AudioMode,
                            voiceName = translation?.VoiceName ?? string.Empty,
                            language = translation?.Language ?? poi.DefaultLanguage,
                            poi.ImageUrl,
                            poi.MapUrl,
                            poi.TriggerMode,
                            poi.Priority,
                            poi.Radius,
                            poi.ApproachRadiusMeters,
                            poi.CooldownSeconds,
                            poi.DebounceSeconds,
                            poi.EstimatedDurationSeconds,
                            poi.Latitude,
                            poi.Longitude
                        };
                    }

                    return new
                    {
                        stop.Id,
                        stop.TourId,
                        stop.PoiId,
                        stop.SortOrder,
                        stop.AutoPlay,
                        stop.Note,
                        poi = mappedPoi
                    };
                })
                .ToList()
        });

        return Ok(new
        {
            languages,
            categories,
            pois = poiData,
            tours = tourData,
            requestedLanguage = language
        });
    }
}
