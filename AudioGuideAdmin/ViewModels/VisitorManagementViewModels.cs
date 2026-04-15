using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.ViewModels;

public class VisitorIndexViewModel
{
    public int TotalVisitors { get; set; }
    public int ActiveVisitors { get; set; }
    public int InactiveVisitors { get; set; }
    public int ActiveThresholdMinutes { get; set; } = 5;
    public bool IncludeTestData { get; set; }
    public int HiddenTestVisitors { get; set; }
    public List<VisitorSummaryViewModel> Visitors { get; set; } = new();
}

public class VisitorSummaryViewModel
{
    public VisitorProfile Visitor { get; set; } = new();
    public int TrackingCount { get; set; }
    public int VisitCount { get; set; }
    public int TriggerCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsSyntheticData { get; set; }
    public string ActivityStatusText => IsActive ? "Đang hoạt động" : "Không hoạt động";
    public string LastSeenDisplayText { get; set; } = "";
    public string LastSeenAgoText { get; set; } = "";
}
