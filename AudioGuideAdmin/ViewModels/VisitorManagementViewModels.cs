using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.ViewModels;

public class VisitorSummaryViewModel
{
    public VisitorProfile Visitor { get; set; } = new();
    public int TrackingCount { get; set; }
    public int VisitCount { get; set; }
    public int TriggerCount { get; set; }
}
