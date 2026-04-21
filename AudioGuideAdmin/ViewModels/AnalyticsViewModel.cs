namespace AudioGuideAdmin.ViewModels;

public class AnalyticsViewModel
{
    public string? ErrorMessage { get; set; }
    public string TrackingWindowLabel { get; set; } = "";
    public int SelectedWindowDays { get; set; } = 7;
    public double SelectedMaxAccuracyMeters { get; set; } = 120;
    public int SelectedStartHour { get; set; }
    public int SelectedEndHour { get; set; } = 23;
    public int TotalTrackingPoint { get; set; }
    public int RawTrackingPoint { get; set; }
    public int FilteredOutTrackingPoint { get; set; }
    public int UniqueVisitors { get; set; }
    public int TotalTrigger { get; set; }
    public int HeatmapClusterCount { get; set; }
    public int RouteSessionCount { get; set; }
    public double AverageListenDuration { get; set; }
    public double CompletionRate { get; set; }
    public double AutoPlayRate { get; set; }
    public double InitialHeatCellSizeMeters { get; set; }
    public List<AnalyticsTopPoiViewModel> TopPois { get; set; } = new();
    public List<AnalyticsPoiMapViewModel> TopPoiMapPoints { get; set; } = new();
    public List<AnalyticsHeatPointViewModel> HeatmapPoints { get; set; } = new();
    public List<AnalyticsHeatCellViewModel> HeatmapCells { get; set; } = new();
    public AnalyticsHeatCellViewModel? HottestCell { get; set; }
    public List<AnalyticsVisitorPointViewModel> VisitorPoints { get; set; } = new();
    public List<AnalyticsRouteViewModel> RouteSessions { get; set; } = new();
    public List<DailyListenViewModel> DailyListens { get; set; } = new();
    public List<TriggerLogViewModel> RecentTriggers { get; set; } = new();
    public List<RecentVisitAnalyticsViewModel> RecentVisits { get; set; } = new();
}

public class AnalyticsTopPoiViewModel
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public int ListenCount { get; set; }
    public double AverageDuration { get; set; }
    public double AutoPlayRate { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class AnalyticsPoiMapViewModel
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public int ListenCount { get; set; }
    public double AverageDuration { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class DailyListenViewModel
{
    public DateTime Date { get; set; }
    public int ListenCount { get; set; }
    public double AverageDuration { get; set; }
}

public class MapPointViewModel
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Label { get; set; } = "";
    public DateTime RecordedAt { get; set; }
    public double Accuracy { get; set; }
}

public class AnalyticsHeatPointViewModel
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int SampleCount { get; set; }
    public double Intensity { get; set; }
}

public class AnalyticsHeatCellViewModel
{
    public string CellId { get; set; } = "";
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double MinLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MaxLongitude { get; set; }
    public int SampleCount { get; set; }
    public int UniqueVisitorCount { get; set; }
    public double EstimatedDwellSeconds { get; set; }
    public string PeakHourLabel { get; set; } = "";
    public int PeakHourCount { get; set; }
    public double Intensity { get; set; }
}

public class AnalyticsVisitorPointViewModel
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string VisitorAlias { get; set; } = "";
    public double Accuracy { get; set; }
    public string Source { get; set; } = "";
    public DateTime RecordedAt { get; set; }
}

public class AnalyticsHeatmapPayloadViewModel
{
    public string? ErrorMessage { get; set; }
    public int Zoom { get; set; }
    public double CellSizeMeters { get; set; }
    public int TotalTrackingPoint { get; set; }
    public int UniqueVisitors { get; set; }
    public int HeatmapClusterCount { get; set; }
    public List<AnalyticsHeatPointViewModel> HeatPoints { get; set; } = new();
    public List<AnalyticsHeatCellViewModel> GridCells { get; set; } = new();
    public List<AnalyticsVisitorPointViewModel> VisitorPoints { get; set; } = new();
    public AnalyticsHeatCellViewModel? HottestCell { get; set; }
}

public class AnalyticsRouteViewModel
{
    public string VisitorAlias { get; set; } = "";
    public string Color { get; set; } = "#17324d";
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int PointCount { get; set; }
    public List<MapPointViewModel> Points { get; set; } = new();
}

public class TriggerLogViewModel
{
    public string VisitorAlias { get; set; } = "";
    public string PoiName { get; set; } = "";
    public string TriggerType { get; set; } = "";
    public string Status { get; set; } = "";
    public double DistanceMeters { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class RecentVisitAnalyticsViewModel
{
    public string VisitorAlias { get; set; } = "";
    public string PoiName { get; set; } = "";
    public int Duration { get; set; }
    public string Language { get; set; } = "";
    public string TriggerType { get; set; } = "";
    public bool WasAutoPlayed { get; set; }
    public DateTime EndTime { get; set; }
}
