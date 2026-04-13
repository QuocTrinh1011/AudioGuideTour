using AudioGuideAPI.Data;
using AudioGuideAPI.Helpers;
using AudioGuideAPI.Models;
using AudioGuideAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private static readonly HashSet<string> SupportedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi-VN",
        "en-US",
        "zh-CN"
    };

    private readonly AppDbContext _context;
    private readonly PayOsClient _payOsClient;

    public RegistrationController(AppDbContext context, PayOsClient payOsClient)
    {
        _context = context;
        _payOsClient = payOsClient;
    }

    [HttpGet("bootstrap")]
    public async Task<ActionResult<RegistrationBootstrapResponse>> Bootstrap([FromQuery] string? visitorId, [FromQuery] string? deviceId, CancellationToken cancellationToken)
    {
        var plans = await _context.RegistrationPlans
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Price)
            .ToListAsync(cancellationToken);

        var registration = await FindLatestRegistrationAsync(visitorId, deviceId, cancellationToken);

        return Ok(new RegistrationBootstrapResponse
        {
            Plans = plans.Select(MapPlan).ToList(),
            ActiveRegistration = registration == null ? null : MapRegistration(registration)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RegistrationStatusResponse>> GetById(string id, [FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
    {
        var registration = await LoadRegistrationAsync(id, cancellationToken);
        if (registration == null)
        {
            return NotFound();
        }

        if (refresh)
        {
            await RefreshPaymentStatusInternalAsync(registration, cancellationToken);
        }

        return Ok(MapRegistration(registration));
    }

    [HttpPost("form")]
    public async Task<ActionResult<RegistrationStatusResponse>> SubmitForm([FromBody] SubmitRegistrationFormRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest("Vui lòng nhập họ và tên.");
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest("Vui lòng nhập số điện thoại.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Vui lòng nhập email.");
        }

        var visitorId = request.VisitorId?.Trim() ?? string.Empty;
        var deviceId = request.DeviceId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(visitorId) && string.IsNullOrWhiteSpace(deviceId))
        {
            return BadRequest("Thiếu thông tin visitor để tạo hồ sơ đăng ký.");
        }

        var registration = await FindEditableRegistrationAsync(visitorId, deviceId, cancellationToken);
        if (registration == null)
        {
            registration = new MembershipRegistration
            {
                Id = Guid.NewGuid().ToString("N"),
                ReturnToken = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            };
            _context.MembershipRegistrations.Add(registration);
        }

        registration.VisitorId = string.IsNullOrWhiteSpace(visitorId) ? registration.VisitorId : visitorId;
        registration.DeviceId = string.IsNullOrWhiteSpace(deviceId) ? registration.DeviceId : deviceId;
        registration.FullName = request.FullName.Trim();
        registration.Phone = request.Phone.Trim();
        registration.Email = request.Email.Trim();
        registration.PreferredLanguage = NormalizeLanguage(request.PreferredLanguage);
        registration.Source = string.IsNullOrWhiteSpace(request.Source) ? "mobile" : request.Source.Trim().ToLowerInvariant();
        registration.Note = request.Note?.Trim() ?? string.Empty;
        registration.FormSubmittedAt = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(registration.Status) || registration.Status is "pending-form" or "cancelled" or "failed")
        {
            registration.Status = "pending-plan";
        }

        if (string.IsNullOrWhiteSpace(registration.PaymentStatus))
        {
            registration.PaymentStatus = "FORM_ONLY";
        }

        await _context.SaveChangesAsync(cancellationToken);

        registration = await LoadRegistrationAsync(registration.Id, cancellationToken) ?? registration;
        return Ok(MapRegistration(registration));
    }

    [HttpPost("{id}/payment")]
    public async Task<ActionResult<RegistrationStatusResponse>> CreatePayment(string id, [FromBody] CreateRegistrationPaymentRequest request, CancellationToken cancellationToken)
    {
        var registration = await LoadRegistrationAsync(id, cancellationToken);
        if (registration == null)
        {
            return NotFound();
        }

        if (!_payOsClient.IsConfigured)
        {
            return BadRequest("API chưa được cấu hình PayOS.");
        }

        var plan = await _context.RegistrationPlans
            .FirstOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive, cancellationToken);
        if (plan == null)
        {
            return BadRequest("Không tìm thấy gói đăng ký phù hợp.");
        }

        if (plan.Price < 20_000)
        {
            return BadRequest("Giá gói đăng ký phải từ 20.000đ trở lên.");
        }

        var callbackBaseUrl = ResolveCallbackBaseUrl(request.CallbackBaseUrl);
        if (string.IsNullOrWhiteSpace(callbackBaseUrl))
        {
            return BadRequest("Không xác định được địa chỉ callback để nhận kết quả thanh toán.");
        }

        var orderCode = await GenerateUniqueOrderCodeAsync(cancellationToken);
        var returnUrl = $"{callbackBaseUrl}/api/registration/payment-return?registrationId={Uri.EscapeDataString(registration.Id)}&token={Uri.EscapeDataString(registration.ReturnToken)}";
        var cancelUrl = $"{callbackBaseUrl}/api/registration/payment-cancel?registrationId={Uri.EscapeDataString(registration.Id)}&token={Uri.EscapeDataString(registration.ReturnToken)}";
        var description = BuildPaymentDescription(registration.Id, orderCode);
        var expiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

        var payment = await _payOsClient.CreatePaymentLinkAsync(
            new PayOsCreatePaymentRequest(
                orderCode,
                plan.Price,
                description,
                registration.FullName,
                registration.Email,
                registration.Phone,
                cancelUrl,
                returnUrl,
                expiredAt,
                new[]
                {
                    new PayOsPaymentItem(plan.Name, 1, plan.Price, "goi")
                }),
            cancellationToken);

        registration.RegistrationPlanId = plan.Id;
        registration.Amount = plan.Price;
        registration.Currency = plan.Currency;
        registration.OrderCode = payment.OrderCode;
        registration.PaymentLinkId = payment.PaymentLinkId;
        registration.CheckoutUrl = payment.CheckoutUrl;
        registration.QrCode = payment.QrCode;
        registration.PaymentStartedAt = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;
        registration.LastSyncedAt = DateTime.UtcNow;
        ApplyPaymentState(registration, payment.Status);

        await _context.SaveChangesAsync(cancellationToken);

        registration = await LoadRegistrationAsync(registration.Id, cancellationToken) ?? registration;
        return Ok(MapRegistration(registration));
    }

    [HttpPost("{id}/refresh")]
    public async Task<ActionResult<RegistrationStatusResponse>> RefreshPaymentStatus(string id, CancellationToken cancellationToken)
    {
        var registration = await LoadRegistrationAsync(id, cancellationToken);
        if (registration == null)
        {
            return NotFound();
        }

        await RefreshPaymentStatusInternalAsync(registration, cancellationToken);
        return Ok(MapRegistration(registration));
    }

    [HttpPost("payos-webhook")]
    public async Task<IActionResult> PayOsWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        if (!_payOsClient.IsConfigured)
        {
            return Ok(new { code = "00", desc = "ignored" });
        }

        if (!payload.TryGetProperty("data", out var dataElement) ||
            !payload.TryGetProperty("signature", out var signatureElement))
        {
            return BadRequest(new { code = "01", desc = "Thiếu dữ liệu webhook." });
        }

        var signature = signatureElement.GetString() ?? string.Empty;
        if (!PayOsSignatureHelper.IsValidWebhookSignature(dataElement, signature, _payOsClient.ChecksumKey))
        {
            return Unauthorized(new { code = "01", desc = "Signature không hợp lệ." });
        }

        if (!TryReadOrderCode(dataElement, out var orderCode))
        {
            return BadRequest(new { code = "01", desc = "Thiếu orderCode." });
        }

        var registration = await _context.MembershipRegistrations
            .Include(x => x.RegistrationPlan)
            .FirstOrDefaultAsync(x => x.OrderCode == orderCode, cancellationToken);
        if (registration == null)
        {
            return Ok(new { code = "00", desc = "ignored" });
        }

        registration.PaymentLinkId = TryReadString(dataElement, "paymentLinkId") ?? registration.PaymentLinkId;

        var webhookPaymentCode = TryReadString(dataElement, "code");
        var webhookStatus = string.Equals(webhookPaymentCode, "00", StringComparison.OrdinalIgnoreCase)
            ? "PAID"
            : TryReadString(dataElement, "status") ?? registration.PaymentStatus;

        ApplyPaymentState(registration, webhookStatus);
        registration.UpdatedAt = DateTime.UtcNow;
        registration.LastSyncedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { code = "00", desc = "success" });
    }

    [HttpGet("payment-return")]
    public async Task<IActionResult> PaymentReturn(
        [FromQuery] string registrationId,
        [FromQuery] string token,
        [FromQuery] string? status,
        [FromQuery] long? orderCode,
        [FromQuery] string? id,
        CancellationToken cancellationToken)
    {
        var registration = await LoadRegistrationAsync(registrationId, cancellationToken);
        if (registration == null || !string.Equals(registration.ReturnToken, token, StringComparison.Ordinal))
        {
            return Content(BuildCallbackHtml("Không tìm thấy hồ sơ đăng ký.", "Vui lòng quay lại ứng dụng để kiểm tra lại trạng thái.", false, null), "text/html; charset=utf-8");
        }

        if (orderCode.HasValue && !registration.OrderCode.HasValue)
        {
            registration.OrderCode = orderCode.Value;
        }

        if (!string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(registration.PaymentLinkId))
        {
            registration.PaymentLinkId = id;
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            ApplyPaymentState(registration, status);
            registration.UpdatedAt = DateTime.UtcNow;
        }

        await RefreshPaymentStatusInternalAsync(registration, cancellationToken, swallowErrors: true);

        var success = string.Equals(registration.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);
        var title = success ? "Đăng ký thành công" : "Đang chờ xác nhận thanh toán";
        var message = success
            ? "Hệ thống đã ghi nhận gói đăng ký của bạn. Bạn có thể quay lại ứng dụng để tiếp tục."
            : "payOS đã quay lại trang xác nhận, nhưng hệ thống vẫn đang chờ trạng thái cuối cùng. Bạn có thể mở lại app và bấm kiểm tra.";

        return Content(BuildCallbackHtml(title, message, success, BuildAppDeepLink(registration.Id, registration.PaymentStatus)), "text/html; charset=utf-8");
    }

    [HttpGet("payment-cancel")]
    public async Task<IActionResult> PaymentCancel(
        [FromQuery] string registrationId,
        [FromQuery] string token,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var registration = await LoadRegistrationAsync(registrationId, cancellationToken);
        if (registration == null || !string.Equals(registration.ReturnToken, token, StringComparison.Ordinal))
        {
            return Content(BuildCallbackHtml("Đã hủy thanh toán", "Không tìm thấy hồ sơ đăng ký tương ứng.", false, null), "text/html; charset=utf-8");
        }

        ApplyPaymentState(registration, string.IsNullOrWhiteSpace(status) ? "CANCELLED" : status);
        registration.UpdatedAt = DateTime.UtcNow;
        registration.CancelledAt ??= DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Content(
            BuildCallbackHtml(
                "Đã hủy thanh toán",
                "Bạn có thể quay lại ứng dụng để chọn lại gói đăng ký hoặc thanh toán lại sau.",
                false,
                BuildAppDeepLink(registration.Id, registration.PaymentStatus)),
            "text/html; charset=utf-8");
    }

    private async Task RefreshPaymentStatusInternalAsync(MembershipRegistration registration, CancellationToken cancellationToken, bool swallowErrors = false)
    {
        if (!_payOsClient.IsConfigured || (!registration.OrderCode.HasValue && string.IsNullOrWhiteSpace(registration.PaymentLinkId)))
        {
            return;
        }

        try
        {
            var lookupId = registration.OrderCode?.ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(lookupId))
            {
                lookupId = registration.PaymentLinkId;
            }

            if (string.IsNullOrWhiteSpace(lookupId))
            {
                return;
            }

            var paymentInfo = await _payOsClient.GetPaymentLinkInfoAsync(lookupId, cancellationToken);
            registration.PaymentLinkId = string.IsNullOrWhiteSpace(paymentInfo.PaymentLinkId) ? registration.PaymentLinkId : paymentInfo.PaymentLinkId;
            registration.OrderCode = paymentInfo.OrderCode;
            registration.Amount = paymentInfo.Amount > 0 ? paymentInfo.Amount : registration.Amount;
            registration.LastSyncedAt = DateTime.UtcNow;
            registration.UpdatedAt = DateTime.UtcNow;
            ApplyPaymentState(registration, paymentInfo.Status);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (!swallowErrors)
            {
                throw;
            }
        }
    }

    private async Task<MembershipRegistration?> LoadRegistrationAsync(string id, CancellationToken cancellationToken)
    {
        return await _context.MembershipRegistrations
            .Include(x => x.RegistrationPlan)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private async Task<MembershipRegistration?> FindLatestRegistrationAsync(string? visitorId, string? deviceId, CancellationToken cancellationToken)
    {
        var normalizedVisitorId = visitorId?.Trim() ?? string.Empty;
        var normalizedDeviceId = deviceId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedVisitorId) && string.IsNullOrWhiteSpace(normalizedDeviceId))
        {
            return null;
        }

        return await _context.MembershipRegistrations
            .Include(x => x.RegistrationPlan)
            .Where(x =>
                (!string.IsNullOrWhiteSpace(normalizedVisitorId) && x.VisitorId == normalizedVisitorId) ||
                (!string.IsNullOrWhiteSpace(normalizedDeviceId) && x.DeviceId == normalizedDeviceId))
            .OrderByDescending(x => x.PaidAt ?? x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<MembershipRegistration?> FindEditableRegistrationAsync(string visitorId, string deviceId, CancellationToken cancellationToken)
    {
        return await _context.MembershipRegistrations
            .Include(x => x.RegistrationPlan)
            .Where(x =>
                (!string.IsNullOrWhiteSpace(visitorId) && x.VisitorId == visitorId) ||
                (!string.IsNullOrWhiteSpace(deviceId) && x.DeviceId == deviceId))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Status != "paid", cancellationToken);
    }

    private async Task<long> GenerateUniqueOrderCodeAsync(CancellationToken cancellationToken)
    {
        var seed = DateTime.UtcNow;
        var candidate = ((seed.Year % 100) * 10_000_000L) + (seed.DayOfYear * 10_000L) + (seed.Hour * 100L) + seed.Minute;
        while (await _context.MembershipRegistrations.AnyAsync(x => x.OrderCode == candidate, cancellationToken))
        {
            candidate++;
        }

        return candidate;
    }

    private string ResolveCallbackBaseUrl(string? requestedBaseUrl)
    {
        if (TryNormalizeAbsoluteBaseUrl(requestedBaseUrl, out var explicitUrl))
        {
            return explicitUrl;
        }

        var current = $"{Request.Scheme}://{Request.Host}";
        if (TryNormalizeAbsoluteBaseUrl(current, out var requestUrl))
        {
            return requestUrl;
        }

        if (TryNormalizeAbsoluteBaseUrl(_payOsClient.DefaultCallbackBaseUrl, out var defaultUrl))
        {
            return defaultUrl;
        }

        return string.Empty;
    }

    private static bool TryNormalizeAbsoluteBaseUrl(string? input, out string result)
    {
        result = string.Empty;
        if (string.IsNullOrWhiteSpace(input) || !Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        result = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return uri.Scheme is "http" or "https";
    }

    private static string BuildPaymentDescription(string registrationId, long orderCode)
    {
        var suffix = string.IsNullOrWhiteSpace(registrationId)
            ? orderCode.ToString(CultureInfo.InvariantCulture)
            : registrationId[^Math.Min(6, registrationId.Length)..].ToUpperInvariant();
        return $"DK{suffix}"[..Math.Min(8, $"DK{suffix}".Length)];
    }

    private string BuildAppDeepLink(string registrationId, string paymentStatus)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_payOsClient.AppDeepLinkBase)
            ? "audiotour://registration/result"
            : _payOsClient.AppDeepLinkBase.TrimEnd('/');

        return $"{baseUrl}?registrationId={Uri.EscapeDataString(registrationId)}&status={Uri.EscapeDataString(paymentStatus)}";
    }

    private static void ApplyPaymentState(MembershipRegistration registration, string? status)
    {
        var normalizedStatus = (status ?? string.Empty).Trim().ToUpperInvariant();
        registration.PaymentStatus = string.IsNullOrWhiteSpace(normalizedStatus) ? registration.PaymentStatus : normalizedStatus;

        switch (registration.PaymentStatus)
        {
            case "PAID":
                registration.Status = "paid";
                registration.PaidAt ??= DateTime.UtcNow;
                registration.CancelledAt = null;
                break;
            case "CANCELLED":
                registration.Status = "cancelled";
                registration.CancelledAt ??= DateTime.UtcNow;
                break;
            case "PROCESSING":
            case "PENDING":
                registration.Status = "pending-payment";
                break;
            case "FORM_ONLY":
                registration.Status = "pending-plan";
                break;
            default:
                if (registration.Status != "paid")
                {
                    registration.Status = "pending-payment";
                }
                break;
        }
    }

    private static bool TryReadOrderCode(JsonElement dataElement, out long orderCode)
    {
        orderCode = 0;
        if (!dataElement.TryGetProperty("orderCode", out var orderCodeElement))
        {
            return false;
        }

        if (orderCodeElement.ValueKind == JsonValueKind.Number)
        {
            return orderCodeElement.TryGetInt64(out orderCode);
        }

        if (orderCodeElement.ValueKind == JsonValueKind.String)
        {
            return long.TryParse(orderCodeElement.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out orderCode);
        }

        return false;
    }

    private static string? TryReadString(JsonElement dataElement, string propertyName)
    {
        if (!dataElement.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "vi-VN";
        }

        var normalized = language.Trim().Replace('_', '-');
        return SupportedCodes.Contains(normalized) ? normalized : "vi-VN";
    }

    private static RegistrationPlanDto MapPlan(RegistrationPlan plan) => new()
    {
        Id = plan.Id,
        Code = plan.Code,
        Name = plan.Name,
        Description = plan.Description,
        HighlightText = plan.HighlightText,
        Price = plan.Price,
        DurationDays = plan.DurationDays,
        Currency = plan.Currency
    };

    private static RegistrationStatusResponse MapRegistration(MembershipRegistration registration) => new()
    {
        Id = registration.Id,
        VisitorId = registration.VisitorId,
        DeviceId = registration.DeviceId,
        FullName = registration.FullName,
        Phone = registration.Phone,
        Email = registration.Email,
        PreferredLanguage = registration.PreferredLanguage,
        Source = registration.Source,
        Status = registration.Status,
        PaymentStatus = registration.PaymentStatus,
        Amount = registration.Amount,
        Currency = registration.Currency,
        OrderCode = registration.OrderCode,
        PaymentLinkId = registration.PaymentLinkId,
        CheckoutUrl = registration.CheckoutUrl,
        QrCode = registration.QrCode,
        Note = registration.Note,
        CreatedAt = registration.CreatedAt,
        UpdatedAt = registration.UpdatedAt,
        PaidAt = registration.PaidAt,
        Plan = registration.RegistrationPlan == null ? null : MapPlan(registration.RegistrationPlan),
        IsSuccessful = string.Equals(registration.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase)
    };

    private static string BuildCallbackHtml(string title, string message, bool success, string? appDeepLink)
    {
        var safeTitle = System.Net.WebUtility.HtmlEncode(title);
        var safeMessage = System.Net.WebUtility.HtmlEncode(message);
        var safeLink = string.IsNullOrWhiteSpace(appDeepLink) ? string.Empty : System.Net.WebUtility.HtmlEncode(appDeepLink);
        var accent = success ? "#1b8a5a" : "#c77d1b";
        var buttonText = success ? "Mở lại ứng dụng" : "Quay lại ứng dụng";
        var autoOpenScript = string.IsNullOrWhiteSpace(appDeepLink)
            ? string.Empty
            : $"<script>setTimeout(function(){{window.location.href='{safeLink}';}}, 600);</script>";

        return $$"""
        <!DOCTYPE html>
        <html lang="vi">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>{{safeTitle}}</title>
            <style>
                body { font-family: 'Segoe UI', sans-serif; background: #f5f7fb; margin: 0; padding: 24px; color: #17324d; }
                .card { max-width: 560px; margin: 48px auto; background: #fff; border-radius: 24px; padding: 28px; box-shadow: 0 20px 60px rgba(23,50,77,.12); }
                .pill { display: inline-block; padding: 8px 14px; border-radius: 999px; background: {{accent}}1A; color: {{accent}}; font-weight: 700; margin-bottom: 16px; }
                h1 { margin: 0 0 12px; font-size: 28px; }
                p { color: #4f657c; line-height: 1.6; }
                a.btn { display: inline-block; margin-top: 14px; padding: 12px 18px; border-radius: 14px; background: {{accent}}; color: #fff; text-decoration: none; font-weight: 700; }
                .hint { margin-top: 16px; font-size: 13px; color: #8395a7; }
            </style>
        </head>
        <body>
            <div class="card">
                <div class="pill">{{(success ? "Đăng ký thành công" : "Thanh toán đang chờ")}}</div>
                <h1>{{safeTitle}}</h1>
                <p>{{safeMessage}}</p>
                {{(string.IsNullOrWhiteSpace(safeLink) ? "" : $"<a class=\"btn\" href=\"{safeLink}\">{buttonText}</a>")}}
                <div class="hint">Nếu ứng dụng chưa tự mở, hãy dùng nút phía trên để quay lại app.</div>
            </div>
            {{autoOpenScript}}
        </body>
        </html>
        """;
    }
}
