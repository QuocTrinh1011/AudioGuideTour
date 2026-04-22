using AudioGuideAdmin.Helpers;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuideAdmin.Controllers;

public class OwnerAuthController : Controller
{
    private readonly IConfiguration _configuration;

    public OwnerAuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        return Redirect(OwnerPortalUrlHelper.Build(
            _configuration,
            "/OwnerAuth/Login",
            new Dictionary<string, string?> { ["returnUrl"] = returnUrl }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(OwnerLoginViewModel model)
    {
        return Redirect(OwnerPortalUrlHelper.Build(
            _configuration,
            "/OwnerAuth/Login",
            new Dictionary<string, string?> { ["returnUrl"] = model.ReturnUrl }));
    }

    public IActionResult Register()
    {
        return Redirect(OwnerPortalUrlHelper.Build(_configuration, "/OwnerAuth/Register"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(OwnerRegisterViewModel _)
    {
        return Redirect(OwnerPortalUrlHelper.Build(_configuration, "/OwnerAuth/Register"));
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Remove("ownerId");
        HttpContext.Session.Remove("ownerName");
        return Redirect(OwnerPortalUrlHelper.Build(_configuration, "/OwnerAuth/Logout"));
    }
}
