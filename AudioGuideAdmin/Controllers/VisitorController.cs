using AudioGuideAdmin.Data;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class VisitorController : Controller
{
    private readonly AppDbContext _context;

    public VisitorController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var visitors = await _context.Visitors
            .AsNoTracking()
            .OrderByDescending(x => x.LastSeenAt)
            .ToListAsync();

        var userIds = visitors.Select(x => x.Id).ToList();

        var trackingCounts = await _context.UserTrackings
            .Where(x => userIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var visitCounts = await _context.VisitHistories
            .Where(x => userIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var triggerCounts = await _context.GeofenceTriggers
            .Where(x => userIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var model = visitors.Select(visitor => new VisitorSummaryViewModel
        {
            Visitor = visitor,
            TrackingCount = trackingCounts.GetValueOrDefault(visitor.Id, 0),
            VisitCount = visitCounts.GetValueOrDefault(visitor.Id, 0),
            TriggerCount = triggerCounts.GetValueOrDefault(visitor.Id, 0)
        }).ToList();

        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var visitor = await _context.Visitors.FindAsync(id);
        return visitor == null ? NotFound() : View(visitor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Models.VisitorProfile model)
    {
        var existing = await _context.Visitors.FindAsync(model.Id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.DisplayName = model.DisplayName?.Trim() ?? existing.DisplayName;
        existing.Language = model.Language?.Trim() ?? existing.Language;
        existing.AllowAutoPlay = model.AllowAutoPlay;
        existing.AllowBackgroundTracking = model.AllowBackgroundTracking;
        existing.LastSeenAt = existing.LastSeenAt == default ? DateTime.UtcNow : existing.LastSeenAt;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Da cap nhat visitor cho mobile app.";
        return RedirectToAction(nameof(Index));
    }
}
