using AudioGuideAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HeatmapController : ControllerBase
{
    private readonly AppDbContext _context;

    public HeatmapController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetHeatmap([FromQuery] int days = 7, [FromQuery] double maxAccuracy = 120)
    {
        days = Math.Clamp(days, 1, 30);
        maxAccuracy = Math.Clamp(maxAccuracy, 30, 300);
        var windowStart = DateTime.UtcNow.AddDays(-days);

        var data = await _context.UserTrackings
            .AsNoTracking()
            .Where(x => x.RecordedAt >= windowStart)
            .Where(x => x.Latitude != 0 && x.Longitude != 0)
            .Where(x => x.Accuracy == 0 || x.Accuracy <= maxAccuracy)
            .GroupBy(x => new
            {
                Latitude = Math.Round(x.Latitude, 4),
                Longitude = Math.Round(x.Longitude, 4)
            })
            .Select(g => new
            {
                g.Key.Latitude,
                g.Key.Longitude,
                count = g.Count()
            })
            .OrderByDescending(x => x.count)
            .Take(500)
            .ToListAsync();

        return Ok(data);
    }
}
