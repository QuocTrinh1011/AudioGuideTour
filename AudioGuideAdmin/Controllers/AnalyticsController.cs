using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class AnalyticsController : Controller
{
    private static readonly string[] RoutePalette =
    {
        "#17324d",
        "#0f766e",
        "#dc2626",
        "#7c3aed",
        "#c97732",
        "#2563eb",
        "#be123c",
        "#2f855a"
    };

    private readonly AppDbContext _context;

    public AnalyticsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var windowStart = DateTime.UtcNow.AddDays(-7);
        var poiLookup = _context.Pois
            .AsNoTracking()
            .Select(x => new PoiLookupItem
            {
                Id = x.Id,
                Name = x.Name,
                Latitude = x.Latitude,
                Longitude = x.Longitude
            })
            .ToDictionary(x => x.Id);

        var trackingRows = _context.UserTrackings
            .AsNoTracking()
            .Where(x => x.RecordedAt >= windowStart)
            .OrderByDescending(x => x.RecordedAt)
            .Take(5000)
            .ToList();

        var triggerRows = _context.GeofenceTriggers
            .AsNoTracking()
            .Where(x => x.RecordedAt >= windowStart)
            .OrderByDescending(x => x.RecordedAt)
            .Take(200)
            .ToList();

        var visitRows = _context.VisitHistories
            .AsNoTracking()
            .Where(x => x.EndTime >= windowStart)
            .ToList();

        var recentVisitRows = visitRows
            .OrderByDescending(x => x.EndTime)
            .Take(20)
            .ToList();

        var visitorAliases = BuildVisitorAliases(
            trackingRows.Select(x => x.UserId)
                .Concat(triggerRows.Select(x => x.UserId))
                .Concat(visitRows.Select(x => x.UserId))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x));

        var topPoiRows = visitRows
            .GroupBy(x => x.PoiId)
            .Select(group =>
            {
                var poi = poiLookup.GetValueOrDefault(group.Key);
                return new AnalyticsTopPoiViewModel
                {
                    PoiId = group.Key,
                    PoiName = poi?.Name ?? $"POI {group.Key}",
                    ListenCount = group.Count(),
                    AverageDuration = group.Average(x => x.Duration),
                    AutoPlayRate = group.Any() ? group.Count(x => x.WasAutoPlayed) * 100.0 / group.Count() : 0,
                    Latitude = poi?.Latitude ?? 0,
                    Longitude = poi?.Longitude ?? 0
                };
            })
            .OrderByDescending(x => x.ListenCount)
            .ThenByDescending(x => x.AverageDuration)
            .Take(10)
            .ToList();

        var dailyRows = visitRows
            .GroupBy(x => x.EndTime.Date)
            .Select(group => new DailyListenViewModel
            {
                Date = group.Key,
                ListenCount = group.Count(),
                AverageDuration = group.Average(x => x.Duration)
            })
            .OrderByDescending(x => x.Date)
            .Take(7)
            .OrderBy(x => x.Date)
            .ToList();

        var heatmapPoints = BuildHeatmapPoints(trackingRows);
        var routeSessions = BuildRouteSessions(trackingRows, visitorAliases)
            .OrderByDescending(x => x.EndedAt)
            .Take(10)
            .ToList();

        var model = new AnalyticsViewModel
        {
            TrackingWindowLabel = "7 ngày gần nhất",
            TotalTrackingPoint = trackingRows.Count,
            UniqueVisitors = visitorAliases.Count,
            TotalTrigger = triggerRows.Count,
            HeatmapClusterCount = heatmapPoints.Count,
            RouteSessionCount = routeSessions.Count,
            AverageListenDuration = visitRows.Select(x => (double?)x.Duration).Average() ?? 0,
            CompletionRate = visitRows.Any() ? visitRows.Count(x => x.WasCompleted) * 100.0 / visitRows.Count : 0,
            AutoPlayRate = visitRows.Any() ? visitRows.Count(x => x.WasAutoPlayed) * 100.0 / visitRows.Count : 0,
            TopPois = topPoiRows,
            TopPoiMapPoints = topPoiRows
                .Where(x => x.Latitude != 0 && x.Longitude != 0)
                .Select(x => new AnalyticsPoiMapViewModel
                {
                    PoiId = x.PoiId,
                    PoiName = x.PoiName,
                    ListenCount = x.ListenCount,
                    AverageDuration = x.AverageDuration,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude
                })
                .ToList(),
            HeatmapPoints = heatmapPoints,
            RouteSessions = routeSessions,
            DailyListens = dailyRows,
            RecentTriggers = triggerRows.Select(x => new TriggerLogViewModel
            {
                VisitorAlias = ResolveVisitorAlias(visitorAliases, x.UserId),
                PoiName = poiLookup.GetValueOrDefault(x.PoiId)?.Name ?? $"POI {x.PoiId}",
                TriggerType = x.TriggerType,
                Status = x.Status,
                DistanceMeters = x.DistanceMeters,
                RecordedAt = x.RecordedAt
            }).ToList(),
            RecentVisits = recentVisitRows.Select(x => new RecentVisitAnalyticsViewModel
            {
                VisitorAlias = ResolveVisitorAlias(visitorAliases, x.UserId),
                PoiName = poiLookup.GetValueOrDefault(x.PoiId)?.Name ?? $"POI {x.PoiId}",
                Duration = x.Duration,
                Language = x.Language,
                TriggerType = x.TriggerType,
                WasAutoPlayed = x.WasAutoPlayed,
                EndTime = x.EndTime
            }).ToList()
        };

        return View(model);
    }

    private static Dictionary<string, string> BuildVisitorAliases(IEnumerable<string> userIds)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = 1;

        foreach (var userId in userIds)
        {
            if (string.IsNullOrWhiteSpace(userId) || aliases.ContainsKey(userId))
            {
                continue;
            }

            aliases[userId] = $"Visitor {index:00}";
            index++;
        }

        return aliases;
    }

    private static string ResolveVisitorAlias(IReadOnlyDictionary<string, string> aliases, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Visitor";
        }

        return aliases.TryGetValue(userId, out var alias)
            ? alias
            : $"Visitor {((Math.Abs(userId.GetHashCode()) % 99) + 1):00}";
    }

    private static List<AnalyticsHeatPointViewModel> BuildHeatmapPoints(IEnumerable<UserTracking> trackingRows)
    {
        var grouped = trackingRows
            .Where(x => x.Accuracy <= 120 || x.Accuracy == 0)
            .GroupBy(x => new
            {
                Latitude = Math.Round(x.Latitude, 4),
                Longitude = Math.Round(x.Longitude, 4)
            })
            .Select(group => new
            {
                group.Key.Latitude,
                group.Key.Longitude,
                SampleCount = group.Count()
            })
            .OrderByDescending(x => x.SampleCount)
            .Take(500)
            .ToList();

        var peak = grouped.Any() ? grouped.Max(x => x.SampleCount) : 1;

        return grouped
            .Select(x => new AnalyticsHeatPointViewModel
            {
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                SampleCount = x.SampleCount,
                Intensity = Math.Round(Math.Max(0.18, x.SampleCount / (double)peak), 2)
            })
            .ToList();
    }

    private static List<AnalyticsRouteViewModel> BuildRouteSessions(
        IEnumerable<UserTracking> trackingRows,
        IReadOnlyDictionary<string, string> visitorAliases)
    {
        var sessions = new List<AnalyticsRouteViewModel>();

        foreach (var visitorGroup in trackingRows
                     .Where(x => x.Accuracy <= 150 || x.Accuracy == 0)
                     .OrderBy(x => x.RecordedAt)
                     .GroupBy(x => x.UserId))
        {
            var currentSession = new List<UserTracking>();

            foreach (var point in visitorGroup)
            {
                if (currentSession.Count == 0)
                {
                    currentSession.Add(point);
                    continue;
                }

                var previous = currentSession[^1];
                var gap = point.RecordedAt - previous.RecordedAt;
                var distance = CalculateDistanceMeters(previous.Latitude, previous.Longitude, point.Latitude, point.Longitude);

                if (gap > TimeSpan.FromMinutes(18) || distance > 1800)
                {
                    AddRouteSession(sessions, currentSession, visitorGroup.Key, visitorAliases);
                    currentSession = new List<UserTracking> { point };
                    continue;
                }

                currentSession.Add(point);
            }

            AddRouteSession(sessions, currentSession, visitorGroup.Key, visitorAliases);
        }

        return sessions;
    }

    private static void AddRouteSession(
        ICollection<AnalyticsRouteViewModel> sessions,
        IReadOnlyList<UserTracking> points,
        string userId,
        IReadOnlyDictionary<string, string> visitorAliases)
    {
        if (points.Count < 2)
        {
            return;
        }

        var alias = ResolveVisitorAlias(visitorAliases, userId);
        var trimmedPoints = points.TakeLast(40).ToList();
        var color = RoutePalette[sessions.Count % RoutePalette.Length];

        sessions.Add(new AnalyticsRouteViewModel
        {
            VisitorAlias = alias,
            Color = color,
            StartedAt = trimmedPoints.First().RecordedAt,
            EndedAt = trimmedPoints.Last().RecordedAt,
            PointCount = trimmedPoints.Count,
            Points = trimmedPoints.Select(point => new MapPointViewModel
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                Accuracy = point.Accuracy,
                RecordedAt = point.RecordedAt,
                Label = $"{alias} · {point.RecordedAt:dd/MM HH:mm}"
            }).ToList()
        });
    }

    private static double CalculateDistanceMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double earthRadius = 6371000d;
        var lat1 = DegreesToRadians(latitude1);
        var lat2 = DegreesToRadians(latitude2);
        var deltaLat = DegreesToRadians(latitude2 - latitude1);
        var deltaLng = DegreesToRadians(longitude2 - longitude1);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadius * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private sealed class PoiLookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
