using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Data;
using AudioGuideAPI.DTOs;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeofenceController : ControllerBase
{
    private readonly AppDbContext _context;

    public GeofenceController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CheckLocation(LocationRequest request)
    {
        var pois = await _context.Pois.ToListAsync();

        foreach (var poi in pois)
        {
            var distance = GetDistance(
                request.Latitude,
                request.Longitude,
                poi.Latitude,
                poi.Longitude);

            if (distance <= poi.Radius)
            {
                return Ok(poi);
            }
        }

        return Ok(null);
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