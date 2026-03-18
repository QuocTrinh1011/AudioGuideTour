using Microsoft.AspNetCore.Mvc;
using AudioGuideAdmin.Data;

public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("user") == null)
        {
            return RedirectToAction("Login", "Auth");
        }
        ViewBag.TotalPoi = _context.Pois.Count();
        ViewBag.TotalVisit = _context.VisitHistories.Count();

        var top = _context.VisitHistories
            .GroupBy(x => x.PoiId)
            .Select(g => new { PoiId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        ViewBag.Top = top;

        return View();
    }
}