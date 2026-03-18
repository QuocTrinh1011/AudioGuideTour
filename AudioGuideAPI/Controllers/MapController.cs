using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Data;
using AudioGuideAPI.DTOs;

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
        var pois = await _context.Pois.ToListAsync();

        var result = pois
            .Select(p => new
            {
                Poi = p,
                Distance = GetDistance(
                    request.Latitude,
                    request.Longitude,
                    p.Latitude,
                    p.Longitude)
            })
            .OrderBy(x => x.Distance)
            .Take(10);

        return Ok(result);
    }

    private double GetDistance(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        double R = 6371000;

        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) *
            Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}