using AudioGuideAdmin.Data;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuideAdmin.Controllers;

public class AnalyticsController : Controller
{
    private readonly AppDbContext _context;

    public AnalyticsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var poiLookup = _context.Pois.ToDictionary(x => x.Id, x => x.Name);
        var trackingPoints = _context.UserTrackings
            .OrderByDescending(x => x.RecordedAt)
            .Take(400)
            .ToList();

        var triggerRows = _context.GeofenceTriggers
            .OrderByDescending(x => x.RecordedAt)
            .Take(100)
            .ToList();

        var visitRows = _context.VisitHistories.ToList();
        var recentVisitRows = visitRows
            .OrderByDescending(x => x.EndTime)
            .Take(20)
            .ToList();

        var topPoiRows = visitRows
            .GroupBy(x => x.PoiId)
            .Select(g => new AnalyticsTopPoiViewModel
            {
                PoiId = g.Key,
                PoiName = poiLookup.GetValueOrDefault(g.Key, $"POI {g.Key}"),
                ListenCount = g.Count(),
                AverageDuration = g.Average(x => x.Duration),
                AutoPlayRate = g.Any() ? g.Count(x => x.WasAutoPlayed) * 100.0 / g.Count() : 0
            })
            .OrderByDescending(x => x.ListenCount)
            .Take(10)
            .ToList();

        var dailyRows = visitRows
            .GroupBy(x => x.EndTime.Date)
            .Select(g => new DailyListenViewModel
            {
                Date = g.Key,
                ListenCount = g.Count(),
                AverageDuration = g.Average(x => x.Duration)
            })
            .OrderByDescending(x => x.Date)
            .Take(7)
            .OrderBy(x => x.Date)
            .ToList();

        var model = new AnalyticsViewModel
        {
            TotalTrackingPoint = trackingPoints.Count,
            UniqueVisitors = trackingPoints.Select(x => x.UserId).Distinct().Count(),
            TotalTrigger = triggerRows.Count,
            AverageListenDuration = visitRows.Select(x => (double?)x.Duration).Average() ?? 0,
            CompletionRate = visitRows.Any() ? visitRows.Count(x => x.WasCompleted) * 100.0 / visitRows.Count : 0,
            AutoPlayRate = visitRows.Any() ? visitRows.Count(x => x.WasAutoPlayed) * 100.0 / visitRows.Count : 0,
            TopPois = topPoiRows,
            DailyListens = dailyRows,
            RecentTriggers = triggerRows.Select(x => new TriggerLogViewModel
            {
                UserId = x.UserId,
                PoiName = poiLookup.GetValueOrDefault(x.PoiId, $"POI {x.PoiId}"),
                TriggerType = x.TriggerType,
                Status = x.Status,
                DistanceMeters = x.DistanceMeters,
                RecordedAt = x.RecordedAt
            }).ToList(),
            RecentVisits = recentVisitRows.Select(x => new RecentVisitAnalyticsViewModel
            {
                UserId = x.UserId,
                PoiName = poiLookup.GetValueOrDefault(x.PoiId, $"POI {x.PoiId}"),
                Duration = x.Duration,
                Language = x.Language,
                WasAutoPlayed = x.WasAutoPlayed,
                EndTime = x.EndTime
            }).ToList()
        };

        return View(model);
    }
}
