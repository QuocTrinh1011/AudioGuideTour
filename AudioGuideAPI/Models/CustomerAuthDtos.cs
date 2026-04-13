namespace AudioGuideAPI.Models;

public class CustomerLoginRequest
{
    public string Identifier { get; set; } = "";
    public string Password { get; set; } = "";
}

public class CustomerSessionResponse
{
    public string AccountId { get; set; } = "";
    public string RegistrationId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string PreferredLanguage { get; set; } = "vi-VN";
    public string SessionToken { get; set; } = "";
    public DateTime? SessionExpiresAt { get; set; }
    public bool IsPaid { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = "";
}

public class CustomerLogoutRequest
{
    public string AccountId { get; set; } = "";
    public string SessionToken { get; set; } = "";
}
