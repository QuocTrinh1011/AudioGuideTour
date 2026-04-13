namespace AudioGuideAdmin.Models;

public class RegistrationPlan
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string HighlightText { get; set; } = "";
    public int Price { get; set; }
    public int DurationDays { get; set; }
    public string Currency { get; set; } = "VND";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<MembershipRegistration> Registrations { get; set; } = new List<MembershipRegistration>();
}
