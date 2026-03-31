using Microsoft.AspNetCore.Mvc.Rendering;

namespace AudioGuideAdmin.ViewModels;

public class TrackingViewModel
{
    public string? UserId { get; set; }
    public int? PoiId { get; set; }
    public string? TriggerType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public int TrackingCount { get; set; }
    public int VisitCount { get; set; }
    public int TriggerCount { get; set; }
    public int UniqueVisitorCount { get; set; }

    public List<SelectListItem> PoiOptions { get; set; } = new();
    public List<SelectListItem> TriggerTypeOptions { get; set; } = new();

    public List<TrackingPointViewModel> TrackingPoints { get; set; } = new();
    public List<VisitRowViewModel> Visits { get; set; } = new();
    public List<TriggerRowViewModel> Triggers { get; set; } = new();
}

public class TrackingPointViewModel
{
    public string UserId { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public string Source { get; set; } = "";
    public bool IsForeground { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class VisitRowViewModel
{
    public string UserId { get; set; } = "";
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public string Language { get; set; } = "";
    public int Duration { get; set; }
    public string TriggerType { get; set; } = "";
    public bool WasAutoPlayed { get; set; }
    public bool WasCompleted { get; set; }
    public DateTime EndTime { get; set; }
}

public class TriggerRowViewModel
{
    public string UserId { get; set; } = "";
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public string TriggerType { get; set; } = "";
    public string Status { get; set; } = "";
    public double DistanceMeters { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime CooldownUntil { get; set; }
}
