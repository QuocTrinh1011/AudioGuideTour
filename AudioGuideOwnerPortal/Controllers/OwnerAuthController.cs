using AudioGuideOwnerPortal.Data;
using AudioGuideOwnerPortal.Helpers;
using AudioGuideOwnerPortal.Models;
using AudioGuideOwnerPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideOwnerPortal.Controllers;

public class OwnerAuthController : Controller
{
    private readonly AppDbContext _context;

    public OwnerAuthController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(OwnerSessionHelper.GetOwnerId(HttpContext)))
        {
            return RedirectToAction("Index", "OwnerDashboard");
        }

        return View(new OwnerLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(OwnerLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedLogin = model.Login.Trim();
        var owner = await _context.ShopOwners
            .FirstOrDefaultAsync(x => x.Phone == normalizedLogin || x.Email == normalizedLogin);

        if (owner == null || !PasswordHashHelper.VerifyPassword(model.Password, owner.PasswordHash, owner.PasswordSalt))
        {
            ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu.");
            return View(model);
        }

        owner.LastLoginAt = DateTime.UtcNow;
        owner.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        OwnerSessionHelper.SignIn(HttpContext, owner.Id, owner.BusinessName);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "OwnerDashboard");
    }

    public IActionResult Register()
    {
        return View(new OwnerRegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(OwnerRegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedPhone = model.Phone.Trim();
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        if (await _context.ShopOwners.AnyAsync(x => x.Phone == normalizedPhone))
        {
            ModelState.AddModelError(nameof(model.Phone), "Số điện thoại này đã được dùng.");
        }

        if (await _context.ShopOwners.AnyAsync(x => x.Email == normalizedEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được dùng.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (hash, salt) = PasswordHashHelper.HashPassword(model.Password);
        var owner = new ShopOwner
        {
            Id = Guid.NewGuid().ToString("N"),
            FullName = model.FullName.Trim(),
            Phone = normalizedPhone,
            Email = normalizedEmail,
            BusinessName = model.BusinessName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Status = ShopOwnerStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ShopOwners.Add(owner);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã gửi đăng ký chủ quán. Khi admin duyệt xong, bạn có thể đăng nhập để quản lý POI.";
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Logout()
    {
        OwnerSessionHelper.SignOut(HttpContext);
        return RedirectToAction(nameof(Login));
    }
}
