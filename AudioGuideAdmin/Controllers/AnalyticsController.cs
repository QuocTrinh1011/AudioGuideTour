using AudioGuideAdmin.Controllers.Data;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    private static readonly TimeZoneInfo AnalyticsTimeZone = ResolveAnalyticsTimeZone();
    private const int MaxTrackingRows = 20000;
    private const int MaxTriggerRows = 200;
    private const int MaxHeatCells = 500;
    private const int MaxVisibleVisitorPoints = 250;
    private const int MinValidVisitDurationSeconds = 5;
    private const int MaxValidVisitDurationSeconds = 1800;
    private const int MinRouteSessionPointCount = 3;
    private const double MaxReasonableTravelSpeedMetersPerSecond = 22d;
    private const double MinSuspiciousJumpDistanceMeters = 120d;
    private static readonly TimeSpan MaxDwellGap = TimeSpan.FromMinutes(3);

    private readonly AppDbContext _context;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(AppDbContext context, ILogger<AnalyticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index([FromQuery] int days = 7, [FromQuery] double maxAccuracy = 120, [FromQuery] int startHour = 0, [FromQuery] int endHour = 23)
    {
        NormalizeFilters(ref days, ref maxAccuracy, ref startHour, ref endHour);
        try
        {

        var windowStart = DateTime.UtcNow.AddDays(-days);
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

        var rawTrackingRows = LoadRawTrackingRows(windowStart);
        var trackingRows = FilterTrackingRows(rawTrackingRows, maxAccuracy, startHour, endHour);

        var triggerRows = _context.GeofenceTriggers
            .AsNoTracking()
            .Where(x => x.RecordedAt >= windowStart)
            .OrderByDescending(x => x.RecordedAt)
            .ToList()
            .Where(x => IsWithinHourWindow(x.RecordedAt, startHour, endHour))
            .Take(MaxTriggerRows)
            .ToList();

        var rawVisitRows = _context.VisitHistories
            .AsNoTracking()
            .Where(x => x.EndTime >= windowStart)
            .ToList()
            .Where(x => IsWithinHourWindow(x.EndTime, startHour, endHour))
            .ToList();
        var visitRows = FilterValidVisitRows(rawVisitRows);

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
            .GroupBy(x => ToAnalyticsLocalTime(x.EndTime).Date)
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

        var initialHeatPayload = BuildHeatmapPayload(trackingRows, visitorAliases, null, 15);
        var routeSessions = BuildRouteSessions(trackingRows, visitorAliases)
            .OrderByDescending(x => x.EndedAt)
            .Take(10)
            .ToList();

        var model = new AnalyticsViewModel
        {
            TrackingWindowLabel = days == 1 ? "24 giờ gần nhất" : $"{days} ngày gần nhất",
            SelectedWindowDays = days,
            SelectedMaxAccuracyMeters = maxAccuracy,
            SelectedStartHour = startHour,
            SelectedEndHour = endHour,
            TotalTrackingPoint = trackingRows.Count,
            RawTrackingPoint = rawTrackingRows.Count,
            FilteredOutTrackingPoint = Math.Max(rawTrackingRows.Count - trackingRows.Count, 0),
            UniqueVisitors = visitorAliases.Count,
            TotalTrigger = triggerRows.Count,
            HeatmapClusterCount = initialHeatPayload.HeatmapClusterCount,
            RouteSessionCount = routeSessions.Count,
            AverageListenDuration = visitRows.Select(x => (double?)x.Duration).Average() ?? 0,
            CompletionRate = visitRows.Any() ? visitRows.Count(x => x.WasCompleted) * 100.0 / visitRows.Count : 0,
            AutoPlayRate = visitRows.Any() ? visitRows.Count(x => x.WasAutoPlayed) * 100.0 / visitRows.Count : 0,
            InitialHeatCellSizeMeters = initialHeatPayload.CellSizeMeters,
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
            HeatmapPoints = initialHeatPayload.HeatPoints,
            HeatmapCells = initialHeatPayload.GridCells,
            HottestCell = initialHeatPayload.HottestCell,
            VisitorPoints = initialHeatPayload.VisitorPoints,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tải trang Analytics với bộ lọc days={Days}, maxAccuracy={MaxAccuracy}, startHour={StartHour}, endHour={EndHour}", days, maxAccuracy, startHour, endHour);

            return View(new AnalyticsViewModel
            {
                ErrorMessage = "Không thể tải đầy đủ dữ liệu analytics trong lần này. Trang đã được giữ ở chế độ an toàn để admin không bị văng ra ngoài.",
                TrackingWindowLabel = days == 1 ? "24 giờ gần nhất" : $"{days} ngày gần nhất",
                SelectedWindowDays = days,
                SelectedMaxAccuracyMeters = maxAccuracy,
                SelectedStartHour = startHour,
                SelectedEndHour = endHour,
                InitialHeatCellSizeMeters = ResolveHeatCellSizeMeters(15)
            });
        }
    }

    [HttpGet]
    public IActionResult HeatmapData(
        [FromQuery] int days = 7,
        [FromQuery] double maxAccuracy = 120,
        [FromQuery] int startHour = 0,
        [FromQuery] int endHour = 23,
        [FromQuery] double? south = null,
        [FromQuery] double? west = null,
        [FromQuery] double? north = null,
        [FromQuery] double? east = null,
        [FromQuery] int zoom = 15)
    {
        NormalizeFilters(ref days, ref maxAccuracy, ref startHour, ref endHour);
        try
        {
        zoom = Math.Clamp(zoom, 11, 19);

        var windowStart = DateTime.UtcNow.AddDays(-days);
        var rawTrackingRows = LoadRawTrackingRows(windowStart);
        var trackingRows = FilterTrackingRows(rawTrackingRows, maxAccuracy, startHour, endHour);

        HeatmapBounds? bounds = null;
        if (south.HasValue && west.HasValue && north.HasValue && east.HasValue)
        {
            bounds = new HeatmapBounds(
                south.Value,
                west.Value,
                north.Value,
                east.Value);
            trackingRows = trackingRows
                .Where(x => x.Latitude >= bounds.Value.South && x.Latitude <= bounds.Value.North)
                .Where(x => x.Longitude >= bounds.Value.West && x.Longitude <= bounds.Value.East)
                .ToList();
        }

        var visitorAliases = BuildVisitorAliases(
            trackingRows.Select(x => x.UserId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x));

        var payload = BuildHeatmapPayload(trackingRows, visitorAliases, bounds, zoom);
        return Ok(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể tải HeatmapData của Analytics");
            return StatusCode(StatusCodes.Status500InternalServerError, new AnalyticsHeatmapPayloadViewModel
            {
                ErrorMessage = "Không thể tải dữ liệu heatmap động trong lần này."
            });
        }
    }

    private List<UserTracking> LoadRawTrackingRows(DateTime windowStart)
    {
        return _context.UserTrackings
            .AsNoTracking()
            .Where(x => x.RecordedAt >= windowStart)
            .OrderByDescending(x => x.RecordedAt)
            .Take(MaxTrackingRows)
            .ToList();
    }

    private static void NormalizeFilters(ref int days, ref double maxAccuracy, ref int startHour, ref int endHour)
    {
        days = Math.Clamp(days, 1, 30);
        maxAccuracy = Math.Clamp(maxAccuracy, 30, 300);
        startHour = Math.Clamp(startHour, 0, 23);
        endHour = Math.Clamp(endHour, 0, 23);
    }

    private static List<UserTracking> FilterTrackingRows(IEnumerable<UserTracking> rawTrackingRows, double maxAccuracy, int startHour, int endHour)
    {
        var candidateRows = rawTrackingRows
            .Where(x => x.Latitude != 0 && x.Longitude != 0)
            .Where(x => x.Accuracy == 0 || x.Accuracy <= maxAccuracy)
            .Where(x => IsWithinHourWindow(x.RecordedAt, startHour, endHour))
            .ToList();

        var filteredRows = new List<UserTracking>(candidateRows.Count);

        foreach (var visitorGroup in candidateRows
                     .OrderBy(x => x.RecordedAt)
                     .GroupBy(x => x.UserId ?? string.Empty))
        {
            UserTracking? previousAccepted = null;
            foreach (var point in visitorGroup)
            {
                if (point.SpeedMetersPerSecond.HasValue &&
                    point.SpeedMetersPerSecond.Value > MaxReasonableTravelSpeedMetersPerSecond)
                {
                    continue;
                }

                if (previousAccepted != null)
                {
                    var gapSeconds = Math.Max((point.RecordedAt - previousAccepted.RecordedAt).TotalSeconds, 1d);
                    var distanceMeters = CalculateDistanceMeters(
                        previousAccepted.Latitude,
                        previousAccepted.Longitude,
                        point.Latitude,
                        point.Longitude);
                    var impliedSpeed = distanceMeters / gapSeconds;

                    if (distanceMeters >= MinSuspiciousJumpDistanceMeters &&
                        impliedSpeed > MaxReasonableTravelSpeedMetersPerSecond)
                    {
                        continue;
                    }
                }

                filteredRows.Add(point);
                previousAccepted = point;
            }
        }

        return filteredRows;
    }

    private static List<VisitHistory> FilterValidVisitRows(IEnumerable<VisitHistory> rawVisitRows)
    {
        return rawVisitRows
            .Where(x => x.Duration >= MinValidVisitDurationSeconds)
            .Where(x => x.Duration <= MaxValidVisitDurationSeconds)
            .Where(x => x.EndTime >= x.StartTime)
            .ToList();
    }

    private static bool IsWithinHourWindow(DateTime recordedAt, int startHour, int endHour)
    {
        var localHour = ToAnalyticsLocalTime(recordedAt).Hour;
        if (startHour <= endHour)
        {
            return localHour >= startHour && localHour <= endHour;
        }

        return localHour >= startHour || localHour <= endHour;
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

    private static AnalyticsHeatmapPayloadViewModel BuildHeatmapPayload(
        IReadOnlyList<UserTracking> trackingRows,
        IReadOnlyDictionary<string, string> visitorAliases,
        HeatmapBounds? bounds,
        int zoom)
    {
        var cellSizeMeters = ResolveHeatCellSizeMeters(zoom);
        var heatCells = BuildHeatCells(trackingRows, cellSizeMeters)
            .OrderByDescending(x => x.UniqueVisitorCount)
            .ThenByDescending(x => x.SampleCount)
            .Take(MaxHeatCells)
            .ToList();

        var peakWeight = heatCells.Any() ? heatCells.Max(x => x.Intensity) : 1d;
        foreach (var cell in heatCells)
        {
            cell.Intensity = Math.Round(Math.Max(0.18, cell.Intensity / Math.Max(peakWeight, 1d)), 2);
        }

        var visibleVisitorPoints = trackingRows
            .GroupBy(x => x.UserId)
            .Select(group => group.OrderByDescending(x => x.RecordedAt).First())
            .OrderByDescending(x => x.RecordedAt)
            .Take(MaxVisibleVisitorPoints)
            .Select(x => new AnalyticsVisitorPointViewModel
            {
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                VisitorAlias = ResolveVisitorAlias(visitorAliases, x.UserId),
                Accuracy = x.Accuracy,
                Source = x.Source,
                RecordedAt = x.RecordedAt
            })
            .ToList();

        var payload = new AnalyticsHeatmapPayloadViewModel
        {
            Zoom = zoom,
            CellSizeMeters = cellSizeMeters,
            TotalTrackingPoint = trackingRows.Count,
            UniqueVisitors = visitorAliases.Count,
            HeatmapClusterCount = heatCells.Count,
            HeatPoints = heatCells
                .Select(x => new AnalyticsHeatPointViewModel
                {
                    Latitude = x.CenterLatitude,
                    Longitude = x.CenterLongitude,
                    SampleCount = x.SampleCount,
                    Intensity = x.Intensity
                })
                .ToList(),
            GridCells = heatCells,
            VisitorPoints = visibleVisitorPoints,
            HottestCell = heatCells.FirstOrDefault()
        };

        if (bounds.HasValue && payload.HottestCell == null)
        {
            payload.HottestCell = new AnalyticsHeatCellViewModel
            {
                CellId = "empty",
                CenterLatitude = (bounds.Value.South + bounds.Value.North) / 2d,
                CenterLongitude = (bounds.Value.West + bounds.Value.East) / 2d,
                MinLatitude = bounds.Value.South,
                MinLongitude = bounds.Value.West,
                MaxLatitude = bounds.Value.North,
                MaxLongitude = bounds.Value.East
            };
        }

        return payload;
    }

    private static IEnumerable<AnalyticsHeatCellViewModel> BuildHeatCells(IEnumerable<UserTracking> trackingRows, double cellSizeMeters)
    {
        var cells = new Dictionary<string, HeatCellAccumulator>(StringComparer.Ordinal);

        foreach (var visitorGroup in trackingRows
                     .OrderBy(x => x.RecordedAt)
                     .GroupBy(x => x.UserId ?? string.Empty))
        {
            UserTracking? previousPoint = null;

            foreach (var point in visitorGroup)
            {
                var snappedCell = SnapToGrid(point.Latitude, point.Longitude, cellSizeMeters);
                if (!cells.TryGetValue(snappedCell.CellId, out var bucket))
                {
                    bucket = new HeatCellAccumulator
                    {
                        CellId = snappedCell.CellId,
                        MinLatitude = snappedCell.MinLatitude,
                        MinLongitude = snappedCell.MinLongitude,
                        MaxLatitude = snappedCell.MaxLatitude,
                        MaxLongitude = snappedCell.MaxLongitude,
                        CenterLatitude = (snappedCell.MinLatitude + snappedCell.MaxLatitude) / 2d,
                        CenterLongitude = (snappedCell.MinLongitude + snappedCell.MaxLongitude) / 2d
                    };
                    cells[snappedCell.CellId] = bucket;
                }

                bucket.SampleCount++;
                bucket.Visitors.Add(point.UserId ?? string.Empty);

                var localHour = ToAnalyticsLocalTime(point.RecordedAt).Hour;
                bucket.HourCounts[localHour] = bucket.HourCounts.GetValueOrDefault(localHour, 0) + 1;

                if (previousPoint != null)
                {
                    var gap = point.RecordedAt - previousPoint.RecordedAt;
                    if (gap > TimeSpan.Zero &&
                        gap <= MaxDwellGap &&
                        string.Equals(bucket.CellId, SnapToGrid(previousPoint.Latitude, previousPoint.Longitude, cellSizeMeters).CellId, StringComparison.Ordinal))
                    {
                        bucket.EstimatedDwellSeconds += Math.Min(gap.TotalSeconds, 60d);
                    }
                }

                previousPoint = point;
            }
        }

        foreach (var bucket in cells.Values)
        {
            var peakHour = bucket.HourCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key).FirstOrDefault();
            var nextHour = (peakHour.Key + 1) % 24;
            var uniqueVisitorCount = bucket.Visitors.Count(id => !string.IsNullOrWhiteSpace(id));
            var weightedScore = bucket.SampleCount + (uniqueVisitorCount * 2.5d) + Math.Min(bucket.EstimatedDwellSeconds / 30d, 8d);
            yield return new AnalyticsHeatCellViewModel
            {
                CellId = bucket.CellId,
                CenterLatitude = bucket.CenterLatitude,
                CenterLongitude = bucket.CenterLongitude,
                MinLatitude = bucket.MinLatitude,
                MinLongitude = bucket.MinLongitude,
                MaxLatitude = bucket.MaxLatitude,
                MaxLongitude = bucket.MaxLongitude,
                SampleCount = bucket.SampleCount,
                UniqueVisitorCount = uniqueVisitorCount,
                EstimatedDwellSeconds = Math.Round(bucket.EstimatedDwellSeconds, 0),
                PeakHourLabel = $"{peakHour.Key:00}:00 - {nextHour:00}:00",
                PeakHourCount = peakHour.Value
                ,
                Intensity = weightedScore
            };
        }
    }

    private static GridCellBounds SnapToGrid(double latitude, double longitude, double cellSizeMeters)
    {
        var latitudeStep = cellSizeMeters / 111320d;
        var longitudeStep = cellSizeMeters / Math.Max(111320d * Math.Max(Math.Cos(DegreesToRadians(latitude)), 0.15d), 1d);

        var minLatitude = Math.Floor(latitude / latitudeStep) * latitudeStep;
        var minLongitude = Math.Floor(longitude / longitudeStep) * longitudeStep;

        return new GridCellBounds(
            $"{Math.Round(minLatitude, 5):F5}|{Math.Round(minLongitude, 5):F5}",
            Math.Round(minLatitude, 6),
            Math.Round(minLongitude, 6),
            Math.Round(minLatitude + latitudeStep, 6),
            Math.Round(minLongitude + longitudeStep, 6));
    }

    private static double ResolveHeatCellSizeMeters(int zoom)
    {
        return zoom switch
        {
            <= 12 => 90,
            13 => 70,
            14 => 48,
            15 => 30,
            16 => 18,
            17 => 12,
            _ => 8
        };
    }

    private static List<AnalyticsRouteViewModel> BuildRouteSessions(
        IEnumerable<UserTracking> trackingRows,
        IReadOnlyDictionary<string, string> visitorAliases)
    {
        var sessions = new List<AnalyticsRouteViewModel>();

        foreach (var visitorGroup in trackingRows
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

                var gapSeconds = Math.Max(gap.TotalSeconds, 1d);
                var impliedSpeed = distance / gapSeconds;

                if (gap > TimeSpan.FromMinutes(12) || distance > 600 || impliedSpeed > MaxReasonableTravelSpeedMetersPerSecond)
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
        if (points.Count < MinRouteSessionPointCount)
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
                Label = $"{alias} · {ToAnalyticsLocalTime(point.RecordedAt):dd/MM HH:mm}"
            }).ToList()
        });
    }

    private static DateTime ToAnalyticsLocalTime(DateTime value)
    {
        var normalized = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(normalized, AnalyticsTimeZone);
    }

    private static TimeZoneInfo ResolveAnalyticsTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }
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

    private sealed class HeatCellAccumulator
    {
        public string CellId { get; set; } = "";
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double MinLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MaxLongitude { get; set; }
        public int SampleCount { get; set; }
        public HashSet<string> Visitors { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, int> HourCounts { get; } = new();
        public double EstimatedDwellSeconds { get; set; }
    }

    private readonly record struct GridCellBounds(string CellId, double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude);
    private readonly record struct HeatmapBounds(double South, double West, double North, double East);
}
