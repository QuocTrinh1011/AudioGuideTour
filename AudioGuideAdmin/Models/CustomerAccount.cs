namespace AudioGuideAdmin.Models;

public class CustomerAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RegistrationId { get; set; } = "";
    public MembershipRegistration? Registration { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string PreferredLanguage { get; set; } = "vi-VN";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsPaid { get; set; }
    public string Status { get; set; } = "pending-payment";
    public string SessionToken { get; set; } = "";
    public DateTime? SessionExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
