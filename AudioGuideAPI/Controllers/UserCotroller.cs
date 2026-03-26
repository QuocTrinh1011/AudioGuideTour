using AudioGuideAPI.Data;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(User user)
    {
        var existing = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == user.Id || x.DeviceId == user.DeviceId);

        if (existing == null)
        {
            if (string.IsNullOrWhiteSpace(user.Id))
            {
                user.Id = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(user.DeviceId))
            {
                user.DeviceId = Guid.NewGuid().ToString("N");
            }

            user.CreatedAt = DateTime.UtcNow;
            user.LastSeenAt = DateTime.UtcNow;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        existing.DisplayName = user.DisplayName;
        existing.Language = user.Language;
        existing.AllowAutoPlay = user.AllowAutoPlay;
        existing.AllowBackgroundTracking = user.AllowBackgroundTracking;
        existing.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Users
            .AsNoTracking()
            .OrderByDescending(x => x.LastSeenAt)
            .ToListAsync());
    }
}
