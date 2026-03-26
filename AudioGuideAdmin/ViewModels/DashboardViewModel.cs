namespace AudioGuideAdmin.ViewModels;

public class DashboardViewModel
{
    public int TotalPoi { get; set; }
    public int TotalVisit { get; set; }
    public int TotalTrackingPoint { get; set; }
    public int TotalTour { get; set; }
    public int UniqueVisitors { get; set; }
    public double AverageListenDuration { get; set; }
    public List<TopPoiViewModel> TopPois { get; set; } = new();
    public List<RecentTriggerViewModel> RecentTriggers { get; set; } = new();
}

public class TopPoiViewModel
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public int ListenCount { get; set; }
    public double AverageDuration { get; set; }
}

public class RecentTriggerViewModel
{
    public string UserId { get; set; } = "";
    public string PoiName { get; set; } = "";
    public string TriggerType { get; set; } = "";
    public DateTime RecordedAt { get; set; }
    public double DistanceMeters { get; set; }
}
