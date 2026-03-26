using AudioGuideAPI.Data;
using AudioGuideAPI.DTOs;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<IActionResult> Track(LocationRequest request)
    {
        var user = await ResolveUserAsync(request);
        var tracking = new UserTracking
        {
            UserId = user.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Accuracy = request.Accuracy,
            SpeedMetersPerSecond = request.SpeedMetersPerSecond,
            Bearing = request.Bearing,
            IsForeground = request.IsForeground,
            RecordedAt = request.RecordedAt == default ? DateTime.UtcNow : request.RecordedAt.ToUniversalTime()
        };

        _context.UserTrackings.Add(tracking);
        await _context.SaveChangesAsync();

        return Ok(tracking);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> History(string userId)
    {
        var history = await _context.UserTrackings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RecordedAt)
            .Take(500)
            .ToListAsync();

        return Ok(history);
    }

    private async Task<User> ResolveUserAsync(LocationRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == request.UserId || x.DeviceId == request.DeviceId);

        if (user != null)
        {
            user.Language = request.Language;
            user.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return user;
        }

        user = new User
        {
            Id = string.IsNullOrWhiteSpace(request.UserId) ? Guid.NewGuid().ToString("N") : request.UserId,
            DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? Guid.NewGuid().ToString("N") : request.DeviceId,
            Language = request.Language,
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
