using AudioGuideAdmin.Data;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class TrackingController : Controller
{
    private readonly AppDbContext _context;

    public TrackingController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string? userId, int? poiId, string? triggerType, DateTime? dateFrom, DateTime? dateTo)
    {
        var poiLookup = _context.Pois.ToDictionary(x => x.Id, x => x.Name);

        var trackingQuery = _context.UserTrackings.AsQueryable();
        var visitQuery = _context.VisitHistories.AsQueryable();
        var triggerQuery = _context.GeofenceTriggers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            trackingQuery = trackingQuery.Where(x => x.UserId.Contains(userId));
            visitQuery = visitQuery.Where(x => x.UserId.Contains(userId));
            triggerQuery = triggerQuery.Where(x => x.UserId.Contains(userId));
        }

        if (poiId.HasValue)
        {
            visitQuery = visitQuery.Where(x => x.PoiId == poiId.Value);
            triggerQuery = triggerQuery.Where(x => x.PoiId == poiId.Value);
        }

        if (!string.IsNullOrWhiteSpace(triggerType))
        {
            visitQuery = visitQuery.Where(x => x.TriggerType == triggerType);
            triggerQuery = triggerQuery.Where(x => x.TriggerType == triggerType);
        }

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.Date;
            trackingQuery = trackingQuery.Where(x => x.RecordedAt >= from);
            visitQuery = visitQuery.Where(x => x.EndTime >= from);
            triggerQuery = triggerQuery.Where(x => x.RecordedAt >= from);
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.Date.AddDays(1);
            trackingQuery = trackingQuery.Where(x => x.RecordedAt < to);
            visitQuery = visitQuery.Where(x => x.EndTime < to);
            triggerQuery = triggerQuery.Where(x => x.RecordedAt < to);
        }

        var trackingRows = trackingQuery
            .OrderByDescending(x => x.RecordedAt)
            .Take(200)
            .ToList();

        var visitRows = visitQuery
            .OrderByDescending(x => x.EndTime)
            .Take(150)
            .ToList();

        var triggerRows = triggerQuery
            .OrderByDescending(x => x.RecordedAt)
            .Take(150)
            .ToList();

        var model = new TrackingViewModel
        {
            UserId = userId,
            PoiId = poiId,
            TriggerType = triggerType,
            DateFrom = dateFrom,
            DateTo = dateTo,
            TrackingCount = trackingRows.Count,
            VisitCount = visitRows.Count,
            TriggerCount = triggerRows.Count,
            UniqueVisitorCount = trackingRows.Select(x => x.UserId).Union(visitRows.Select(x => x.UserId)).Union(triggerRows.Select(x => x.UserId)).Distinct().Count(),
            PoiOptions = BuildPoiOptions(poiId),
            TriggerTypeOptions = BuildTriggerTypeOptions(triggerType),
            TrackingPoints = trackingRows
                .Select(x => new TrackingPointViewModel
                {
                    UserId = x.UserId,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Accuracy = x.Accuracy,
                    Source = x.Source,
                    IsForeground = x.IsForeground,
                    RecordedAt = x.RecordedAt
                })
                .ToList(),
            Visits = visitRows
                .Select(x => new VisitRowViewModel
                {
                    UserId = x.UserId,
                    PoiId = x.PoiId,
                    PoiName = poiLookup.GetValueOrDefault(x.PoiId, $"POI {x.PoiId}"),
                    Language = x.Language,
                    Duration = x.Duration,
                    TriggerType = x.TriggerType,
                    WasAutoPlayed = x.WasAutoPlayed,
                    WasCompleted = x.WasCompleted,
                    EndTime = x.EndTime
                })
                .ToList(),
            Triggers = triggerRows
                .Select(x => new TriggerRowViewModel
                {
                    UserId = x.UserId,
                    PoiId = x.PoiId,
                    PoiName = poiLookup.GetValueOrDefault(x.PoiId, $"POI {x.PoiId}"),
                    TriggerType = x.TriggerType,
                    Status = x.Status,
                    DistanceMeters = x.DistanceMeters,
                    RecordedAt = x.RecordedAt,
                    CooldownUntil = x.CooldownUntil
                })
                .ToList(),
            MapPoints = trackingRows
                .OrderBy(x => x.RecordedAt)
                .Take(120)
                .Select(x => new MapPointViewModel
                {
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Label = $"{x.UserId} - {x.RecordedAt:dd/MM HH:mm}"
                })
                .ToList()
        };

        return View(model);
    }

    private List<SelectListItem> BuildPoiOptions(int? selected)
    {
        var items = new List<SelectListItem>
        {
            new("Tat ca POI", "", !selected.HasValue)
        };

        items.AddRange(_context.Pois
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == selected))
            .ToList());

        return items;
    }

    private static List<SelectListItem> BuildTriggerTypeOptions(string? selected)
    {
        var types = new[]
        {
            new SelectListItem("Tat ca trigger", "", string.IsNullOrWhiteSpace(selected)),
            new SelectListItem("enter", "enter", selected == "enter"),
            new SelectListItem("nearby", "nearby", selected == "nearby"),
            new SelectListItem("manual", "manual", selected == "manual")
        };

        return types.ToList();
    }
}
