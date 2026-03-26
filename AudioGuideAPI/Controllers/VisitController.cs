using AudioGuideAPI.Data;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitController : ControllerBase
{
    private readonly AppDbContext _context;

    public VisitController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Save(VisitHistory visit)
    {
        if (visit.EndTime == default)
        {
            visit.EndTime = visit.StartTime == default
                ? DateTime.UtcNow
                : visit.StartTime.AddSeconds(Math.Max(visit.Duration, 0));
        }

        if (visit.StartTime == default)
        {
            visit.StartTime = visit.EndTime.AddSeconds(-Math.Max(visit.Duration, 0));
        }

        if (visit.Duration <= 0)
        {
            visit.Duration = Math.Max((int)(visit.EndTime - visit.StartTime).TotalSeconds, 0);
        }

        _context.VisitHistories.Add(visit);
        await _context.SaveChangesAsync();

        return Ok(visit);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> ByUser(string userId)
    {
        var visits = await _context.VisitHistories
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.EndTime)
            .ToListAsync();

        return Ok(visits);
    }
}
