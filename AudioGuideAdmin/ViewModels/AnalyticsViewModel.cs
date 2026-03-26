namespace AudioGuideAdmin.ViewModels;

public class AnalyticsViewModel
{
    public int TotalTrackingPoint { get; set; }
    public int UniqueVisitors { get; set; }
    public int TotalTrigger { get; set; }
    public double AverageListenDuration { get; set; }
    public double CompletionRate { get; set; }
    public double AutoPlayRate { get; set; }
    public List<AnalyticsTopPoiViewModel> TopPois { get; set; } = new();
    public List<DailyListenViewModel> DailyListens { get; set; } = new();
    public List<MapPointViewModel> HeatPoints { get; set; } = new();
    public List<TrackingRouteViewModel> Routes { get; set; } = new();
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
}

public class TrackingRouteViewModel
{
    public string UserId { get; set; } = "";
    public List<MapPointViewModel> Points { get; set; } = new();
}

public class TriggerLogViewModel
{
    public string UserId { get; set; } = "";
    public string PoiName { get; set; } = "";
    public string TriggerType { get; set; } = "";
    public string Status { get; set; } = "";
    public double DistanceMeters { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class RecentVisitAnalyticsViewModel
{
    public string UserId { get; set; } = "";
    public string PoiName { get; set; } = "";
    public int Duration { get; set; }
    public string Language { get; set; } = "";
    public bool WasAutoPlayed { get; set; }
    public DateTime EndTime { get; set; }
}
