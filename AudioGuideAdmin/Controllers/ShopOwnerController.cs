using AudioGuideAdmin.Controllers.Data;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class ShopOwnerController : Controller
{
    private readonly AppDbContext _context;

    public ShopOwnerController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var owners = _context.ShopOwners.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            owners = owners.Where(x => x.Status == status);
        }

        ViewBag.Status = status ?? string.Empty;
        return View(await owners
            .OrderBy(x => x.Status == ShopOwnerStatus.Pending ? 0 : x.Status == ShopOwnerStatus.Approved ? 1 : 2)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        var owner = await _context.ShopOwners.FirstOrDefaultAsync(x => x.Id == id);
        if (owner == null)
        {
            return NotFound();
        }

        owner.Status = ShopOwnerStatus.Approved;
        owner.ApprovedAt = DateTime.UtcNow;
        owner.UpdatedAt = DateTime.UtcNow;
        owner.ApprovedByAdminId = await ResolveAdminReviewerIdAsync();
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã duyệt tài khoản chủ quán.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(string id)
    {
        var owner = await _context.ShopOwners.FirstOrDefaultAsync(x => x.Id == id);
        if (owner == null)
        {
            return NotFound();
        }

        owner.Status = ShopOwnerStatus.Suspended;
        owner.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã tạm khóa tài khoản chủ quán.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> ResolveAdminReviewerIdAsync()
    {
        var username = HttpContext.Session.GetString("user");
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _context.Users
            .Where(x => x.Username == username)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }
}
