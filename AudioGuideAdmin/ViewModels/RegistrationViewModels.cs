using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.ViewModels;

public class RegistrationIndexViewModel
{
    public string StatusFilter { get; set; } = "";
    public int TotalCount { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public int CancelledCount { get; set; }
    public List<RegistrationSummaryViewModel> Items { get; set; } = new();
}

public class RegistrationSummaryViewModel
{
    public MembershipRegistration Registration { get; set; } = new();
    public string PlanName { get; set; } = "Chưa chọn gói";
    public string StatusLabel { get; set; } = "";
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public bool IsSuccessful { get; set; }
}

public class RegistrationDetailViewModel
{
    public MembershipRegistration Registration { get; set; } = new();
    public string PlanName { get; set; } = "Chưa chọn gói";
    public string StatusLabel { get; set; } = "";
    public string StatusBadgeClass { get; set; } = "bg-secondary";
}
