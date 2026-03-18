using Microsoft.AspNetCore.Mvc;
using AudioGuideAPI.Data;
using AudioGuideAPI.Models;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrackingController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrackingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Track(UserTracking tracking)
    {
        _context.UserTrackings.Add(tracking);

        await _context.SaveChangesAsync();

        return Ok();
    }
}