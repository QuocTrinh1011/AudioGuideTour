using AudioGuideAPI.Data;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Helpers;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ImageStorageOptions _imageStorageOptions;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public PoiController(AppDbContext context, ImageStorageOptions imageStorageOptions)
    {
        _context = context;
        _imageStorageOptions = imageStorageOptions;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var query = ApiPoiScopeHelper.GetScopedPoiQuery(_context)
            .AsNoTracking()
            .Include(x => x.Translations)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var pois = await query
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(pois);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var poi = await ApiPoiScopeHelper.GetScopedPoiQuery(_context)
            .Include(x => x.Translations)
            .Include(x => x.TourStops)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (poi == null)
        {
            return NotFound();
        }

        return Ok(poi);
    }

    [HttpGet("{id}/image")]
    public async Task<IActionResult> GetImage(int id)
    {
        var poi = await ApiPoiScopeHelper.GetScopedPoiQuery(_context)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Name, x.ImageUrl })
            .FirstOrDefaultAsync();

        if (poi == null || string.IsNullOrWhiteSpace(poi.ImageUrl))
        {
            return NotFound();
        }

        return BuildImageResult(poi.ImageUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Poi poi)
    {
        poi.AudioMode = string.IsNullOrWhiteSpace(poi.AudioUrl) ? "tts" : "tts-fallback";
        poi.CreatedAt = DateTime.UtcNow;
        poi.UpdatedAt = DateTime.UtcNow;

        _context.Pois.Add(poi);
        await _context.SaveChangesAsync();

        return Ok(poi);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Poi poi)
    {
        var existing = await _context.Pois.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = poi.Name;
        existing.Category = poi.Category;
        existing.Summary = poi.Summary;
        existing.Description = poi.Description;
        existing.Address = poi.Address;
        existing.Latitude = poi.Latitude;
        existing.Longitude = poi.Longitude;
        existing.Radius = poi.Radius;
        existing.ApproachRadiusMeters = poi.ApproachRadiusMeters;
        existing.Priority = poi.Priority;
        existing.DebounceSeconds = poi.DebounceSeconds;
        existing.CooldownSeconds = poi.CooldownSeconds;
        existing.TriggerMode = poi.TriggerMode;
        existing.ImageUrl = poi.ImageUrl;
        existing.MapUrl = poi.MapUrl;
        existing.IsActive = poi.IsActive;
        existing.AudioMode = string.IsNullOrWhiteSpace(poi.AudioUrl) ? "tts" : "tts-fallback";
        existing.AudioUrl = poi.AudioUrl;
        existing.TtsScript = poi.TtsScript;
        existing.DefaultLanguage = poi.DefaultLanguage;
        existing.EstimatedDurationSeconds = poi.EstimatedDurationSeconds;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var poi = await _context.Pois.FindAsync(id);
        if (poi == null)
        {
            return NotFound();
        }

        _context.Pois.Remove(poi);
        await _context.SaveChangesAsync();
        return Ok();
    }

    private IActionResult BuildImageResult(string imageUrl)
    {
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             absolute.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return Redirect(absolute.ToString());
        }

        var fileName = Path.GetFileName(imageUrl.Trim());
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return NotFound();
        }

        var physicalPath = Path.Combine(_imageStorageOptions.RootPath, fileName);
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(physicalPath, contentType, enableRangeProcessing: true);
    }
}
