namespace AudioGuideAPI.Models;

public class SubmitRegistrationFormRequest
{
    public string VisitorId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string PreferredLanguage { get; set; } = "vi-VN";
    public string Source { get; set; } = "mobile";
    public string Note { get; set; } = "";
}

public class CreateRegistrationPaymentRequest
{
    public int PlanId { get; set; }
    public string CallbackBaseUrl { get; set; } = "";
}

public class RegistrationPlanDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string HighlightText { get; set; } = "";
    public int Price { get; set; }
    public int DurationDays { get; set; }
    public string Currency { get; set; } = "VND";
}

public class RegistrationStatusResponse
{
    public string Id { get; set; } = "";
    public string VisitorId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string PreferredLanguage { get; set; } = "vi-VN";
    public string Source { get; set; } = "mobile";
    public string Status { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public int Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public long? OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = "";
    public string CheckoutUrl { get; set; } = "";
    public string QrCode { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public RegistrationPlanDto? Plan { get; set; }
    public bool IsSuccessful { get; set; }
}

public class RegistrationBootstrapResponse
{
    public List<RegistrationPlanDto> Plans { get; set; } = new();
    public RegistrationStatusResponse? ActiveRegistration { get; set; }
}
