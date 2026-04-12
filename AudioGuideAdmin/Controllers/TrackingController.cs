using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
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

        IQueryable<UserTracking> trackingQuery = _context.UserTrackings.AsQueryable();
        IQueryable<VisitHistory> visitQuery = _context.VisitHistories.AsQueryable();
        IQueryable<GeofenceTrigger> triggerQuery = _context.GeofenceTriggers.AsQueryable();

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

        var totalTrackingCount = trackingQuery.Count();
        var totalVisitCount = visitQuery.Count();
        var totalTriggerCount = triggerQuery.Count();
        var totalUniqueVisitorCount = trackingQuery.Select(x => x.UserId)
            .Concat(visitQuery.Select(x => x.UserId))
            .Concat(triggerQuery.Select(x => x.UserId))
            .Distinct()
            .Count();

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
            TrackingCount = totalTrackingCount,
            VisitCount = totalVisitCount,
            TriggerCount = totalTriggerCount,
            UniqueVisitorCount = totalUniqueVisitorCount,
            DisplayedTrackingCount = trackingRows.Count,
            DisplayedVisitCount = visitRows.Count,
            DisplayedTriggerCount = triggerRows.Count,
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
                .ToList()
        };

        return View(model);
    }

    private List<SelectListItem> BuildPoiOptions(int? selected)
    {
        var items = new List<SelectListItem>
        {
            new("Tất cả POI", "", !selected.HasValue)
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
            new SelectListItem("Tất cả trigger", "", string.IsNullOrWhiteSpace(selected)),
            new SelectListItem("enter", "enter", selected == "enter"),
            new SelectListItem("nearby", "nearby", selected == "nearby"),
            new SelectListItem("manual", "manual", selected == "manual"),
            new SelectListItem("qr", "qr", selected == "qr"),
            new SelectListItem("tour", "tour", selected == "tour")
        };

        return types.ToList();
    }
}
