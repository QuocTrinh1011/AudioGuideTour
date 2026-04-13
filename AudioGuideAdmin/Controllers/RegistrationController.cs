using AudioGuideAdmin.Data;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class RegistrationController : Controller
{
    private readonly AppDbContext _context;

    public RegistrationController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var query = _context.MembershipRegistrations
            .AsNoTracking()
            .Include(x => x.RegistrationPlan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status.ToLower() == normalizedStatus || x.PaymentStatus.ToLower() == normalizedStatus);
        }

        var rows = await query
            .OrderByDescending(x => x.PaidAt ?? x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        var allRows = await _context.MembershipRegistrations.AsNoTracking().ToListAsync();

        var model = new RegistrationIndexViewModel
        {
            StatusFilter = status?.Trim() ?? string.Empty,
            TotalCount = allRows.Count,
            PaidCount = allRows.Count(x => x.PaymentStatus == "PAID"),
            PendingCount = allRows.Count(x => x.Status == "pending-payment" || x.Status == "pending-plan"),
            CancelledCount = allRows.Count(x => x.PaymentStatus == "CANCELLED" || x.Status == "cancelled"),
            Items = rows.Select(MapSummary).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(string id)
    {
        var registration = await _context.MembershipRegistrations
            .Include(x => x.RegistrationPlan)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (registration == null)
        {
            return NotFound();
        }

        return View(new RegistrationDetailViewModel
        {
            Registration = registration,
            PlanName = registration.RegistrationPlan?.Name ?? "Chưa chọn gói",
            StatusLabel = FormatStatus(registration),
            StatusBadgeClass = ResolveBadgeClass(registration.PaymentStatus, registration.Status)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNote(string id, string? adminNote)
    {
        var registration = await _context.MembershipRegistrations.FirstOrDefaultAsync(x => x.Id == id);
        if (registration == null)
        {
            return NotFound();
        }

        registration.AdminNote = adminNote?.Trim() ?? string.Empty;
        registration.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật ghi chú quản trị cho hồ sơ đăng ký.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static RegistrationSummaryViewModel MapSummary(Models.MembershipRegistration registration) => new()
    {
        Registration = registration,
        PlanName = registration.RegistrationPlan?.Name ?? "Chưa chọn gói",
        StatusLabel = FormatStatus(registration),
        StatusBadgeClass = ResolveBadgeClass(registration.PaymentStatus, registration.Status),
        IsSuccessful = string.Equals(registration.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase)
    };

    private static string FormatStatus(Models.MembershipRegistration registration)
    {
        return registration.PaymentStatus switch
        {
            "PAID" => "Đã thanh toán",
            "PENDING" => "Chờ thanh toán",
            "PROCESSING" => "Đang xử lý",
            "CANCELLED" => "Đã hủy",
            "FORM_ONLY" when registration.Status == "pending-plan" => "Đã gửi form, chờ chọn gói",
            _ => string.IsNullOrWhiteSpace(registration.Status) ? "Chưa rõ" : registration.Status
        };
    }

    private static string ResolveBadgeClass(string paymentStatus, string registrationStatus)
    {
        if (string.Equals(paymentStatus, "PAID", StringComparison.OrdinalIgnoreCase))
        {
            return "bg-success";
        }

        if (string.Equals(paymentStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(registrationStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return "bg-danger";
        }

        if (string.Equals(paymentStatus, "PROCESSING", StringComparison.OrdinalIgnoreCase))
        {
            return "bg-info text-dark";
        }

        if (string.Equals(paymentStatus, "PENDING", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(registrationStatus, "pending-payment", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(registrationStatus, "pending-plan", StringComparison.OrdinalIgnoreCase))
        {
            return "bg-warning text-dark";
        }

        return "bg-secondary";
    }
}
