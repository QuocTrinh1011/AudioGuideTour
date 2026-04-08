using AudioGuideAPI.Data;
using AudioGuideAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QRCodeController : ControllerBase
{
    private readonly AppDbContext _context;

    public QRCodeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, [FromQuery] string language = "vi-VN")
    {
        var qr = await _context.QRCodes
            .AsNoTracking()
            .Include(x => x.Poi)
            .ThenInclude(x => x!.Translations)
            .FirstOrDefaultAsync(x => x.Code == code);

        if (qr == null)
        {
            return NotFound();
        }

        var poi = qr.Poi;
        if (poi == null)
        {
            return NotFound();
        }

        var translation = PoiTranslationSelector.Select(poi.Translations, language);

        return Ok(new
        {
            qr.Id,
            qr.Code,
            qr.Note,
            poi = new
            {
                poi.Id,
                poi.Name,
                poi.Category,
                title = translation?.Title ?? poi.Name,
                summary = translation?.Summary ?? poi.Summary,
                description = translation?.Description ?? poi.Description,
                poi.Address,
                ttsScript = string.IsNullOrWhiteSpace(translation?.TtsScript) ? poi.TtsScript : translation?.TtsScript,
                audioUrl = string.IsNullOrWhiteSpace(translation?.AudioUrl) ? poi.AudioUrl : translation?.AudioUrl,
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
            }
        });
    }
}
