using AudioGuideAPI.Data;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoiController : ControllerBase
{
    private readonly AppDbContext _context;

    public PoiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var query = _context.Pois
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
        var poi = await _context.Pois
            .Include(x => x.Translations)
            .Include(x => x.TourStops)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (poi == null)
        {
            return NotFound();
        }

        return Ok(poi);
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
}
