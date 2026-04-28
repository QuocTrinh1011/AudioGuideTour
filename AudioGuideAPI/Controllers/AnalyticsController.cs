using AudioGuideAPI.Data;
using AudioGuideAPI.Helpers;
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
        var scopedPoiIds = ApiPoiScopeHelper.GetScopedPoiIds(_context);
        var totalPoi = scopedPoiIds.Count;
        var totalVisits = await _context.VisitHistories
            .Where(x => scopedPoiIds.Contains(x.PoiId))
            .CountAsync();
        var uniqueVisitors = await _context.VisitHistories
            .Where(x => scopedPoiIds.Contains(x.PoiId))
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();
        var avgListenDuration = await _context.VisitHistories
            .Where(x => scopedPoiIds.Contains(x.PoiId))
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
        var scopedPoiIds = ApiPoiScopeHelper.GetScopedPoiIds(_context);
        var result = await _context.VisitHistories
            .Where(v => scopedPoiIds.Contains(v.PoiId))
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
        var scopedPoiIds = ApiPoiScopeHelper.GetScopedPoiIds(_context);
        var result = await _context.VisitHistories
            .Where(x => scopedPoiIds.Contains(x.PoiId))
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
