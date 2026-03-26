using AudioGuideAdmin.Data;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;

public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var poiLookup = _context.Pois.ToDictionary(x => x.Id, x => x.Name);
        var topPoiRows = _context.VisitHistories
            .GroupBy(x => x.PoiId)
            .Select(g => new
            {
                PoiId = g.Key,
                ListenCount = g.Count(),
                AverageDuration = g.Average(x => x.Duration)
            })
            .OrderByDescending(x => x.ListenCount)
            .Take(5)
            .ToList();

        var recentTriggerRows = _context.GeofenceTriggers
            .OrderByDescending(x => x.RecordedAt)
            .Take(10)
            .ToList();

        var model = new DashboardViewModel
        {
            TotalPoi = _context.Pois.Count(),
            TotalVisit = _context.VisitHistories.Count(),
            TotalTrackingPoint = _context.UserTrackings.Count(),
            TotalTour = _context.Tours.Count(),
            UniqueVisitors = _context.VisitHistories.Select(x => x.UserId).Distinct().Count(),
            AverageListenDuration = _context.VisitHistories.Select(x => (double?)x.Duration).Average() ?? 0
        };

        model.TopPois = topPoiRows
            .Select(x => new TopPoiViewModel
            {
                PoiId = x.PoiId,
                ListenCount = x.ListenCount,
                AverageDuration = x.AverageDuration,
                PoiName = poiLookup.GetValueOrDefault(x.PoiId, $"POI {x.PoiId}")
            })
            .ToList();

        model.RecentTriggers = recentTriggerRows
            .Select(x => new RecentTriggerViewModel
            {
                UserId = x.UserId,
                PoiName = poiLookup.GetValueOrDefault(x.PoiId, $"POI {x.PoiId}"),
                TriggerType = x.TriggerType,
                RecordedAt = x.RecordedAt,
                DistanceMeters = x.DistanceMeters
            })
            .ToList();

        return View(model);
    }
}
