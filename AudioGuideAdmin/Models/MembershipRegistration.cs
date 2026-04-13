namespace AudioGuideAdmin.Models;

public class MembershipRegistration
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string VisitorId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string PreferredLanguage { get; set; } = "vi-VN";
    public string Source { get; set; } = "mobile";
    public string Status { get; set; } = "pending-form";
    public string PaymentStatus { get; set; } = "FORM_ONLY";
    public int? RegistrationPlanId { get; set; }
    public RegistrationPlan? RegistrationPlan { get; set; }
    public int Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public long? OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = "";
    public string CheckoutUrl { get; set; } = "";
    public string QrCode { get; set; } = "";
    public string ReturnToken { get; set; } = "";
    public string Note { get; set; } = "";
    public string AdminNote { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FormSubmittedAt { get; set; }
    public DateTime? PaymentStartedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}
