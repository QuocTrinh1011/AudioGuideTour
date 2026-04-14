using AudioGuideAPI.Data;
using AudioGuideAPI.DTOs;
using AudioGuideAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MapController : ControllerBase
{
    private readonly AppDbContext _context;

    public MapController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("nearby")]
    public async Task<IActionResult> GetNearby(LocationRequest request)
    {
        var pois = await _context.Pois
            .AsNoTracking()
            .Include(x => x.Translations.Where(t => t.IsPublished))
            .Where(x => x.IsActive)
            .ToListAsync();

        var result = pois
            .Select(p => new
            {
                poi = p,
                distance = GeoMath.DistanceInMeters(request.Latitude, request.Longitude, p.Latitude, p.Longitude),
                translation = PoiTranslationSelector.Select(p.Translations, request.Language)
            })
            .OrderBy(x => x.distance)
            .Take(20)
            .Select((x, index) => new
            {
                x.poi.Id,
                x.poi.Name,
                x.poi.Category,
                title = x.translation?.Title ?? x.poi.Name,
                summary = x.translation?.Summary ?? x.poi.Summary,
                description = x.translation?.Description ?? x.poi.Description,
                x.poi.Address,
                ttsScript = string.IsNullOrWhiteSpace(x.translation?.TtsScript) ? x.poi.TtsScript : x.translation.TtsScript,
                audioUrl = PoiAudioSelector.Resolve(x.poi, x.translation),
                x.poi.AudioMode,
                voiceName = x.translation?.VoiceName ?? string.Empty,
                language = x.translation?.Language ?? x.poi.DefaultLanguage,
                x.poi.Latitude,
                x.poi.Longitude,
                x.poi.ImageUrl,
                x.poi.MapUrl,
                x.poi.TriggerMode,
                x.poi.Priority,
                x.poi.CooldownSeconds,
                x.poi.DebounceSeconds,
                x.poi.Radius,
                x.poi.ApproachRadiusMeters,
                x.poi.EstimatedDurationSeconds,
                distanceMeters = Math.Round(x.distance, 2),
                isNearest = index == 0
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("feed")]
    public async Task<IActionResult> Feed([FromQuery] string language = "vi-VN")
    {
        var pois = await _context.Pois
            .AsNoTracking()
            .Include(x => x.Translations.Where(t => t.IsPublished))
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var data = pois.Select(p =>
        {
            var translation = PoiTranslationSelector.Select(p.Translations, language);
            return new
            {
                p.Id,
                p.Name,
                title = translation?.Title ?? p.Name,
                summary = translation?.Summary ?? p.Summary,
                p.Latitude,
                p.Longitude,
                p.Radius,
                p.ApproachRadiusMeters,
                p.Priority,
                p.ImageUrl,
                p.MapUrl
            };
        });

        return Ok(data);
    }
}
