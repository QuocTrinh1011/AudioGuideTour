using AudioGuideAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AnalyticsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var totalPoi = await _context.Pois.CountAsync();
        var totalVisits = await _context.VisitHistories.CountAsync();
        var uniqueVisitors = await _context.VisitHistories
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();
        var avgListenDuration = await _context.VisitHistories
            .Select(x => (double?)x.Duration)
            .AverageAsync() ?? 0;

        return Ok(new
        {
            totalPoi,
            totalVisits,
            uniqueVisitors,
            avgListenDurationSeconds = Math.Round(avgListenDuration, 2)
        });
    }

    [HttpGet("top-poi")]
    public async Task<IActionResult> TopPoi()
    {
        var result = await _context.VisitHistories
            .GroupBy(v => v.PoiId)
            .Select(g => new
            {
                poiId = g.Key,
                listenCount = g.Count(),
                avgDuration = g.Average(x => x.Duration)
            })
            .OrderByDescending(x => x.listenCount)
            .Take(10)
            .ToListAsync();

        var poiLookup = await _context.Pois
            .Where(x => result.Select(r => r.poiId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        return Ok(result.Select(x => new
        {
            x.poiId,
            poiName = poiLookup.GetValueOrDefault(x.poiId, $"POI {x.poiId}"),
            x.listenCount,
            x.avgDuration
        }));
    }

    [HttpGet("avg-listen-duration")]
    public async Task<IActionResult> AverageListenDuration()
    {
        var result = await _context.VisitHistories
            .GroupBy(x => x.PoiId)
            .Select(g => new
            {
                poiId = g.Key,
                avgDuration = g.Average(x => x.Duration)
            })
            .OrderByDescending(x => x.avgDuration)
            .ToListAsync();

        return Ok(result);
    }
}
