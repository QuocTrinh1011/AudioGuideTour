using AudioGuideAdmin.Data;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class VisitorController : Controller
{
    private static readonly TimeSpan ActiveThreshold = TimeSpan.FromMinutes(5);
    private static readonly HashSet<string> SupportedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi-VN",
        "en-US",
        "zh-CN"
    };

    private readonly AppDbContext _context;

    public VisitorController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(bool includeTest = false)
    {
        var now = DateTime.UtcNow;
        var localTimeZone = TimeZoneInfo.Local;
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

        var items = visitors.Select(visitor => new VisitorSummaryViewModel
        {
            Visitor = visitor,
            TrackingCount = trackingCounts.GetValueOrDefault(visitor.Id, 0),
            VisitCount = visitCounts.GetValueOrDefault(visitor.Id, 0),
            TriggerCount = triggerCounts.GetValueOrDefault(visitor.Id, 0),
            IsActive = visitor.LastSeenAt != default && now - NormalizeStoredUtc(visitor.LastSeenAt) <= ActiveThreshold,
            IsSyntheticData = IsSyntheticVisitor(visitor),
            LastSeenDisplayText = FormatLastSeenLocal(visitor.LastSeenAt, localTimeZone),
            LastSeenAgoText = FormatLastSeenAgo(visitor.LastSeenAt, now)
        })
        .OrderByDescending(x => x.IsActive)
        .ThenBy(x => x.IsSyntheticData)
        .ThenByDescending(x => x.Visitor.LastSeenAt)
        .ToList();

        var hiddenTestVisitors = items.Count(x => x.IsSyntheticData);
        var displayItems = includeTest
            ? items
            : items.Where(x => !x.IsSyntheticData).ToList();

        var model = new VisitorIndexViewModel
        {
            TotalVisitors = displayItems.Count,
            ActiveVisitors = displayItems.Count(x => x.IsActive),
            InactiveVisitors = displayItems.Count(x => !x.IsActive),
            ActiveThresholdMinutes = (int)ActiveThreshold.TotalMinutes,
            IncludeTestData = includeTest,
            HiddenTestVisitors = includeTest ? 0 : hiddenTestVisitors,
            Visitors = displayItems
        };

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
        existing.Language = NormalizeLanguage(model.Language, existing.Language);
        existing.AllowAutoPlay = model.AllowAutoPlay;
        existing.AllowBackgroundTracking = model.AllowBackgroundTracking;
        existing.LastSeenAt = existing.LastSeenAt == default ? DateTime.UtcNow : existing.LastSeenAt;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã cấp nhat visitor cho mobile app.";
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizeLanguage(string? language, string currentLanguage)
    {
        var normalized = string.IsNullOrWhiteSpace(language)
            ? currentLanguage
            : language.Trim().Replace('_', '-');

        return SupportedCodes.Contains(normalized) ? normalized : "vi-VN";
    }

    private static string FormatLastSeenAgo(DateTime lastSeenAt, DateTime nowUtc)
    {
        if (lastSeenAt == default)
        {
            return "Chưa có hoạt động";
        }

        var delta = nowUtc - NormalizeStoredUtc(lastSeenAt);
        if (delta.TotalSeconds < 60)
        {
            return "Vừa xong";
        }

        if (delta.TotalMinutes < 60)
        {
            return $"{Math.Max(1, (int)Math.Floor(delta.TotalMinutes))} phút trước";
        }

        if (delta.TotalHours < 24)
        {
            return $"{Math.Max(1, (int)Math.Floor(delta.TotalHours))} giờ trước";
        }

        return $"{Math.Max(1, (int)Math.Floor(delta.TotalDays))} ngày trước";
    }

    private static string FormatLastSeenLocal(DateTime lastSeenAt, TimeZoneInfo timeZone)
    {
        if (lastSeenAt == default)
        {
            return "Chưa có hoạt động";
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(NormalizeStoredUtc(lastSeenAt), timeZone);
        return local.ToString("dd/MM/yyyy HH:mm");
    }

    private static DateTime NormalizeStoredUtc(DateTime timestamp)
    {
        if (timestamp == default)
        {
            return default;
        }

        return timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
        };
    }

    private static bool IsSyntheticVisitor(Models.VisitorProfile visitor)
    {
        var deviceId = visitor.DeviceId?.Trim() ?? string.Empty;
        var displayName = visitor.DisplayName?.Trim() ?? string.Empty;

        return deviceId.StartsWith("sim-device", StringComparison.OrdinalIgnoreCase)
            || deviceId.Equals("demo-device", StringComparison.OrdinalIgnoreCase)
            || displayName.StartsWith("Khách test", StringComparison.OrdinalIgnoreCase)
            || displayName.Equals("Khách demo", StringComparison.OrdinalIgnoreCase);
    }
}
