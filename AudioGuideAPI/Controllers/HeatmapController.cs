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
    public async Task<IActionResult> GetHeatmap()
    {
        var data = await _context.UserTrackings
            .AsNoTracking()
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
