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
                translation = p.Translations.FirstOrDefault(t => t.Language == request.Language)
                    ?? p.Translations.FirstOrDefault()
            })
            .OrderBy(x => x.distance)
            .Take(20)
            .Select((x, index) => new
            {
                x.poi.Id,
                x.poi.Name,
                title = x.translation?.Title ?? x.poi.Name,
                summary = x.translation?.Summary ?? x.poi.Summary,
                x.poi.Latitude,
                x.poi.Longitude,
                x.poi.ImageUrl,
                x.poi.MapUrl,
                x.poi.Priority,
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
            var translation = p.Translations.FirstOrDefault(t => t.Language == language) ?? p.Translations.FirstOrDefault();
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
