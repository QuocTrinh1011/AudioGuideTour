using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.ViewModels;

public class CustomerIndexViewModel
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public List<CustomerSummaryViewModel> Items { get; set; } = new();
}

public class CustomerSummaryViewModel
{
    public CustomerAccount Account { get; set; } = new();
    public string PlanName { get; set; } = "Chưa chọn gói";
    public string RegistrationStatus { get; set; } = "";
}

public class CustomerDetailViewModel
{
    public CustomerAccount Account { get; set; } = new();
    public MembershipRegistration? Registration { get; set; }
    public string PlanName { get; set; } = "Chưa chọn gói";
}
