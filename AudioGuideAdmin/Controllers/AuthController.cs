using AudioGuideAdmin.Controllers.Data;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

public class AuthController : Controller
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Login()
    {
        if (!HasAnyAdmin())
        {
            return RedirectToAction("Register");
        }

        if (IsLoggedIn())
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password, string? returnUrl = null)
    {
        var user = _context.Users.FirstOrDefault(x => x.Username == username);
        if (user != null)
        {
            var hashed = HashPassword(password);
            if (user.Password == hashed || user.Password == password)
            {
                if (user.Password != hashed)
                {
                    user.Password = hashed;
                    _context.SaveChanges();
                }

                HttpContext.Session.SetString("user", user.Username);

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }
        }

        ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    public IActionResult Register()
    {
        if (HasAnyAdmin() && !IsLoggedIn())
        {
            return RedirectToAction("Login");
        }

        return View();
    }

    [HttpPost]
    public IActionResult Register(User model)
    {
        if (HasAnyAdmin() && !IsLoggedIn())
        {
            return RedirectToAction("Login");
        }

        if (_context.Users.Any(x => x.Username == model.Username))
        {
            ViewBag.Error = "Tên đăng nhập đã tồn tại";
            return View(model);
        }

        model.Password = HashPassword(model.Password);
        model.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(model);
        _context.SaveChanges();

        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    private static string HashPassword(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("user"));
    }

    private bool HasAnyAdmin()
    {
        return _context.Users.Any();
    }
}
