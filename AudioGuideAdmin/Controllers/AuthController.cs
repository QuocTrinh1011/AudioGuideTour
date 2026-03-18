using Microsoft.AspNetCore.Mvc;
using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;

public class AuthController : Controller
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        var user = _context.Users
            .FirstOrDefault(x => x.Username == username && x.Password == password);

        if (user != null)
        {
            HttpContext.Session.SetString("user", user.Username);
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(User model)
    {
        _context.Users.Add(model);
        _context.SaveChanges();

        // 👉 QUAY LẠI LOGIN
        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}