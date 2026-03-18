using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Data;

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
            .Select(x => new
            {
                x.Latitude,
                x.Longitude
            })
            .ToListAsync();

        return Ok(data);
    }
}