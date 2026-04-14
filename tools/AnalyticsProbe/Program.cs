using AudioGuideAdmin.Controllers;
using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

var dbPath = @"C:\Users\Quoc Trinh\source\repos\AudioGuideSystem\SharedStorage\data\AudioGuide.db";
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

await using var context = new AppDbContext(options);
var controller = new AnalyticsController(context, NullLogger<AnalyticsController>.Instance);

try
{
    var result = controller.Index();
    Console.WriteLine(result.GetType().FullName);

    if (result is ViewResult view)
    {
        Console.WriteLine($"ViewName={view.ViewName ?? "(default)"}");
        Console.WriteLine($"ModelType={view.Model?.GetType().FullName ?? "(null)"}");
        if (view.Model is AnalyticsViewModel model)
        {
            Console.WriteLine($"ErrorMessage={model.ErrorMessage ?? "(null)"}");
            Console.WriteLine($"Tracking={model.TotalTrackingPoint}");
            Console.WriteLine($"Raw={model.RawTrackingPoint}");
            Console.WriteLine($"Visitors={model.UniqueVisitors}");
            Console.WriteLine($"Triggers={model.TotalTrigger}");
            Console.WriteLine($"HeatClusters={model.HeatmapClusterCount}");
            Console.WriteLine($"RouteSessions={model.RouteSessionCount}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("EXCEPTION");
    Console.WriteLine(ex);
    throw;
}

Console.WriteLine("---- step probe ----");

var days = 7;
var maxAccuracy = 120d;
var startHour = 0;
var endHour = 23;
var windowStart = DateTime.UtcNow.AddDays(-days);

try
{
    var poiLookup = context.Pois
        .AsNoTracking()
        .Select(x => new { x.Id, x.Name, x.Latitude, x.Longitude })
        .ToDictionary(x => x.Id);
    Console.WriteLine($"poiLookup={poiLookup.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("poiLookup failed");
    Console.WriteLine(ex);
}

List<UserTracking> rawTrackingRows = [];
try
{
    rawTrackingRows = context.UserTrackings
        .AsNoTracking()
        .Where(x => x.RecordedAt >= windowStart)
        .OrderByDescending(x => x.RecordedAt)
        .Take(20000)
        .ToList();
    Console.WriteLine($"rawTrackingRows={rawTrackingRows.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("rawTrackingRows failed");
    Console.WriteLine(ex);
}

try
{
    var trackingRows = rawTrackingRows
        .Where(x => x.Latitude != 0 && x.Longitude != 0)
        .Where(x => x.Accuracy == 0 || x.Accuracy <= maxAccuracy)
        .Where(x =>
        {
            var local = x.RecordedAt.Kind == DateTimeKind.Utc
                ? x.RecordedAt
                : DateTime.SpecifyKind(x.RecordedAt, DateTimeKind.Utc);
            var hour = TimeZoneInfo.ConvertTimeFromUtc(local, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Hour;
            return startHour <= endHour
                ? hour >= startHour && hour <= endHour
                : hour >= startHour || hour <= endHour;
        })
        .ToList();
    Console.WriteLine($"trackingRows={trackingRows.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("trackingRows failed");
    Console.WriteLine(ex);
}

try
{
    var triggerRows = context.GeofenceTriggers
        .AsNoTracking()
        .Where(x => x.RecordedAt >= windowStart)
        .ToList();
    Console.WriteLine($"triggerRows={triggerRows.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("triggerRows failed");
    Console.WriteLine(ex);
}

try
{
    var visitRows = context.VisitHistories
        .AsNoTracking()
        .Where(x => x.EndTime >= windowStart)
        .ToList();
    Console.WriteLine($"visitRows={visitRows.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("visitRows failed");
    Console.WriteLine(ex);
}

var triggerRows2 = context.GeofenceTriggers.AsNoTracking().Where(x => x.RecordedAt >= windowStart).ToList();
var visitRows2 = context.VisitHistories.AsNoTracking().Where(x => x.EndTime >= windowStart).ToList();
var trackingRows2 = rawTrackingRows
    .Where(x => x.Latitude != 0 && x.Longitude != 0)
    .Where(x => x.Accuracy == 0 || x.Accuracy <= maxAccuracy)
    .ToList();

try
{
    var visitorAliases = trackingRows2.Select(x => x.UserId)
        .Concat(triggerRows2.Select(x => x.UserId))
        .Concat(visitRows2.Select(x => x.UserId))
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct()
        .OrderBy(x => x)
        .Select((x, i) => new { x, alias = $"Visitor {i + 1:00}" })
        .ToDictionary(x => x.x!, x => x.alias);
    Console.WriteLine($"visitorAliases={visitorAliases.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("visitorAliases failed");
    Console.WriteLine(ex);
}

try
{
    var dailyRows = visitRows2
        .GroupBy(x =>
        {
            var local = x.EndTime.Kind == DateTimeKind.Utc
                ? x.EndTime
                : DateTime.SpecifyKind(x.EndTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(local, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
        })
        .Select(group => new { group.Key, Count = group.Count(), Avg = group.Average(x => x.Duration) })
        .ToList();
    Console.WriteLine($"dailyRows={dailyRows.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("dailyRows failed");
    Console.WriteLine(ex);
}

try
{
    var topPoiRows = visitRows2
        .GroupBy(x => x.PoiId)
        .Select(group => new { PoiId = group.Key, Count = group.Count(), Avg = group.Average(x => x.Duration) })
        .ToList();
    Console.WriteLine($"topPoiRows={topPoiRows.Count}");
}
catch (Exception ex)
{
    Console.WriteLine("topPoiRows failed");
    Console.WriteLine(ex);
}

var visitorAliases2 = trackingRows2.Select(x => x.UserId)
    .Concat(triggerRows2.Select(x => x.UserId))
    .Concat(visitRows2.Select(x => x.UserId))
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .Distinct()
    .OrderBy(x => x)
    .Select((x, i) => new { x, alias = $"Visitor {i + 1:00}" })
    .ToDictionary(x => x.x!, x => x.alias);

try
{
    var method = typeof(AnalyticsController).GetMethod("BuildHeatmapPayload", BindingFlags.NonPublic | BindingFlags.Static);
    var payload = method!.Invoke(null, [trackingRows2, visitorAliases2, null, 15]);
    Console.WriteLine($"buildHeatmapPayload={(payload == null ? "null" : "ok")}");
}
catch (Exception ex)
{
    Console.WriteLine("buildHeatmapPayload failed");
    Console.WriteLine(ex);
}

try
{
    var method = typeof(AnalyticsController).GetMethod("BuildRouteSessions", BindingFlags.NonPublic | BindingFlags.Static);
    var routes = method!.Invoke(null, [trackingRows2, visitorAliases2]);
    Console.WriteLine($"buildRouteSessions={(routes == null ? "null" : "ok")}");
}
catch (Exception ex)
{
    Console.WriteLine("buildRouteSessions failed");
    Console.WriteLine(ex);
}

try
{
    var poiLookup2 = context.Pois
        .AsNoTracking()
        .Select(x => new { x.Id, x.Name, x.Latitude, x.Longitude })
        .ToDictionary(x => x.Id);

    var topPoiRows2 = visitRows2
        .GroupBy(x => x.PoiId)
        .Select(group =>
        {
            var poi = poiLookup2.GetValueOrDefault(group.Key);
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

    var buildHeatmapPayloadMethod = typeof(AnalyticsController).GetMethod("BuildHeatmapPayload", BindingFlags.NonPublic | BindingFlags.Static)!;
    var buildRouteSessionsMethod = typeof(AnalyticsController).GetMethod("BuildRouteSessions", BindingFlags.NonPublic | BindingFlags.Static)!;
    var resolveVisitorAliasMethod = typeof(AnalyticsController).GetMethod("ResolveVisitorAlias", BindingFlags.NonPublic | BindingFlags.Static)!;

    var initialHeatPayload2 = (AnalyticsHeatmapPayloadViewModel)buildHeatmapPayloadMethod.Invoke(null, [trackingRows2, visitorAliases2, null, 15])!;
    var routeSessions2 = ((IEnumerable<AnalyticsRouteViewModel>)buildRouteSessionsMethod.Invoke(null, [trackingRows2, visitorAliases2])!)
        .OrderByDescending(x => x.EndedAt)
        .Take(10)
        .ToList();

    var dailyRows2 = visitRows2
        .GroupBy(x =>
        {
            var local = x.EndTime.Kind == DateTimeKind.Utc
                ? x.EndTime
                : DateTime.SpecifyKind(x.EndTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(local, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
        })
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

    var recentVisitRows2 = visitRows2.OrderByDescending(x => x.EndTime).Take(20).ToList();

    var model2 = new AnalyticsViewModel
    {
        TrackingWindowLabel = days == 1 ? "24 giờ gần nhất" : $"{days} ngày gần nhất",
        SelectedWindowDays = days,
        SelectedMaxAccuracyMeters = maxAccuracy,
        SelectedStartHour = startHour,
        SelectedEndHour = endHour,
        TotalTrackingPoint = trackingRows2.Count,
        RawTrackingPoint = rawTrackingRows.Count,
        FilteredOutTrackingPoint = Math.Max(rawTrackingRows.Count - trackingRows2.Count, 0),
        UniqueVisitors = visitorAliases2.Count,
        TotalTrigger = triggerRows2.Count,
        HeatmapClusterCount = initialHeatPayload2.HeatmapClusterCount,
        RouteSessionCount = routeSessions2.Count,
        AverageListenDuration = visitRows2.Select(x => (double?)x.Duration).Average() ?? 0,
        CompletionRate = visitRows2.Any() ? visitRows2.Count(x => x.WasCompleted) * 100.0 / visitRows2.Count : 0,
        AutoPlayRate = visitRows2.Any() ? visitRows2.Count(x => x.WasAutoPlayed) * 100.0 / visitRows2.Count : 0,
        InitialHeatCellSizeMeters = initialHeatPayload2.CellSizeMeters,
        TopPois = topPoiRows2,
        TopPoiMapPoints = topPoiRows2
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
        HeatmapPoints = initialHeatPayload2.HeatPoints,
        HeatmapCells = initialHeatPayload2.GridCells,
        HottestCell = initialHeatPayload2.HottestCell,
        VisitorPoints = initialHeatPayload2.VisitorPoints,
        RouteSessions = routeSessions2,
        DailyListens = dailyRows2,
        RecentTriggers = triggerRows2.Select(x => new TriggerLogViewModel
        {
            VisitorAlias = (string)resolveVisitorAliasMethod.Invoke(null, [visitorAliases2, x.UserId])!,
            PoiName = poiLookup2.GetValueOrDefault(x.PoiId)?.Name ?? $"POI {x.PoiId}",
            TriggerType = x.TriggerType,
            Status = x.Status,
            DistanceMeters = x.DistanceMeters,
            RecordedAt = x.RecordedAt
        }).ToList(),
        RecentVisits = recentVisitRows2.Select(x => new RecentVisitAnalyticsViewModel
        {
            VisitorAlias = (string)resolveVisitorAliasMethod.Invoke(null, [visitorAliases2, x.UserId])!,
            PoiName = poiLookup2.GetValueOrDefault(x.PoiId)?.Name ?? $"POI {x.PoiId}",
            Duration = x.Duration,
            Language = x.Language,
            TriggerType = x.TriggerType,
            WasAutoPlayed = x.WasAutoPlayed,
            EndTime = x.EndTime
        }).ToList()
    };

    Console.WriteLine($"fullModel ok: tracking={model2.TotalTrackingPoint}, clusters={model2.HeatmapClusterCount}, routes={model2.RouteSessionCount}");
}
catch (Exception ex)
{
    Console.WriteLine("fullModel failed");
    Console.WriteLine(ex);
}
