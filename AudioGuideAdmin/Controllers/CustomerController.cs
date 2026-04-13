using AudioGuideAdmin.Data;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class CustomerController : Controller
{
    private readonly AppDbContext _context;

    public CustomerController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var accounts = await _context.CustomerAccounts
            .AsNoTracking()
            .Include(x => x.Registration)
            .ThenInclude(x => x!.RegistrationPlan)
            .OrderByDescending(x => x.PaidAt ?? x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        var model = new CustomerIndexViewModel
        {
            TotalCount = accounts.Count,
            ActiveCount = accounts.Count(x => x.IsActive),
            PaidCount = accounts.Count(x => x.IsPaid),
            PendingCount = accounts.Count(x => !x.IsPaid),
            Items = accounts.Select(x => new CustomerSummaryViewModel
            {
                Account = x,
                PlanName = x.Registration?.RegistrationPlan?.Name ?? "Chưa chọn gói",
                RegistrationStatus = string.IsNullOrWhiteSpace(x.Status) ? "Chưa rõ" : x.Status
            }).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(string id)
    {
        var account = await _context.CustomerAccounts
            .AsNoTracking()
            .Include(x => x.Registration)
            .ThenInclude(x => x!.RegistrationPlan)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (account == null)
        {
            return NotFound();
        }

        return View(new CustomerDetailViewModel
        {
            Account = account,
            Registration = account.Registration,
            PlanName = account.Registration?.RegistrationPlan?.Name ?? "Chưa chọn gói"
        });
    }
}
