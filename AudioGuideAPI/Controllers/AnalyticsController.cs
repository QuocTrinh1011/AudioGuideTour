using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Data;

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

    [HttpGet("top-poi")]
    public async Task<IActionResult> TopPoi()
    {
        var result = await _context.VisitHistories
            .GroupBy(v => v.PoiId)
            .Select(g => new
            {
                PoiId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        return Ok(result);
    }
}