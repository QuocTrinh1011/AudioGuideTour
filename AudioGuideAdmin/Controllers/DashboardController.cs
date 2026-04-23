using AudioGuideAdmin.Controllers.Data;
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
        var ownerManagedPoiQuery = _context.Pois
            .Where(x => x.OwnerId != null && x.OwnerId != string.Empty);

        var dashboardPoiQuery = ownerManagedPoiQuery.Any()
            ? ownerManagedPoiQuery
            : _context.Pois.AsQueryable();

        var dashboardPois = dashboardPoiQuery
            .Select(x => new { x.Id, x.Name })
            .ToList();

        var dashboardPoiIds = dashboardPois
            .Select(x => x.Id)
            .ToList();

        var poiLookup = dashboardPois.ToDictionary(x => x.Id, x => x.Name);

        var visitQuery = _context.VisitHistories
            .Where(x => dashboardPoiIds.Contains(x.PoiId));

        var triggerQuery = _context.GeofenceTriggers
            .Where(x => dashboardPoiIds.Contains(x.PoiId));

        var topPoiRows = visitQuery
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

        var recentTriggerRows = triggerQuery
            .OrderByDescending(x => x.RecordedAt)
            .Take(10)
            .ToList();

        var model = new DashboardViewModel
        {
            TotalPoi = dashboardPois.Count,
            TotalVisit = visitQuery.Count(),
            TotalTrackingPoint = _context.UserTrackings.Count(),
            TotalTour = _context.Tours.Count(),
            UniqueVisitors = visitQuery.Select(x => x.UserId).Distinct().Count(),
            AverageListenDuration = visitQuery.Select(x => (double?)x.Duration).Average() ?? 0
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
